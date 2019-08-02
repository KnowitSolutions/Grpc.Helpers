using System;
using System.Linq;
using System.Text;

namespace Grpc.Web
{
    public abstract class Base64
    {
        public abstract int InputBlockSize { get; }
        public abstract int OutputBlockSize { get; }

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        protected const byte InvalidChar = 0xFF;
        protected const byte Padding = 0xFE;
        protected static readonly byte PaddingChar = Encoding.ASCII.GetBytes("=").First();
        protected static readonly byte[] EncodeLookup;
        protected static readonly byte[] DecodeLookup;

        static Base64()
        {
            EncodeLookup = Encoding.ASCII.GetBytes(Alphabet);
            DecodeLookup = Enumerable.Repeat(InvalidChar, byte.MaxValue).ToArray();
            DecodeLookup[PaddingChar] = Padding;
            for (byte i = 0; i < EncodeLookup.Length; i++)
            {
                DecodeLookup[EncodeLookup[i]] = i;
            }
        }

        protected abstract unsafe int ProcessBlock(byte* input, ref byte* output, ref long remaining, int state);
        public abstract long Finalize(Memory<byte> output, long offset, int state);

        public long RequiredBufferSize(long length)
        {
            var fullBlocks = length / InputBlockSize;
            var leftoverBlocks = length % InputBlockSize > 0 ? 1 : 0;
            var blocks = fullBlocks + leftoverBlocks;
            var bytes = blocks * OutputBlockSize;
            return bytes;
        }
        
        public unsafe void ProcessBlock(ReadOnlyMemory<byte> input, Memory<byte> output, ref long offset, ref int state)
        {
            using var inputHandle = input.Pin();
            using var outputHandle = output.Pin();
            
            var inputStart = (byte*) inputHandle.Pointer;
            var outputStart = (byte*) outputHandle.Pointer;
            var inputView = inputStart;
            var outputView = outputStart + offset;
            long remaining = input.Length;

            state = ProcessBlock(inputView, ref outputView, ref remaining, state);
            offset = outputView - outputStart;
        }

        protected unsafe void Advance(ref byte* input, ref byte* output)
        {
            input += InputBlockSize;
            output += OutputBlockSize;
        }
    }
}