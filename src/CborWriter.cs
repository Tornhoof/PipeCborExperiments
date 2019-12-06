using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingCbor
{
    public partial class CborWriter
    {
        private readonly PipeWriter _pipeWriter;
        private const int MinBuffer = 500;
        private int _pending;
        private const byte IndefiniteLength = 31;

        public CborWriter(PipeWriter pipeWriter)
        {
            _pipeWriter = pipeWriter;
        }

        public ValueTask WriteString(string value, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                WriteNull();
                return FlushAsync(cancellationToken);
            }

            if (value.Length < MinBuffer)
            {
                WriteString(value.AsSpan());
                return FlushAsync(cancellationToken);
            }

            return WriteString(value.AsMemory(), cancellationToken);
        }

        public void WriteString(in ReadOnlySpan<char> value)
        {
            var finalLength = Encoding.UTF8.GetByteCount(value);
            WriteInteger(CborType.TextString, (ulong) finalLength);
            var output = _pipeWriter.GetSpan(finalLength);
            var opStatus = Utf8.FromUtf16(value, output, out _, out var bytesWritten);
            Debug.Assert(opStatus == OperationStatus.Done);
            Advance(bytesWritten);
        }

        public void WriteNull()
        {
            WriteEncodedType(CborType.Primitive, (byte) SimpleValues.Null);
        }

        private async ValueTask WriteString(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
        {
            var finalLength = Encoding.UTF8.GetByteCount(value.Span);
            WriteInteger(CborType.TextString, (ulong) finalLength);
            OperationStatus opStatus;
            do
            {
                var length = value.Length < MinBuffer ? value.Length : MinBuffer;
                var output = _pipeWriter.GetMemory(length);
                opStatus = Utf8.FromUtf16(value.Span, output.Span, out var charsRead, out var bytesWritten);
                if (opStatus == OperationStatus.DestinationTooSmall)
                {
                    value = value.Slice(charsRead);
                    Advance(bytesWritten);
                    await FlushAsync(cancellationToken).ConfigureAwait(false); // need to flush and stuff, so it's async now;
                }
                else
                {
                    Advance(bytesWritten);
                }
            } while (opStatus != OperationStatus.Done);

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBeginMap(int count)
        {
            WriteSize(CborType.Map, count);
        }

        public async ValueTask WriteMap(ICollection<KeyValuePair<string, object>> map, CancellationToken cancellationToken = default)
        {
            WriteBeginMap(map.Count);
            foreach (var kvp in map)
            {
                await WriteString(kvp.Key, cancellationToken).ConfigureAwait(false);
                switch (kvp.Value)
                {
                    case string s:
                    {
                        await WriteString(s, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case byte[] b:
                    {
                        await WriteBytes(b, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case Stream st:
                    {
                        await WriteBytes(st, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case null:
                    {
                        WriteNull();
                        break;
                    }
                }
            }
            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int bytes)
        {
            _pipeWriter.Advance(bytes);
            _pending += bytes;
        }

        public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
        {
            var t = FlushInternalAsync(cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                _pipeWriter.Complete();
                _pending = 0;
                return default;
            }

            return AwaitCompleteAsync(t);
        }

        private async ValueTask AwaitCompleteAsync(ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            await _pipeWriter.CompleteAsync().ConfigureAwait(false);
            _pending = 0;
        }

        /// <summary>
        /// We try not to flush too early, only after MinBuffer is written, currently at 500 bytes
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            return _pending < MinBuffer ? default : FlushInternalAsync(cancellationToken);
        }

        private ValueTask FlushInternalAsync(CancellationToken cancellationToken = default)
        {
            var t = _pipeWriter.FlushAsync(cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                _pending = 0;
                return default;
            }

            return AwaitValueTask(t);
        }

        private async ValueTask AwaitValueTask(ValueTask<FlushResult> task)
        {
            await task.ConfigureAwait(false);
            _pending = 0;
        }

        public ValueTask WriteBytes(byte[] value, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                WriteNull();
                return FlushAsync(cancellationToken);
            }

            if (value.Length < MinBuffer)
            {
                WriteBytes(value.AsSpan());
                return FlushAsync(cancellationToken);
            }

            return WriteBytes(value.AsMemory(), cancellationToken);
        }

        public async ValueTask WriteBytes(Stream value, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                WriteNull();
                await FlushAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var buffer = _pipeWriter.GetMemory(MinBuffer);
            int read;
            while ((read = await value.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) 
            {
                Advance(read);
                await FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void WriteBytes(in ReadOnlySpan<byte> value)
        {
            WriteInteger(CborType.ByteString, (ulong)value.Length);
            value.CopyTo(_pipeWriter.GetSpan(value.Length));
            Advance(value.Length);
        }

        private async ValueTask WriteBytes(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
        {
            WriteInteger(CborType.ByteString, (ulong)value.Length);
            do
            {
                var length = value.Length < MinBuffer ? value.Length : MinBuffer;
                value.Span.CopyTo(_pipeWriter.GetSpan(length));
                Advance(length);
                value = value.Slice(length);
                await FlushAsync(cancellationToken).ConfigureAwait(false);

            } while (value.Length > 0);

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSize(CborType type, long? size)
        {
            if (size is null)
            {
                WriteEncodedType(type, IndefiniteLength);
            }
            else
            {
                WriteInteger(type, (ulong) size.Value);
            }
        }

        private void WriteInteger(CborType type, ulong value)
        {
            if (value <= 23)
            {
                WriteEncodedType(type, (byte) value);
            }
            else if (value <= byte.MaxValue)
            {
                Span<byte> bytes = _pipeWriter.GetSpan(2);
                bytes[1] = (byte) value;
                bytes[0] = EncodeType(type, 24);
                Advance(2);
            }
            else if (value <= ushort.MaxValue)
            {
                Span<byte> bytes = _pipeWriter.GetSpan(3);
                bytes[0] = EncodeType(type, 25);
                BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(1), (ushort) value);
                Advance(3);
            }
            else if (value <= uint.MaxValue)
            {
                Span<byte> bytes = _pipeWriter.GetSpan(5);
                bytes[0] = EncodeType(type, 26);
                BinaryPrimitives.WriteUInt32BigEndian(bytes.Slice(1), (uint) value);
                Advance(5);
            }
            else
            {
                Span<byte> bytes = _pipeWriter.GetSpan(9);
                bytes[0] = EncodeType(type, 27);
                BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(1), value);
                Advance(9);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte EncodeType(CborType type, byte value)
        {
            return (byte) ((byte) type << 5 | (value & 0x1f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteEncodedType(CborType type, byte value)
        {
            Span<byte> buffer = _pipeWriter.GetSpan(1);
            buffer[0] = EncodeType(type, value);
            Advance(1);
        }
    }
}
