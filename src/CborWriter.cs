using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingCbor
{
    public class CborWriter
    {
        private readonly PipeWriter _pipeWriter;
        private const int MinBuffer = 500;
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
            _pipeWriter.Advance(bytesWritten);
        }

        private void WriteNull()
        {
            WriteEncodedType(CborType.Primitive, (byte) SimpleValues.Null);
        }

        private async ValueTask WriteString(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
        {
            var finalLength = Encoding.UTF8.GetByteCount(value.Span);
            WriteInteger(CborType.TextString, (ulong) finalLength);
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            OperationStatus opStatus;
            do
            {
                var length = value.Length < MinBuffer ? value.Length : MinBuffer;
                var output = _pipeWriter.GetMemory(length);
                opStatus = Utf8.FromUtf16(value.Span, output.Span, out var charsRead, out var bytesWritten);
                if (opStatus == OperationStatus.DestinationTooSmall)
                {
                    value = value.Slice(charsRead);
                    _pipeWriter.Advance(bytesWritten);
                    await FlushAsync(cancellationToken).ConfigureAwait(false); // need to flush and stuff, so it's async now;
                }
                else
                {
                    _pipeWriter.Advance(bytesWritten);
                }
            } while (opStatus != OperationStatus.Done);

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask WriteMap(ICollection<KeyValuePair<string, object>> map, CancellationToken cancellationToken = default)
        {
            WriteSize(CborType.Map, map.Count);
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


        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            var t = _pipeWriter.FlushAsync(cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                return default;
            }

            return AwaitValueTask(t);
        }

        private async ValueTask AwaitValueTask(ValueTask<FlushResult> task)
        {
            await task.ConfigureAwait(false);
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
                _pipeWriter.Advance(read);
                await FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void WriteBytes(in ReadOnlySpan<byte> value)
        {
            WriteInteger(CborType.ByteString, (ulong)value.Length);
            value.CopyTo(_pipeWriter.GetSpan(value.Length));
            _pipeWriter.Advance(value.Length);
        }

        private async ValueTask WriteBytes(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
        {
            WriteInteger(CborType.ByteString, (ulong)value.Length);
            await FlushAsync(cancellationToken).ConfigureAwait(false);
            do
            {
                var length = value.Length < MinBuffer ? value.Length : MinBuffer;
                value.Span.CopyTo(_pipeWriter.GetSpan(length));
                _pipeWriter.Advance(length);
                value = value.Slice(length);
                await FlushAsync(cancellationToken).ConfigureAwait(false);

            } while (value.Length > 0);

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

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
                WriteEncodedType(type, 24);
                WriteRawByte((byte)value);
            }
            else if (value <= ushort.MaxValue)
            {
                WriteEncodedType(type, 25);
                Span<byte> bytes = _pipeWriter.GetSpan(2);
                BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value);
                _pipeWriter.Advance(2);
            }
            else if (value <= uint.MaxValue)
            {
                WriteEncodedType(type, 26);
                Span<byte> bytes = _pipeWriter.GetSpan(4);
                BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)value);
                _pipeWriter.Advance(4);
            }
            else
            {
                WriteEncodedType(type, 27);
                Span<byte> bytes = _pipeWriter.GetSpan(8);
                BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
                _pipeWriter.Advance(8);
            }
        }

        private void WriteEncodedType(CborType type, byte value)
        {
            WriteRawByte((byte) ((byte) type << 5 | (value & 0x1f)));
        }

        private void WriteRawByte(byte value)
        {
            Span<byte> buffer = _pipeWriter.GetSpan(1);
            buffer[0] = value;
            _pipeWriter.Advance(1);
        }
    }
}
