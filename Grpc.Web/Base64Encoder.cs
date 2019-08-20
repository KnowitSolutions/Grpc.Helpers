using System;

namespace Knowit.Grpc.Web
{
    public class Base64Encoder : Base64
    {
        public override int InputBlockSize { get; } = 3;
        public override int OutputBlockSize { get; } = 4;
        public bool Pad { get; set; } = true;
        
        private const int FirstByte = 0;
        private const int SecondByte = 1;
        private const int ThirdByte = 2;

        protected override unsafe int ProcessBlock(byte* input, ref byte* output, ref long remaining, int state)
        {
            switch (state)
            {
                case FirstByte:
                    if (remaining == 0) return FirstByte;
                    
                    output[0] = (byte) (input[0] >> 2 & 0b00111111);
                    output[1] = (byte) (input[0] << 4 & 0b00110000);
                    output[0] = EncodeLookup[output[0]];

                    input++;
                    output++;
                    remaining--;
                    goto case SecondByte;

                case SecondByte:
                    if (remaining == 0) return SecondByte;
                    
                    output[0] |= (byte) (input[0] >> 4 & 0b00001111);
                    output[1] = (byte) (input[0] << 2 & 0b00111100);
                    output[0] = EncodeLookup[output[0]];
                    
                    input++;
                    output++;
                    remaining--;
                    goto case ThirdByte;

                case ThirdByte:
                    if (remaining == 0) return ThirdByte;
                    
                    output[0] |= (byte) (input[0] >> 6 & 0b00000011);
                    output[1] = (byte) (input[0] & 0b00111111);
                    output[0] = EncodeLookup[output[0]];
                    output[1] = EncodeLookup[output[1]];

                    input++;
                    output += 2;
                    remaining--;
                    goto case FirstByte;
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        public override unsafe long Finalize(Memory<byte> output, long offset, int state)
        {
            using var outputHandle = output.Pin();
            
            var outputStart = (byte*) outputHandle.Pointer;
            var outputView = outputStart + offset;

            FinalizeEncoding(ref outputView, ref offset, state);
            if (Pad) FinalizePadding(ref outputView, ref offset, state);
            
            return offset;
        }

        private unsafe void FinalizeEncoding(ref byte* output, ref long length, int state)
        {
            switch (state)
            {
                case FirstByte:
                    break;
                
                case SecondByte:
                    output[0] = EncodeLookup[output[0]];
                    
                    output++;
                    length++;
                    break;
                
                case ThirdByte:
                    output[0] = EncodeLookup[output[0]];
                    
                    output++;
                    length++;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private unsafe void FinalizePadding(ref byte* output, ref long length, int state)
        {
            switch (state)
            {
                case FirstByte:
                    break;
                    
                case SecondByte:
                    output[0] = PaddingChar;
                    
                    output++;
                    length++;
                    goto case ThirdByte;
                    
                case ThirdByte:
                    output[0] = PaddingChar;
                    
                    output++;
                    length++;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }
    }
}