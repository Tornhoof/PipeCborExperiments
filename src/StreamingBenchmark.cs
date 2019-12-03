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
    /// <summary>
    /// |           Method | Length |         Mean |       Error |      StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    /// |----------------- |------- |-------------:|------------:|------------:|-------:|------:|------:|----------:|
    /// |    SerializePipe |    100 |   2,020.9 ns |     5.23 ns |     4.89 ns | 0.1755 |     - |     - |     920 B |
    /// |  SerializePeterO |    100 |   2,143.4 ns |     8.26 ns |     7.73 ns | 3.1891 |     - |     - |   16696 B |
    /// | SerializeDahomey |    100 |     641.9 ns |     1.68 ns |     1.57 ns | 0.0315 |     - |     - |     168 B |
    /// |    SerializePipe |   1000 |   2,463.3 ns |    12.13 ns |    10.75 ns | 0.1755 |     - |     - |     920 B |
    /// |  SerializePeterO |   1000 |   7,021.2 ns |    14.08 ns |    13.18 ns | 3.1891 |     - |     - |   16696 B |
    /// | SerializeDahomey |   1000 |     979.2 ns |     3.12 ns |     2.92 ns | 0.0305 |     - |     - |     168 B |
    /// |    SerializePipe |  10000 |   6,716.5 ns |    19.63 ns |    18.36 ns | 0.2365 |     - |     - |    1272 B |
    /// |  SerializePeterO |  10000 |  55,900.5 ns |   143.43 ns |   119.77 ns | 3.1738 |     - |     - |   16696 B |
    /// | SerializeDahomey |  10000 |   4,512.7 ns |     8.49 ns |     7.53 ns | 0.0305 |     - |     - |     168 B |
    /// |    SerializePipe | 100000 | 116,069.4 ns |   296.56 ns |   277.40 ns | 0.6104 |     - |     - |    3208 B |
    /// |  SerializePeterO | 100000 | 541,354.1 ns | 1,520.09 ns | 1,421.89 ns | 2.9297 |     - |     - |   16701 B |
    /// | SerializeDahomey | 100000 |  50,264.6 ns |   189.44 ns |   167.93 ns |      - |     - |     - |     168 B |
    /// </summary>
    [MemoryDiagnoser]
    public class StreamingBenchmark
    {
        [Params(100,1000,10000,100000)]
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
            var pipe = new Pipe();
            Task writing = FillPipeAsync(document, pipe.Writer);
            Task reading = ReadPipeAsync(pipe.Reader);

            await Task.WhenAll(reading, writing).ConfigureAwait(false);
        }

        [Benchmark]
        public void SerializePeterO()
        {
            cborObject.WriteTo(Stream.Null);
        }

        [Benchmark]
        public Task SerializeDahomey()
        {
            return Cbor.SerializeAsync(document, Stream.Null, CborOptions.Default);
        }

        private static async Task FillPipeAsync(Document document, PipeWriter writer)
        {
            CborWriter cborWriter = new CborWriter(writer);
            var formatter = new ComplexClassFormatter<Document>();
            await formatter.SerializeAsync(cborWriter, document).ConfigureAwait(false);
            //await cborWriter.WriteMap(input).ConfigureAwait(false);
            await cborWriter.FlushAsync().ConfigureAwait(false);
            await writer.CompleteAsync().ConfigureAwait(false);
        }

        private static async Task ReadPipeAsync(PipeReader reader)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                reader.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete
            reader.Complete();
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
