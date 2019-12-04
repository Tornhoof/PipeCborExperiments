using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingCbor
{
    public class CborReader
    {
        private readonly PipeReader _pipeReader;
        private ReadOnlySequence<byte> _currentSequence;
        private static readonly byte NullByte = EncodeType(CborType.Primitive, (byte) SimpleValues.Null);
        private static readonly byte MapByte = (byte) CborType.Map << 5;
        private static readonly ValueTask<bool> TrueTask = new ValueTask<bool>(true).Preserve();
        private static readonly ValueTask<bool> FalseTask = new ValueTask<bool>(false).Preserve();
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
                return _token == token ? TrueTask : FalseTask;
            }

            return AwaitReadIsToken(t, token);
        }

        private async ValueTask<bool> AwaitReadIsToken(ValueTask task, byte token)
        {
            await task.ConfigureAwait(false);
            return (_token & token) == token;
        }

        public ValueTask<bool> ReadIsMap(CancellationToken cancellationToken = default)
        {
            return ReadIsToken(MapByte, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte EncodeType(CborType type, byte value)
        {
            return (byte) ((byte) type << 5 | (value & 0x1f));
        }

    }
}
