using System;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Grpc.Web.Test
{
    public class Base64EncoderTests
    {
        private static readonly Random Random = new Random();

        [Test]
        [Repeat(10)]
        [TestCase(false)]
        [TestCase(true)]
        public void TestOneOffEncoding(bool pad)
        {
            var input = GetData();

            long length = 0;
            var leftover = 0;
            var encoder = new Base64Encoder {Pad = pad};
            var output = new byte[encoder.RequiredBufferSize(input.Length)];
            
            encoder.ProcessBlock(input, output, ref length, ref leftover);
            length = (int) encoder.Finalize(output, length, leftover);
            output = output[..(int) length];

            var correctOutput = CorrectOutput(pad, input);
            Assert.AreEqual(input.Length % encoder.InputBlockSize, leftover);
            Assert.AreEqual(correctOutput, output);
        }

        [Test]
        [Repeat(10)]
        [TestCase(false)]
        [TestCase(true)]
        public void TestContinuedEncoding(bool pad)
        {
            var count = Random.Next(1, 10);
            var inputs = Enumerable.Range(0, count).Select(_ => GetData()).ToArray();
            var inputLength = inputs.Sum(input => input.Length);

            long length = 0;
            var state = 0;
            var encoder = new Base64Encoder {Pad = pad};
            var output = new byte[encoder.RequiredBufferSize(inputLength)];

            foreach (var input in inputs)
            {
                encoder.ProcessBlock(input, output, ref length, ref state);
            }
            
            length = encoder.Finalize(output, length, state);
            output = output[..(int) length];

            var correctOutput = CorrectOutput(pad, inputs);
            Assert.AreEqual(correctOutput, output);
        }

        private static byte[] GetData(int maxCount = 1024)
        {
            var count = Random.Next(1, maxCount);
            var data = new byte[count];
            Random.NextBytes(data);
            return data;
        }

        private static byte[] CorrectOutput(bool pad, params byte[][] data)
        {
            var transformed = Convert.ToBase64String(data.SelectMany(x => x).ToArray());
            if (!pad) transformed = transformed.TrimEnd('=');
            return Encoding.ASCII.GetBytes(transformed);
        }
    }
}