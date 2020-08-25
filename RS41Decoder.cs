using System;
using System.IO;

namespace RSDecoder.RS41
{
    public class RS41Decoder
    {
        private RS41Demodulator demodulator;

        private readonly bool[] headerBuffer = new bool[Constants.FRAME_HEADER.Length];
        private int headerBufferPos = 0;

        public RS41Decoder(RS41Demodulator demodulator)
        {
            if (demodulator == null)
                throw new ArgumentNullException(nameof(demodulator));

            this.demodulator = demodulator;
        }


        public void Decode()
        {
            bool hasFoundHeader = false;

            bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
            Array.Copy(Constants.FRAME_HEADER, frameBits, Constants.FRAME_HEADER.Length);

            int frameBitsPos = Constants.FRAME_HEADER.Length;

            while (true)
            {
                try
                {
                    foreach (bool bit in demodulator.ReadBits())
                    {
                        if (!hasFoundHeader)
                        {
                            headerBuffer[headerBufferPos] = bit;
                            headerBufferPos = (headerBufferPos + 1) % headerBuffer.Length;

                            if (CheckForFrameHeader())
                                hasFoundHeader = true;
                        }
                        else
                        {
                            frameBits[frameBitsPos++] = bit;

                            if (frameBitsPos == frameBits.Length)
                            {
                                frameBitsPos = Constants.FRAME_HEADER.Length;
                                hasFoundHeader = false;

                                FrameDecoder frame = new FrameDecoder(frameBits);
                                frame.Decode();
                                frame.PrintFrameTable();
                            }
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Determines whether the circular buffer <see cref="headerBuffer"/> contains the frame header
        /// bits with the final bit being at index <see cref="headerBufferPos"/> - 1.
        /// </summary>
        /// <returns>true if the header buffer contains the frame header, otherwise false.</returns>
        private bool CheckForFrameHeader()
        {
            int i = 0;
            int j = headerBufferPos - 1;

            while (i < Constants.FRAME_HEADER.Length)
            {
                if (j < 0)
                    j = Constants.FRAME_HEADER.Length - 1;

                if (headerBuffer[j--] != Constants.FRAME_HEADER[Constants.FRAME_HEADER.Length - 1 - i++])
                    break;
            }

            return i == Constants.FRAME_HEADER.Length;
        }
    }
}
