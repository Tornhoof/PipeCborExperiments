using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StreamingCbor
{
    public class CborReader
    {
        private readonly PipeReader _pipeReader;
        private ReadOnlySequence<byte> _currentSequence;
        private static readonly byte NullByte = CborWriter.EncodeType(CborType.Primitive, (byte) SimpleValues.Null);
        private static readonly byte MapByte = CborWriter.EncodeType(CborType.Map, 0);
        private static readonly byte TextStringByte = CborWriter.EncodeType(CborType.TextString, 0);
        private static readonly ValueTask<bool> TrueTask = new ValueTask<bool>(true).Preserve();
        private static readonly ValueTask<bool> FalseTask = new ValueTask<bool>(false).Preserve();
        private static readonly ValueTask<string> NullTask = new ValueTask<string>((string) null).Preserve();
        private byte _token;

        public CborReader(PipeReader pipeReader)
        {
            _pipeReader = pipeReader;
        }

        private ValueTask ReadNextToken(CancellationToken cancellationToken = default)
        {
            if (_pipeReader.TryRead(out var sequence))
            {
                ReadNextToken(sequence);
                return default;
            }
            var result = _pipeReader.ReadAsync(cancellationToken);
            if (result.IsCompletedSuccessfully)
            {
                ReadNextToken(result.Result);
                return default;
            }

            return AwaitReadToken(result);

        }

        private void ReadNextToken(ReadResult result)
        {
            _currentSequence = result.Buffer;
            _token = _currentSequence.FirstSpan[0];
            _currentSequence = _currentSequence.Slice(1);
            _pipeReader.AdvanceTo(_currentSequence.Start);
        }

        private async ValueTask AwaitReadToken(ValueTask<ReadResult> task)
        {
            var result = await task.ConfigureAwait(false);
            ReadNextToken(result);
        }

        public ValueTask<bool> ReadIsNull(CancellationToken cancellationToken = default)
        {
            return ReadIsToken(NullByte, cancellationToken);
        }


        private ValueTask<bool> ReadIsToken(byte token, CancellationToken cancellationToken = default)
        {
            var t = ReadNextToken(cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                return IsToken(token) ? TrueTask : FalseTask;
            }

            return AwaitReadIsToken(t, token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsToken(byte token)
        {
            return (_token & token) == token;
        }

        private async ValueTask<bool> AwaitReadIsToken(ValueTask task, byte token)
        {
            await task.ConfigureAwait(false);
            return IsToken(token);
        }

        public ValueTask<bool> ReadIsMap(CancellationToken cancellationToken = default)
        {
            return ReadIsToken(MapByte, cancellationToken);
        }

        public ValueTask<string> ReadString(CancellationToken cancellationToken = default)
        {
            var t = ReadIsNull(cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                if (t.Result)
                {
                    return NullTask;
                }

                if (IsToken(TextStringByte))
                {
                    return DecodeString(cancellationToken);
                }
            }

            return default;
        }

        private ValueTask<string> DecodeString(CancellationToken cancellationToken = default)
        {
            var length = ReadLength();
            if (_currentSequence.Length >= length)
            {
                return default;
            }

            return default;
        }

        private long ReadLength()
        {
            var minorValue = _token & 0x1F;
            if (minorValue <= 23)
            {
                return minorValue;
            }

            if (minorValue == 24)
            {
                var length = _currentSequence.FirstSpan[0];
                _currentSequence = _currentSequence.Slice(1);
                _pipeReader.AdvanceTo(_currentSequence.Start);
                return length;
            }

            if (minorValue == 25)
            {
                var length = BinaryPrimitives.ReadUInt16BigEndian(_currentSequence.FirstSpan.Slice(0, 2));
                _currentSequence = _currentSequence.Slice(2);
                _pipeReader.AdvanceTo(_currentSequence.Start);
                return length;
            }

            if (minorValue == 26)
            {
                var length = BinaryPrimitives.ReadUInt32BigEndian(_currentSequence.FirstSpan.Slice(0, 4));
                _currentSequence = _currentSequence.Slice(4);
                _pipeReader.AdvanceTo(_currentSequence.Start);
                return length;
            }

            if (minorValue == 27)
            {
                var length = BinaryPrimitives.ReadUInt64BigEndian(_currentSequence.FirstSpan.Slice(0, 8));
                _currentSequence = _currentSequence.Slice(8);
                _pipeReader.AdvanceTo(_currentSequence.Start);
                return checked((long) length);
            }

            ThrowInvalidFormatException();
            return default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidFormatException()
        {
            throw new InvalidOperationException();
        }
    }
}
