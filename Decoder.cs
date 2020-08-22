using System;
using System.IO;

namespace RS41Decoder
{
    public class Decoder
    {
        private const int STANDARD_FRAME_LENGTH = 320; // Bytes
        private const int XDATA_LENGTH = 198; // Bytes
        private const int FRAME_LENGTH = STANDARD_FRAME_LENGTH; // + XDATA_LENGTH; // Bytes

        private const int HEADER_LENGTH = 64; // Bits
        private const int FRAME_START = (HEADER_LENGTH) / 8; // Hex location
        private const int MASK_LENGTH = 64;

        private const int POSITION_BLOCK_EMPTY = 0x12B; // Hex location


        private readonly bool[] HEADER = {
            false, false, false, false, true, false, false, false, // 00001000
            false, true, true, false, true, true, false, true, // 01101101
            false, true, false, true, false, false, true, true, // 01010011
            true, false, false, false, true, false, false, false, // 10001000
            false, true, false, false, false, true, false, false, // 01000100
            false, true, true, false, true, false, false, true, // 01101001
            false, true, false, false, true, false, false, false, // 01001000
            false, false, false, true, true, true, true, true // 00011111
        };

        private byte[] mask = {
            0x96, 0x83, 0x3E, 0x51, 0xB1, 0x49, 0x08, 0x98, 0x32, 0x05, 0x59, 0x0E, 0xF9, 0x44, 0xC6, 0x26,
            0x21, 0x60, 0xC2, 0xEA, 0x79, 0x5D, 0x6D, 0xA1, 0x54, 0x69, 0x47, 0x0C, 0xDC, 0xE8, 0x5C, 0xF1,
            0xF7, 0x76, 0x82, 0x7F, 0x07, 0x99, 0xA2, 0x2C, 0x93, 0x7C, 0x30, 0x63, 0xF5, 0x10, 0x2E, 0x61,
            0xD0, 0xBC, 0xB4, 0xB6, 0x06, 0xAA, 0xF4, 0x23, 0x78, 0x6E, 0x3B, 0xAE, 0xBF, 0x7B, 0x4C, 0xC1
        };

        public bool[] headerBuffer = new bool[HEADER_LENGTH];
        public int headerBufferPos = -1;
        public bool hasFoundHeader = false;


        public void DecodeLoop(BinaryReader reader, Demodulator demodulator)
        {
            bool[] frameBits = new bool[FRAME_LENGTH * 8];
            Array.Copy(HEADER, frameBits, HEADER_LENGTH);
            int frameBitCount = FRAME_START * 8;

            while (true)
            {
                (int, int) bits; // Value, count

                try
                {
                    bits = demodulator.ReadBits(reader);
                }
                catch (EndOfStreamException)
                {
                    return;
                }

                // If no bits were received from the audio...
                if (bits.Item2 == 0)
                {
                    // ...then if we've received up to the end of a frame, start a new frame
                    if (frameBitCount / 8 > POSITION_BLOCK_EMPTY)
                    {
                        frameBitCount = FRAME_START * 8;
                        hasFoundHeader = false;

                        new FrameDecoder().Decode(frameBits);
                    }

                    continue;
                }

                for (int i = 0; i < bits.Item2; i++)
                {
                    headerBufferPos = (headerBufferPos + 1) % HEADER_LENGTH;
                    headerBuffer[headerBufferPos] = Convert.ToBoolean(bits.Item1);

                    if (!hasFoundHeader)
                    {
                        if (CheckForFrameHeader() >= HEADER_LENGTH)
                            hasFoundHeader = true;
                    }
                    else
                    {
                        frameBits[frameBitCount++] = Convert.ToBoolean(bits.Item1);

                        if (frameBitCount / 8 == FRAME_LENGTH)
                        {
                            frameBitCount = FRAME_START * 8;
                            hasFoundHeader = false;

                            new FrameDecoder().Decode(frameBits);
                        }
                    }
                }
            }
        }

        private int CheckForFrameHeader()
        {
            int i = 0;
            int j = headerBufferPos;

            while (i < HEADER_LENGTH)
            {
                if (j < 0)
                    j = HEADER_LENGTH - 1;

                if (headerBuffer[j] != HEADER[HEADER_LENGTH - 1 - i])
                    break;

                j--;
                i++;
            }

            return i;
        }
    }
}
