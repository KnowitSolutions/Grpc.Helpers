using System;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Knowit.Grpc.Web.Tests
{
    public class Base64DecoderTests
    {
        private static readonly Random Random = new Random();

        [Test]
        [Repeat(10)]
        [TestCase(true)]
        [TestCase(false)]
        public void TestOneOffDecoding(bool padded)
        {
            var (inputs, correctOutput) = GetData();
            if (!padded)
            {
                var padding = Encoding.ASCII
                    .GetBytes("=")
                    .First();
                inputs[0] = inputs[0]
                    .TakeWhile(x => x != padding)
                    .ToArray();
            }

            long length = 0;
            var state = 0;
            var decoder = new Base64Decoder();
            var output = new byte[decoder.RequiredBufferSize(inputs[0].Length)];
            
            decoder.ProcessBlock(inputs[0], output, ref length, ref state);
            length = (int) decoder.Finalize(output, length, state);
            output = output[..(int) length];

            Assert.AreEqual(correctOutput, output);
        }

        [Test]
        [Repeat(10)]
        public void TestContinuedDecoding()
        {
            var count = Random.Next(1, 10);
            var scaling = Random.Next(1, 10);
            var (inputs, correctOutput) = GetData(count * scaling, count, 1024 / scaling);
            var inputLength = inputs.Sum(input => input.Length);

            long length = 0;
            var state = 0;
            var decoder = new Base64Decoder();
            var output = new byte[decoder.RequiredBufferSize(inputLength)];

            foreach (var input in inputs)
            {
                decoder.ProcessBlock(input, output, ref length, ref state);
            }
            
            length = decoder.Finalize(output, length, state);
            output = output[..(int) length];

            Assert.AreEqual(correctOutput, output);
        }

        private static (byte[][], byte[]) GetData(
            int inputSlices = 1,
            int outputSlices = 1,
            int maxCount = 1024)
        {
            var unencoded = Enumerable
                .Range(0, inputSlices)
                .Select(_ => Random.Next(1, maxCount))
                .Select(x =>
                {
                    var data = new byte[x];
                    Random.NextBytes(data);
                    return data;
                })
                .ToArray();
            
            var encoded = unencoded
                .Select(Convert.ToBase64String)
                .Select(Encoding.ASCII.GetBytes)
                .SelectMany(x => x)
                .ToArray();

            var splits = Enumerable
                .Range(0, outputSlices - 1)
                .Select(_ => Random.Next(1, encoded.Length - 1))
                .OrderBy(x => x)
                .Append(encoded.Length)
                .ToArray();

            var chunks = splits
                .Prepend(0)
                .Zip(splits)
                .Select(x => encoded[x.First..x.Second])
                .ToArray();

            var correct = unencoded
                .SelectMany(x => x)
                .ToArray();

            return (chunks, correct);
        }
    }
}