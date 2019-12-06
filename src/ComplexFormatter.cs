using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
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
        private static readonly SerializeDelegate Serializer = BuildSerializeDelegate();
        private static readonly DeserializeDelegate Deserializer = BuildDeserializeDelegate();

        public static readonly ComplexClassFormatter<T> Default = new ComplexClassFormatter<T>();

        private static DeserializeDelegate BuildDeserializeDelegate()
        {
            var readerParam = Expression.Parameter(typeof(CborReader), "reader");
            var wrappedResultParam = Expression.Parameter(typeof(WrappedResult).MakeByRefType(), "wrappedResult");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            return null;
        }


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
        private static SerializeDelegate BuildSerializeDelegate()
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
                var writeNameExpressions = GetIntegersForMemberName(property.Name);

                BlockExpression nameBody;
                if (writeNameExpressions != null)
                {
                    var typesToMatch = writeNameExpressions.Select(a => a.Value.GetType());
                    var propertyNameWriterMethodInfo =
                        FindPublicInstanceMethod(writerParam.Type, nameof(CborWriter.WriteVerbatim), typesToMatch.ToArray());
                    nameBody = Expression.Block(Expression.Call(writerParam, propertyNameWriterMethodInfo, writeNameExpressions));
                }
                else
                {
                    nameBody = BuildWriteExpression(writerParam, result, FindWriterMethod(nameof(CborWriter.WriteString)), Expression.Constant(property.Name),
                        cancellationTokenParam, stateParam, returnLabel, (i * 2) + 1); // next following value state
                }

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

        private static MethodInfo FindPublicInstanceMethod(Type type, string name, params Type[] args)
        {
            return args?.Length > 0 ? type.GetMethod(name, args) : type.GetMethod(name);
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

        public ValueTask<T> DeserializeAsync(CborReader reader, CancellationToken cancellationToken = default)
        {
            var result = new WrappedResult();
            var task = Deserializer(reader, ref result, cancellationToken);
            if (task.IsCompletedSuccessfully && result.State == MaxSteps) // if everything is synchronous, we should not hit any async path at all
            {
                return new ValueTask<T>(result.Result);
            }

            return AwaitDeserializeAsync(task.AsTask(), reader, result, cancellationToken);
        }

        private async ValueTask<T> AwaitDeserializeAsync(Task task, CborReader reader, WrappedResult result, CancellationToken cancellationToken)
        {
            await task.ConfigureAwait(false);
            result.TempTask = task;
            while (result.State < MaxSteps)
            {
                // This is not really nice, as depending on the valuetasksource this allocates the task
                // The idea is, that only if the valuetask really needs awaiting the delegate will return and
                // then the valuetask wraps a task anyway.
                // This is not completely true, as custom valuetasksources might behave differently
                var vTask = Deserializer(reader, ref result, cancellationToken);
                if (vTask.IsCompletedSuccessfully && result.State == MaxSteps)
                {
                    return result.Result;
                }

                task = vTask.AsTask();
                result.TempTask = task;
            }

            return result.Result;
        }

        private delegate ValueTask SerializeDelegate(CborWriter writer, T value, ref int state, CancellationToken cancellationToken = default);

        private delegate ValueTask DeserializeDelegate(CborReader reader, ref WrappedResult result, CancellationToken cancellationToken = default);

        private struct WrappedResult
        {
            public T Result { get; set; }
            public Task TempTask { get; set; }
            public int State { get; set; }
        }

        /// <summary>
        /// This is basically the same algorithm as in the t4 template to create the methods
        /// It's necessary to update both
        /// </summary>
        private static ConstantExpression[] GetIntegersForMemberName(string formattedName)
        {
            var result = new List<ConstantExpression>();
            var length = Encoding.UTF8.GetByteCount(formattedName);
            if (length > 23)
            {
                return null; // fallback
            }
            var bytes = new byte[length+1];
            bytes[0] = CborWriter.EncodeType(CborType.TextString, (byte) length);
            var opStatus = Utf8.FromUtf16(formattedName, bytes.AsSpan(1), out _, out _);
            Debug.Assert(opStatus == OperationStatus.Done);
            var remaining = bytes.Length;
            var ulongCount = Math.DivRem(remaining, 8, out remaining);
            var offset = 0;
            for (var j = 0; j < ulongCount; j++)
            {
                result.Add(Expression.Constant(BitConverter.ToUInt64(bytes, offset)));
                offset += sizeof(ulong);
            }

            var uintCount = Math.DivRem(remaining, 4, out remaining);
            for (var j = 0; j < uintCount; j++)
            {
                result.Add(Expression.Constant(BitConverter.ToUInt32(bytes, offset)));
                offset += sizeof(uint);
            }

            var ushortCount = Math.DivRem(remaining, 2, out remaining);
            for (var j = 0; j < ushortCount; j++)
            {
                result.Add(Expression.Constant(BitConverter.ToUInt16(bytes, offset)));
                offset += sizeof(ushort);
            }

            var byteCount = Math.DivRem(remaining, 1, out remaining);
            for (var j = 0; j < byteCount; j++)
            {
                result.Add(Expression.Constant(bytes[offset]));
                offset++;
            }

            Debug.Assert(remaining == 0);
            Debug.Assert(offset == bytes.Length);
            return result.ToArray();
        }
    }

}
