using System;

namespace Knowit.Grpc.Web
{
    public class Base64Decoder : Base64
    {
        public override int InputBlockSize { get; } = 4;
        public override int OutputBlockSize { get; } = 3;

        private const int FirstByte = 0;
        private const int SecondByte = 1;
        private const int ThirdByte = 2;
        private const int ForthByte = 3;
        private const int FirstPadding = -1;
        private const int SecondPadding = -2;
        private const int ThirdPadding = -3;
        private const int ForthPadding = -4;

        protected override unsafe int ProcessBlock(byte* input, ref byte* output, ref long remaining, int state)
        {
            byte @byte;
            
            switch (state)
            {
                case FirstByte:
                    if (!TryGetNextByte(input, remaining, out @byte)) return FirstByte;
                    if (@byte == Padding) goto case FirstPadding;
                    
                    output[0] = (byte) ((@byte & 0b00111111) << 2);
                    
                    input++;
                    remaining--;
                    goto case SecondByte;

                case SecondByte:
                    if (!TryGetNextByte(input, remaining, out @byte)) return SecondByte;
                    if (@byte == Padding) goto case SecondPadding;

                    output[0] |= (byte) ((@byte & 0b00110000) >> 4);
                    output[1] = (byte) ((@byte & 0b00001111) << 4);
                    
                    input++;
                    output++;
                    remaining--;
                    goto case ThirdByte;

                case ThirdByte:
                    if (!TryGetNextByte(input, remaining, out @byte)) return ThirdByte;
                    if (@byte == Padding) goto case ThirdPadding;
                    
                    output[0] |= (byte) ((@byte & 0b00111100) >> 2);
                    output[1] = (byte) ((@byte & 0b00000011) << 6);
                    
                    input++;
                    output++;
                    remaining--;
                    goto case ForthByte;

                case ForthByte:
                    if (!TryGetNextByte(input, remaining, out @byte)) return ForthByte;
                    if (@byte == Padding) goto case ForthPadding;
                    
                    output[0] |= (byte) (@byte & 0b00111111);
                    
                    input++;
                    output++;
                    remaining--;
                    goto case FirstByte;
                    
                case FirstPadding:
                case SecondPadding:
                    throw new FormatException("Padding at invalid location");
                    
                case ThirdPadding:
                    input++;
                    remaining--;
                    goto case ForthPadding;
                    
                case ForthPadding:
                    if (!TryGetNextByte(input, remaining, out @byte)) return ForthPadding;
                    if (@byte != Padding) throw new FormatException("Padding unexpectedly ended");

                    input++;
                    remaining--;
                    goto case FirstByte;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        public override long Finalize(Memory<byte> output, long offset, int state)
        {
            switch (state)
            {
                case FirstByte:
                case ThirdByte:
                case ForthPadding:
                case ForthByte:
                    return offset;
                
                case SecondByte:
                    throw new FormatException("Incomplete base64 string");
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static unsafe bool TryGetNextByte(byte* bytes, long remaining, out byte @byte)
        {
            if (remaining > 0)
            {
                @byte = DecodeLookup[bytes[0]];
                if (@byte == InvalidChar) throw new FormatException("Invalid base64 string");
                return true;
            }

            @byte = default;
            return false;
        }
    }
}