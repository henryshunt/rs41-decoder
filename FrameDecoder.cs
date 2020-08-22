using System;
using System.Collections.Generic;
using System.Text;

namespace RS41Decoder
{
    public class FrameDecoder
    {
        private byte[] frameBytes = new byte[518];

        private byte[] mask = {
            0x96, 0x83, 0x3E, 0x51, 0xB1, 0x49, 0x08, 0x98, 0x32, 0x05, 0x59, 0x0E, 0xF9, 0x44, 0xC6, 0x26,
            0x21, 0x60, 0xC2, 0xEA, 0x79, 0x5D, 0x6D, 0xA1, 0x54, 0x69, 0x47, 0x0C, 0xDC, 0xE8, 0x5C, 0xF1,
            0xF7, 0x76, 0x82, 0x7F, 0x07, 0x99, 0xA2, 0x2C, 0x93, 0x7C, 0x30, 0x63, 0xF5, 0x10, 0x2E, 0x61,
            0xD0, 0xBC, 0xB4, 0xB6, 0x06, 0xAA, 0xF4, 0x23, 0x78, 0x6E, 0x3B, 0xAE, 0xBF, 0x7B, 0x4C, 0xC1
        };

        private const int MASK_LENGTH = 64;

        public FrameDecoder()
        {

        }


        public void Decode(bool[] frameBits)
        {
            frameBytes = DecodeXor(DecodeBytes(frameBits));

            foreach (byte b in frameBytes)
                Console.Write(b.ToString("x2"));

            Console.WriteLine();
            Console.WriteLine();
        }

        // Takes in array of bits and returns an array of bytes with endianness decoded
        private byte[] DecodeBytes(bool[] frameBits)
        {
            if (frameBits == null)
                throw new ArgumentNullException();

            if (frameBits.Length != 320 * 8)
                throw new ArgumentException();


            byte[] frameBytes = new byte[320];

            // Convert each set of 8 bits into a byte (320 total bytes)
            for (int i = 0; i < 320; i++)
            {
                bool[] byteBits = new bool[8];

                for (int j = 0; j < 8; j++)
                    byteBits[j] = frameBits[(i * 8) + j];

                frameBytes[i] = BoolArrayToByte(byteBits);
            }

            return frameBytes;
        }

        // Takes in an array of bytes and returns an array of XOR-decoded bytes
        private byte[] DecodeXor(byte[] frameBytes)
        {
            if (frameBytes == null)
                throw new ArgumentNullException();

            if (frameBytes.Length != 320)
                throw new ArgumentException();

            for (int i = 0; i < 320; i++)
                frameBytes[i] = (byte)(frameBytes[i] ^ mask[i % MASK_LENGTH]);

            return frameBytes;
        }

        private byte BoolArrayToByte(bool[] bits)
        {
            byte d = 1;
            byte byteval = 0;

            for (int i = 0; i < 8; i++) // little endian
            {
                if (bits[i] == true)
                    byteval += d;
                else if (bits[i] == false)
                    byteval += 0;

                d <<= 1;
            }

            return byteval;
        }
    }
}
