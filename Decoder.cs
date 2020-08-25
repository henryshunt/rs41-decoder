using System;
using System.IO;

namespace RSDecoder.RS41
{
    public class Decoder
    {
        public bool[] headerBuffer = new bool[Constants.FRAME_HEADER.Length];
        public int headerBufferPos = -1;
        public bool hasFoundHeader = false;

        public void DecodingLoop(BinaryReader reader, Demodulator demodulator)
        {
            bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
            Array.Copy(Constants.FRAME_HEADER, frameBits, Constants.FRAME_HEADER.Length);

            int frameBitCount = Constants.POS_ECC * 8;

            while (true)
            {
                foreach (bool bit in demodulator.ReadBits(reader))
                {
                    headerBufferPos = (headerBufferPos + 1) % Constants.FRAME_HEADER.Length;
                    headerBuffer[headerBufferPos] = bit;

                    if (!hasFoundHeader)
                    {
                        if (CheckForFrameHeader())
                            hasFoundHeader = true;
                    }
                    else
                    {
                        frameBits[frameBitCount++] = bit;

                        if (frameBitCount / 8 == Constants.FRAME_LENGTH)
                        {
                            frameBitCount = Constants.POS_ECC * 8;
                            hasFoundHeader = false;

                            FrameDecoder frame = new FrameDecoder(frameBits);
                            frame.Decode();
                            frame.PrintFrameTable();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the circular header buffer contains the frame header.
        /// </summary>
        /// <returns>
        /// true if the header buffer contains the frame header, otherwise false.
        /// </returns>
        private bool CheckForFrameHeader()
        {
            int i = 0;
            int j = headerBufferPos;

            while (i < Constants.FRAME_HEADER.Length)
            {
                if (j < 0)
                    j = Constants.FRAME_HEADER.Length - 1;

                if (headerBuffer[j] != Constants.FRAME_HEADER[Constants.FRAME_HEADER.Length - 1 - i])
                    break;

                j--;
                i++;
            }

            return i == Constants.FRAME_HEADER.Length ? true : false;
        }
    }
}
