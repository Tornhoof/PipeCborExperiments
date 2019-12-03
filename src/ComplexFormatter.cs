using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
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

    public class State
    {
        public int Step { get; set; } = 0;
    }

    public sealed class ComplexClassFormatter<T> : ComplexFormatter where T : class
    {
        private static readonly int MaxSteps = typeof(T).GetProperties().Length * 2;
        private static readonly SerializeDelegate Serializer = BuildDelegate();

        private static SerializeDelegate BuildDelegate()
        {
            var writerParam = Expression.Parameter(typeof(CborWriter), "writer");
            var valueParam = Expression.Parameter(typeof(T), "value");
            var stateParam = Expression.Parameter(typeof(State), "state");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var properties = typeof(T).GetProperties();
            var expressions = new List<Expression>();
            var switchCases = new List<SwitchCase>();
            var result = Expression.Parameter(typeof(ValueTask), "result");
            var returnLabel = Expression.Label(typeof(ValueTask), "return");
            var labels = new LabelTarget[properties.Length * 2];
            int counter = 0;
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                labels[counter] = Expression.Label($"{property.Name + "Name"}");
                var switchCaseName = Expression.SwitchCase(Expression.Goto(labels[counter]), Expression.Constant(counter++));
                switchCases.Add(switchCaseName);
                labels[counter] = Expression.Label($"{property.Name + "Value"}");
                var switchCaseValue = Expression.SwitchCase(Expression.Goto(labels[counter]), Expression.Constant(counter++));
                switchCases.Add(switchCaseValue);
            }
            switchCases.Add(Expression.SwitchCase(Expression.Goto(returnLabel, Expression.Default(typeof(ValueTask))), Expression.Constant(MaxSteps)));
            var switchExpression = Expression.Switch(Expression.Property(stateParam, nameof(State.Step)), switchCases.ToArray());
            expressions.Add(switchExpression);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var nameBody = BuildWriteExpression(writerParam, result, FindWriterMethod(nameof(CborWriter.WriteString)), Expression.Constant(property.Name),
                    cancellationTokenParam, stateParam, returnLabel);
                if (i == 0)
                {
                    var beginMapExpression = Expression.Call(writerParam, nameof(CborWriter.WriteBeginMap), null, Expression.Constant(properties.Length));
                    nameBody = nameBody.Update(nameBody.Variables, nameBody.Expressions.Prepend(beginMapExpression));
                }
                expressions.Add(Expression.Label(labels[i * 2]));
                expressions.Add(nameBody);
                var valueBody = BuildWriteExpression(writerParam, result, FindWriterMethod(nameof(CborWriter.WriteString)), Expression.Property(valueParam, property),
                    cancellationTokenParam, stateParam, returnLabel);
                expressions.Add(Expression.Label(labels[(i * 2) + 1]));
                expressions.Add(valueBody);
            }

            expressions.Add(Expression.Assign(Expression.Property(stateParam, nameof(State.Step)), Expression.Constant(MaxSteps)));
            expressions.Add(Expression.Label(returnLabel, Expression.Default(typeof(ValueTask))));
            var block = Expression.Block(expressions);
            var lambda = Expression.Lambda<SerializeDelegate>(block, writerParam, valueParam, stateParam, cancellationTokenParam);
            return lambda.Compile();
        }

        private static BlockExpression BuildWriteExpression(ParameterExpression writerParameter, ParameterExpression result, MethodInfo writerMethod, Expression argument,
            ParameterExpression cancellationTokenParameter, ParameterExpression stateParameter, LabelTarget returnLabel)
        {
            var assignExpression = Expression.Assign(result, Expression.Call(writerParameter, writerMethod, argument, cancellationTokenParameter));
            var ifExpression = Expression.IfThen(
                Expression.IsFalse(
                    Expression.Property(result, nameof(ValueTask.IsCompletedSuccessfully))),
                Expression.Block(
                    Expression.PostIncrementAssign(
                        Expression.Property(stateParameter, nameof(State.Step))),
                    Expression.Return(returnLabel, result)));
            var block = Expression.Block(new[] {result}, assignExpression, ifExpression);
            return block;
        }

        public async ValueTask SerializeAsync(CborWriter writer, T value, State state, CancellationToken cancellationToken = default)
        {
            if (value is null)
            {
                writer.WriteNull();
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            do
            {
                await Serializer(writer, value, state, cancellationToken).ConfigureAwait(false);
            } while (state.Step < MaxSteps);
        }

        private delegate ValueTask SerializeDelegate(CborWriter writer, T value, State state, CancellationToken cancellationToken = default);
    }
}
