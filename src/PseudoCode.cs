using System.Reflection.Metadata.Ecma335;
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
        /// Loop until we hit the end, ideal case scenario is that 
        /// </summary>
        public async ValueTask Serialize(object value, CancellationToken cancellationToken = default)
        {
            int step = 0;
            do
            {
                await SerializeInternal(value, ref step, cancellationToken).ConfigureAwait(false);

            } while (step < 4);
        }

        private ValueTask SerializeInternal(object value, ref int step, CancellationToken cancellationToken = default)
        {
            switch (step)
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
            var task = WriteString("Hello");
            if (!task.IsCompletedSuccessfully)
            {
                step = 1;
                return task;
            }

            WriteFirstValue:
            task = WriteString("World");
            if (!task.IsCompletedSuccessfully)
            {
                step = 2;
                return task;
            }

            WriteSecondKey:
            task = WriteString("Hello");
            if (!task.IsCompletedSuccessfully)
            {
                step = 3;
                return task;
            }

            WriteSecondValue:
            task = WriteString("Universe");
            if (!task.IsCompletedSuccessfully)
            {
                step = 4;
                return task;
            }

            return default;
        }

        private ValueTask WriteString(string value)
        {
            // write string
            return FlushAsync();
        }

        /// <summary>
        /// This is the async part, if it needs flush, the flush could be async or sync
        /// </summary>
        private async ValueTask FlushAsync()
        {
            bool syncFlush = true;
            if (syncFlush)
            {
                return;
            }
            await Task.Yield();
        }
    }
}
