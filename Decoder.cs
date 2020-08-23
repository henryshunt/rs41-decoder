using System;
using System.Diagnostics;
using System.IO;

namespace RSDecoder.RS41
{
    public class Decoder
    {
        public bool[] headerBuffer = new bool[Constants.HEADER_LENGTH_BITS];
        public int headerBufferPos = -1;
        public bool hasFoundHeader = false;

        public void DecodingLoop(BinaryReader reader, Demodulator demodulator)
        {
            bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
            Array.Copy(Constants.HEADER_BITS, frameBits, Constants.HEADER_LENGTH_BITS);
            int frameBitCount = Constants.POS_POST_HEADER * 8;

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
                    if (frameBitCount / 8 > Constants.POS_BLOCK_EMPTY)
                    {
                        frameBitCount = Constants.POS_POST_HEADER * 8;
                        hasFoundHeader = false;

                        FrameDecoder frame = new FrameDecoder(frameBits);
                        frame.Decode();

                        //Console.WriteLine(frame.ToString());
                        frame.PrintFrameTable();
                    }

                    continue;
                }

                for (int i = 0; i < bits.Item2; i++)
                {
                    headerBufferPos = (headerBufferPos + 1) % Constants.HEADER_LENGTH_BITS;
                    headerBuffer[headerBufferPos] = Convert.ToBoolean(bits.Item1);

                    if (!hasFoundHeader)
                    {
                        if (CheckForFrameHeader() >= Constants.HEADER_LENGTH_BITS)
                            hasFoundHeader = true;
                    }
                    else
                    {
                        frameBits[frameBitCount++] = Convert.ToBoolean(bits.Item1);

                        if (frameBitCount / 8 == Constants.FRAME_LENGTH)
                        {
                            frameBitCount = Constants.POS_POST_HEADER * 8;
                            hasFoundHeader = false;

                            FrameDecoder frame = new FrameDecoder(frameBits);
                            frame.Decode();

                            //Console.WriteLine(frame.ToString());
                            frame.PrintFrameTable();
                        }
                    }
                }
            }
        }

        private int CheckForFrameHeader()
        {
            int i = 0;
            int j = headerBufferPos;

            while (i < Constants.HEADER_LENGTH_BITS)
            {
                if (j < 0)
                    j = Constants.HEADER_LENGTH_BITS - 1;

                if (headerBuffer[j] != Constants.HEADER_BITS[Constants.HEADER_LENGTH_BITS - 1 - i])
                    break;

                j--;
                i++;
            }

            return i;
        }
    }
}
