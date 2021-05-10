using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rs41Decoder
{
    /// <summary>
    /// Represents a decoder for the RS41 radiosonde.
    /// </summary>
    public class Rs41Decoder
    {
        /// <summary>
        /// The demodulator.
        /// </summary>
        private readonly Demodulator demodulator;

        /// <summary>
        /// A circular buffer for detecting the frame header.
        /// </summary>
        private readonly bool[] headerBuffer = new bool[Constants.FRAME_HEADER.Length];

        /// <summary>
        /// The index within <see cref="headerBuffer"/> at which to place the next item.
        /// </summary>
        private int headerBufferPos = 0;

        /// <summary>
        /// Manages decoding of the subframes.
        /// </summary>
        private readonly SubframeDecoder subframeDecoder = new SubframeDecoder();

        /// <summary>
        /// Initialises a new instance of the <see cref="Rs41Decoder"/> class.
        /// </summary>
        /// <param name="wavFile">
        /// The path of the WAV file to decode.
        /// </param>
        public Rs41Decoder(string wavFile)
        {
            demodulator = new Demodulator(wavFile);
        }

        /// <summary>
        /// Attempts to decodes the RS41 radiosonde data contained within the WAV file.
        /// </summary>
        /// <returns>
        /// The frames decoded from the contents of the WAV file.
        /// </returns>
        public Task<List<Rs41Frame>> Decode()
        {
            return Task.Run(() =>
            {
                List<Rs41Frame> frames = new List<Rs41Frame>();
                bool hasFoundHeader = false;

                try
                {
                    demodulator.Open();
                }
                catch (EndOfStreamException)
                {
                    return frames;
                }

                bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
                Array.Copy(Constants.FRAME_HEADER, frameBits, Constants.FRAME_HEADER.Length);
                int frameBitsPos = Constants.FRAME_HEADER.Length;

                while (true)
                {
                    try
                    {
                        foreach (bool bit in demodulator.ReadDemodulatedBits())
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
            });
        }

        /// <summary>
        /// Determines whether <see cref="headerBuffer"/> contains (in a circular fashion) all bits of the frame
        /// header. Index <see cref="headerBufferPos"/> - 1 is treated as the final bit in the circle.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if <see cref="headerBuffer"/> contains the complete frame header, otherwise
        /// <see langword="false"/>.
        /// </returns>
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
