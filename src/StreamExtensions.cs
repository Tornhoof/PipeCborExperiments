using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingCbor
{
    public static class StreamExtensions
    {
        public static async ValueTask WriteSequenceAsync(this Stream stream, ReadOnlySequence<byte> sequence, CancellationToken cancellationToken = default)
        {
            if(!sequence.IsEmpty)
            {
                SequencePosition position = sequence.Start;
                while (sequence.TryGet(ref position, out var memory))
                {
                    await stream.WriteAsync(memory, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
