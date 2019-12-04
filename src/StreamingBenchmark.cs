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
    public class StreamingBenchmark
    {
        [Params(100, 1000, 10000, 100000)]
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
        public async Task SerializePipe()
        {
            var pipe = PipeWriter.Create(Stream.Null);
            CborWriter cborWriter = new CborWriter(PipeWriter.Create(Stream.Null));
            await ComplexClassFormatter<Document>.Default.SerializeAsync(cborWriter, document).ConfigureAwait(false);
            await pipe.CompleteAsync().ConfigureAwait(false);
        }

        [Benchmark]
        public void SerializePeterO()
        {
            cborObject.WriteTo(Stream.Null);
        }

        [Benchmark]
        public async Task SerializeDahomey()
        {
            await Cbor.SerializeAsync(document, Stream.Null, CborOptions.Default).ConfigureAwait(false);
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
