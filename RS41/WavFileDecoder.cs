using System;
using System.Collections.Generic;
using System.IO;

namespace RSDecoder.RS41
{
    /// <summary>
    /// Represents a decoder for a static WAV file.
    /// </summary>
    public class WavFileDecoder
    {
        /// <summary>
        /// The demodulator.
        /// </summary>
        private readonly WavFileDemodulator demodulator;

        /// <summary>
        /// A circular buffer for detecting the frame header.
        /// </summary>
        private readonly bool[] headerBuffer = new bool[Constants.FRAME_HEADER.Length];

        /// <summary>
        /// The index within <see cref="headerBuffer"/> at which to place the next bit.
        /// </summary>
        private int headerBufferPos = 0;

        /// <summary>
        /// For decoding subframes.
        /// </summary>
        private readonly SubframeDecoder subframeDecoder = new SubframeDecoder();
    
        /// <summary>
        /// Initialises a new instance of the <see cref="WavFileDecoder"/> class.
        /// </summary>
        /// <param name="wavPath">The WAV file to decode.</param>
        public WavFileDecoder(string wavPath)
        {
            demodulator = new WavFileDemodulator(wavPath);
        }

        /// <summary>
        /// Decodes the radiosonde data in the WAV file.
        /// </summary>
        /// <returns>A list of the frames decoded from the WAV file.</returns>
        public List<Frame> Decode()
        {
            List<Frame> frames = new List<Frame>();
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
                                frames.Add(new FrameDecoder(frameBits, subframeDecoder).Decode());

                                frameBitsPos = Constants.FRAME_HEADER.Length;
                                hasFoundHeader = false;
                            }
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    return frames;
                }
            }
        }

        /// <summary>
        /// Determines whether <see cref="headerBuffer"/> contains (in a circular fashion) all bits of the frame header.
        /// Index <see cref="headerBufferPos"/> - 1 is treated as the final bit in the circle.
        /// </summary>
        /// <returns><see langword="true"/> if <see cref="headerBuffer"/> contains the frame header, otherwise
        /// <see langword="false"/>.</returns>
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
