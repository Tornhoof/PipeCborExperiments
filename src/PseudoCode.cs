using System.IO.Pipelines;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingCbor
{
    /// <summary>
    /// This is how the Expression Tree Code looks like
    /// </summary>
    public class PseudoCode
    {
        /// <summary>
        /// Loop until we hit the end, best case scenario is that it returns exactly once with the final state
        /// </summary>
        public async ValueTask Serialize(PipeWriter writer, object value, CancellationToken cancellationToken = default)
        {
            int state = 0;
            do
            {
                await SerializeInternal(writer, value, ref state, cancellationToken).ConfigureAwait(false);

            } while (state < 4);
        }

        /// <summary>
        /// The generated Expression Tree Delegate 
        /// </summary>
        private ValueTask SerializeInternal(PipeWriter writer, object value, ref int state, CancellationToken cancellationToken = default)
        {
            switch (state)
            {
                case 0:
                    goto WriteFirstKey;
                case 1:
                    goto WriteFirstValue;
                case 2:
                    goto WriteSecondKey;
                case 3:
                    goto WriteSecondValue;
                case 4:
                    return default;
            }

            WriteFirstKey:
            var task = WriteString(writer, "Hello", cancellationToken);
            if (!task.IsCompletedSuccessfully)
            {
                state = 1;
                return task;
            }

            WriteFirstValue:
            task = WriteString(writer, "World", cancellationToken);
            if (!task.IsCompletedSuccessfully)
            {
                state = 2;
                return task;
            }

            WriteSecondKey:
            task = WriteString(writer, "Hello", cancellationToken);
            if (!task.IsCompletedSuccessfully)
            {
                state = 3;
                return task;
            }

            WriteSecondValue:
            task = WriteString(writer, "Universe", cancellationToken);
            if (!task.IsCompletedSuccessfully)
            {
                state = 4;
                return task;
            }

            state = 4;
            return default;
        }

        private ValueTask WriteString(PipeWriter pipeWriter, string value, CancellationToken cancellationToken = default)
        {
            var t = pipeWriter.WriteAsync(Encoding.UTF8.GetBytes(value), cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                return default;
            }
            // write string
            return AwaitFlushResultTask(t);
        }

        private async ValueTask AwaitFlushResultTask(ValueTask<FlushResult> vt)
        {
            await vt.ConfigureAwait(false);
        }
    }
}
