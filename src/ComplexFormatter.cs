using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace StreamingCbor
{
    public abstract class ComplexFormatter
    {
        protected static MethodInfo FindWriterMethod(string name)
        {
            return typeof(CborWriter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, new Type[] {typeof(string), typeof(CancellationToken)}, null);
        }
    }

    public sealed class ComplexClassFormatter<T> : ComplexFormatter where T : class
    {
        private static readonly int MaxSteps = typeof(T).GetProperties().Length * 2;
        private static readonly SerializeDelegate Serializer = BuildDelegate();

        public static readonly ComplexClassFormatter<T> Default = new ComplexClassFormatter<T>();

        /// <summary>
        /// The trick is simple:
        /// We build a switch table for each step in the state machine, if a method's return task is not completed successfully the
        /// task is returned further for awaiting and the state of the machine is set to the next state
        /// Example:
        /// State 0 -> Write Name of 1. Property (and include the BeginMap)
        /// State 1 -> Write Value of 1. Property
        /// State 2 -> Write Name of 2. Property
        /// State 3 -> Write Value of 2. Property
        /// State 4 -> End
        /// </summary>
        /// <returns></returns>
        private static SerializeDelegate BuildDelegate()
        {
            var writerParam = Expression.Parameter(typeof(CborWriter), "writer");
            var valueParam = Expression.Parameter(typeof(T), "value");
            var stateParam = Expression.Parameter(typeof(int).MakeByRefType(), "state");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var properties = typeof(T).GetProperties();
            var expressions = new List<Expression>();
            var switchCases = new List<SwitchCase>();
            var result = Expression.Parameter(typeof(ValueTask), "result");
            var returnLabel = Expression.Label(typeof(ValueTask), "return");
            var labels = new LabelTarget[properties.Length * 2];
            int counter = 0;
            for (int i = 0; i < properties.Length; i++) // Build array with step labels we use for reentry
            {
                var property = properties[i];
                labels[counter] = Expression.Label($"{property.Name + "Name"}");
                var switchCaseName = Expression.SwitchCase(Expression.Goto(labels[counter]), Expression.Constant(counter++));
                switchCases.Add(switchCaseName);
                labels[counter] = Expression.Label($"{property.Name + "Value"}");
                var switchCaseValue = Expression.SwitchCase(Expression.Goto(labels[counter]), Expression.Constant(counter++));
                switchCases.Add(switchCaseValue);
            }
            var switchExpression = Expression.Switch(stateParam, switchCases.ToArray());
            expressions.Add(switchExpression);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var nameBody = BuildWriteExpression(writerParam, result, FindWriterMethod(nameof(CborWriter.WriteString)), Expression.Constant(property.Name),
                    cancellationTokenParam, stateParam, returnLabel, (i * 2) + 1); // next following value state
                if (i == 0)
                {
                    var beginMapExpression = Expression.Call(writerParam, nameof(CborWriter.WriteBeginMap), null, Expression.Constant(properties.Length));
                    nameBody = nameBody.Update(nameBody.Variables, nameBody.Expressions.Prepend(beginMapExpression));
                }
                expressions.Add(Expression.Label(labels[i * 2]));
                expressions.Add(nameBody);
                var valueBody = BuildWriteExpression(writerParam, result, FindWriterMethod(nameof(CborWriter.WriteString)), Expression.Property(valueParam, property),
                    cancellationTokenParam, stateParam, returnLabel, (i + 1) * 2); // next following name state
                expressions.Add(Expression.Label(labels[(i * 2) + 1]));
                expressions.Add(valueBody);
            }

            expressions.Add(Expression.Assign(stateParam, Expression.Constant(MaxSteps)));
            expressions.Add(Expression.Label(returnLabel, Expression.Default(typeof(ValueTask))));
            var block = Expression.Block(expressions);
            var lambda = Expression.Lambda<SerializeDelegate>(block, writerParam, valueParam, stateParam, cancellationTokenParam);
            return lambda.Compile();
        }

        private static BlockExpression BuildWriteExpression(ParameterExpression writerParameter, ParameterExpression result, MethodInfo writerMethod, Expression argument,
            ParameterExpression cancellationTokenParameter, ParameterExpression stateParameter, LabelTarget returnLabel, int state)
        {
            var assignExpression = Expression.Assign(result, Expression.Call(writerParameter, writerMethod, argument, cancellationTokenParameter));
            var ifExpression = Expression.IfThen(
                Expression.IsFalse(
                    Expression.Property(result, nameof(ValueTask.IsCompletedSuccessfully))),
                Expression.Block(
                    Expression.Assign(stateParameter, Expression.Constant(state)),
                    Expression.Return(returnLabel, result)));
            var block = Expression.Block(new[] {result}, assignExpression, ifExpression);
            return block;
        }

        public ValueTask SerializeAsync(CborWriter writer, T value, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                writer.WriteNull();
                return writer.FlushAsync(cancellationToken);
            }

            var step = 0;
            var task = Serializer(writer, value, ref step, cancellationToken);
            if (task.IsCompletedSuccessfully && step == MaxSteps) // if everything is synchronous, we should not hit any async path at all
            {
                return writer.FlushAsync(cancellationToken);
            }

            return AwaitSerializeAsync(task, writer, value, step, cancellationToken);
        }

        private async ValueTask AwaitSerializeAsync(ValueTask task, CborWriter writer, T value, int step, CancellationToken cancellationToken = default)
        {
            await task.ConfigureAwait(false);
            while (step < MaxSteps)
            {
                await Serializer(writer, value, ref step, cancellationToken).ConfigureAwait(false);
            }
        }

        private delegate ValueTask SerializeDelegate(CborWriter writer, T value, ref int state, CancellationToken cancellationToken = default);
    }
}
