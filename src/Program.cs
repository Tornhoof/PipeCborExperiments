using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using PeterO.Cbor;

namespace StreamingCbor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BenchmarkRunner.Run<StreamingBenchmark>();
            //var sb = new StreamingBenchmark();
            //sb.Length = 1000;
            //sb.Setup();
            //await sb.SerializePipe();

            //var po = new PipeOptions();
            //var pipe = new Pipe(po);
            //var map = new Dictionary<string, object>
            //{
            //    {"Hello", "World"},
            //    {"This", Encoding.UTF8.GetBytes("Universe")},
            //};
            //Task writing = FillPipeAsync(map, pipe.Writer);
            //Task reading = ReadPipeAsync(pipe.Reader);

            //await Task.WhenAll(reading, writing).ConfigureAwait(false);

            //var output = await (Task<Dictionary<string, string>>)reading;

        }

        private static async Task FillPipeAsync(Dictionary<string, object> input, PipeWriter writer)
        {
            CborWriter cborWriter = new CborWriter(writer);
            var document = new Document { Key = GetText(1000), Value = GetText(1000)};
            var formatter = new ComplexClassFormatter<Document>();
            await formatter.SerializeAsync(cborWriter, document).ConfigureAwait(false);
            //await cborWriter.WriteMap(input).ConfigureAwait(false);
            await cborWriter.FlushAsync().ConfigureAwait(false);
            await writer.CompleteAsync().ConfigureAwait(false);
        }

        private static async Task<Dictionary<string, string>> ReadPipeAsync(PipeReader reader)
        {
            await using var memoryStream = new MemoryStream();
            
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                await memoryStream.WriteSequenceAsync(result.Buffer).ConfigureAwait(false);
                reader.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete
            reader.Complete();
            memoryStream.Position = 0;
            var o = CBORObject.Read(memoryStream);
            var text = o.ToObject<Dictionary<string, string>>();
            return text;
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