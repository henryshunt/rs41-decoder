using System;

namespace RSDecoder.RS41
{
    public class FrameDecoder
    {
        private bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
        private byte[] frameBytes = new byte[Constants.FRAME_LENGTH];

        public FrameDecoder(bool[] frameBits)
        {
            if (frameBits == null)
                throw new ArgumentNullException();

            if (frameBits.Length != Constants.FRAME_LENGTH * 8)
                throw new ArgumentException();

            this.frameBits = frameBits;
        }


        public void Decode()
        {
            DecodeBytes();
            DecodeXor();
        }

        private void DecodeBytes()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
            {
                bool[] byteBits = new bool[8];

                for (int j = 0; j < 8; j++)
                    byteBits[j] = frameBits[(i * 8) + j];

                frameBytes[i] = BoolArrayToByte(byteBits);
            }
        }

        private void DecodeXor()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
                frameBytes[i] = (byte)(frameBytes[i] ^ Constants.XOR_MASK[i % Constants.XOR_MASK_LENGTH]);
        }

        private byte BoolArrayToByte(bool[] bits)
        {
            byte d = 1;
            byte value = 0;

            for (int i = 0; i < 8; i++) // little endian
            {
                if (bits[i] == true)
                    value += d;
                else if (bits[i] == false)
                    value += 0;

                d <<= 1;
            }

            return value;
        }

        public override string ToString()
        {
            string value = "";

            foreach (byte b in frameBytes)
                value += b.ToString("X2");

            return value;
        }
    }
}
