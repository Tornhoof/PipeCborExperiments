using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dahomey.Cbor;
using PeterO.Cbor;

namespace StreamingCbor
{
    [MemoryDiagnoser]
    [ShortRunJob]
    [DisassemblyDiagnoser(recursiveDepth:2)]
    public class StreamingBenchmark
    {
        [Params(100 , 1000, 10000, 100000)]
        public int Length;

        private Document document;
        private CBORObject cborObject;

        [GlobalSetup]
        public void Setup()
        {
            document = new Document{Key = GetText(Length), Value = GetText(Length)};
            cborObject = CBORObject.NewMap();
            cborObject.Add(nameof(Document.Key), document.Key);
            cborObject.Add(nameof(Document.Value), document.Value);
        }

        [Benchmark]
        public ValueTask SerializePipe()
        {
            var pipe = PipeWriter.Create(Stream.Null);
            CborWriter cborWriter = new CborWriter(pipe);
            var t = ComplexClassFormatter<Document>.Default.SerializeAsync(cborWriter, document);
            if (t.IsCompletedSuccessfully)
            {
                return cborWriter.CompleteAsync();
            }

            return AwaitComplete(t, cborWriter);
        }

        private async ValueTask AwaitComplete(ValueTask valueTask, CborWriter writer)
        {
            await valueTask.ConfigureAwait(false);
            await writer.CompleteAsync();
        }

        //[Benchmark]
        //public void SerializePeterO()
        //{
        //    cborObject.WriteTo(Stream.Null);
        //}

        [Benchmark]
        public Task SerializeDahomey()
        {
            return Cbor.SerializeAsync(document, Stream.Null, CborOptions.Default);
        }

        private const string LoremIpsum = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";

        private static string GetText(int length)
        {
            return string.Create(length, LoremIpsum, (span, s) =>
            {
                var output = span;
                int remaining = output.Length;
                do
                {
                    var l = Math.Min(s.Length, remaining);
                    s.AsSpan(0, l).CopyTo(output);
                    output = output.Slice(l);
                    remaining -= l;
                } while (remaining > 0);

            });
        }
    }
}
