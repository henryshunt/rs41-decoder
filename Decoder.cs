using System;
using System.IO;

namespace RS41Decoder
{
    public class Decoder
    {
        private const int HEADER_LENGTH = 32;
        private const int HEADER_OFFSET = 24;
        private const int FRAME_START = (HEADER_OFFSET + HEADER_LENGTH) / 8;
        private const int MASK_LENGTH = 64;

        private const int STANDARD_FRAME_LENGTH = 320;
        private const int XDATA_LENGTH = 198;
        private const int FRAME_LENGTH = STANDARD_FRAME_LENGTH + XDATA_LENGTH;

        private string header = "0000100001101101010100111000100001000100011010010100100000011111";

        private byte[] mask = {
            0x96, 0x83, 0x3E, 0x51, 0xB1, 0x49, 0x08, 0x98, 0x32, 0x05, 0x59, 0x0E, 0xF9, 0x44, 0xC6, 0x26,
            0x21, 0x60, 0xC2, 0xEA, 0x79, 0x5D, 0x6D, 0xA1, 0x54, 0x69, 0x47, 0x0C, 0xDC, 0xE8, 0x5C, 0xF1,
            0xF7, 0x76, 0x82, 0x7F, 0x07, 0x99, 0xA2, 0x2C, 0x93, 0x7C, 0x30, 0x63, 0xF5, 0x10, 0x2E, 0x61,
            0xD0, 0xBC, 0xB4, 0xB6, 0x06, 0xAA, 0xF4, 0x23, 0x78, 0x6E, 0x3B, 0xAE, 0xBF, 0x7B, 0x4C, 0xC1
        };

        private const int POSITION_BLOCK_EMPTY = 0x12B;

        private byte[] frame = new byte[FRAME_LENGTH];

        public char[] buffer = new char[HEADER_LENGTH];
        public int bufferPosition = -1;

        public bool hasFoundHeader;

        public Decoder()
        {
            frame[0] = 0x86;
            frame[1] = 0x35;
            frame[2] = 0xf4;
            frame[3] = 0x40;
            frame[4] = 0x93;
            frame[5] = 0xdf;
            frame[6] = 0x1a;
            frame[7] = 0x60;
        }


        public void IncrementHeaderBuffer()
        {
            bufferPosition = (bufferPosition + 1) % HEADER_LENGTH;
        }

        public void DecodeLoop(BinaryReader reader, Demodulator demodulator)
        {
            char[] bitBuffer = new char[8];
            int bitCount = 0;

            byte b;
            int byteCount = FRAME_START;

            while (true)
            {
                (int, int) bits = demodulator.ReadBits(reader); // Value, count

                if (bits.Item2 == 0)
                {
                    if (byteCount > POSITION_BLOCK_EMPTY)
                    {
                        DecodeFrame();
                        bitCount = 0;
                        byteCount = FRAME_START;
                        hasFoundHeader = false;
                    }

                    continue;
                }

                for (int i = 0; i < bits.Item2; i++)
                {
                    IncrementHeaderBuffer();
                    buffer[bufferPosition] = (char)(0x30 + bits.Item1);

                    if (!hasFoundHeader)
                    {
                        if (CheckForFrameHeader() >= HEADER_LENGTH)
                            hasFoundHeader = true;
                    }
                    else
                    {
                        bitBuffer[bitCount] = (char)bits.Item1;
                        bitCount++;

                        if (bitCount == 8)
                        {
                            bitCount = 0;
                            b = bitsToByte(bitBuffer);

                            frame[byteCount] = (byte)(b ^ mask[byteCount % MASK_LENGTH]);
                            byteCount++;
                        }
                    }
                }
            }
        }

        private int CheckForFrameHeader()
        {
            int i = 0;
            int j = bufferPosition;

            while (i < HEADER_LENGTH)
            {
                if (j < 0)
                    j = HEADER_LENGTH - 1;

                if (buffer[j] != header[HEADER_OFFSET + HEADER_LENGTH - 1 - i])
                    break;

                j--;
                i++;
            }

            return i;
        }

        private byte bitsToByte(char[] bits)
        {
            byte d = 1;
            byte byteval = 0;

            for (int i = 0; i < 8; i++) // little endian
            {
                if (bits[i] == 1)
                    byteval += d;
                else if (bits[i] == 0)
                    byteval += 0;
                else throw new Exception();

                d <<= 1;
            }

            return byteval;
        }

        private void DecodeFrame()
        {
            Console.WriteLine("PRINT FRAME");

            for (int i = 0; i < 320; i++)
                Console.Write(frame[i].ToString("x2"));

            Console.WriteLine();
        }
    }
}
