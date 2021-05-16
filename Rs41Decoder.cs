using Rs41Decoder.Decoding;
using Rs41Decoder.Demodulation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Constants = Rs41Decoder.Decoding.Constants;

namespace Rs41Decoder
{
    /// <summary>
    /// Represents a decoder for the RS41 radiosonde.
    /// </summary>
    public class Rs41Decoder : IDisposable
    {
        private readonly DemodulatorBase demodulator;

        /// <summary>
        /// Indicates whether the decoder is decoding.
        /// </summary>
        public bool IsDecoding { get; private set; } = false;

        /// <summary>
        /// Used for cancelling the decoding.
        /// </summary>
        private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();

        /// <summary>
        /// A circular buffer for detecting the frame header.
        /// </summary>
        private readonly bool[] headerBuffer = new bool[Constants.FRAME_HEADER.Length];

        /// <summary>
        /// The index within <see cref="headerBuffer"/> at which to place the next item.
        /// </summary>
        private int headerBufferPos = 0;

        private readonly SubframeDecoder subframeDecoder = new SubframeDecoder();

        /// <summary>
        /// Occurs when a frame is decoded.
        /// </summary>
        public event EventHandler<FrameDecodedEventArgs>? FrameDecoded;

        /// <summary>
        /// Initialises a new instance of the <see cref="Rs41Decoder"/> class with the data source being a pre-recorded
        /// WAV file.
        /// </summary>
        /// <param name="wavFile">
        /// The path of the WAV file to use as the data source.
        /// </param>
        public Rs41Decoder(string wavFile)
        {
            demodulator = new FileDemodulator(wavFile);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="Rs41Decoder"/> class with the data source being an audio input
        /// device.
        /// </summary>
        /// <param name="deviceNumber">
        /// The number of the audio input device to use as the data source.
        /// </param>
        public Rs41Decoder(int deviceNumber)
        {
            demodulator = new LiveDemodulator(deviceNumber);
        }

        /// <summary>
        /// Starts the process of decoding the frames contained within the data source.
        /// </summary>
        public Task StartDecodingAsync()
        {
            return Task.Run(() =>
            {
                if (IsDecoding)
                    throw new InvalidOperationException("Decoding has already started");
                IsDecoding = true;

                try
                {
                    demodulator.Open();

                    bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
                    Array.Copy(Constants.FRAME_HEADER, frameBits, Constants.FRAME_HEADER.Length);
                    int frameBitsPos = Constants.FRAME_HEADER.Length;

                    bool hasFoundHeader = false;

                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        foreach (bool bit in demodulator.ReadDemodulatedBits())
                        {
                            if (cancellationToken.IsCancellationRequested)
                                return;

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
                                    Rs41Frame frame = new FrameDecoder(frameBits, subframeDecoder).Decode();

                                    frameBitsPos = Constants.FRAME_HEADER.Length;
                                    hasFoundHeader = false;

                                    FrameDecoded?.Invoke(this, new FrameDecodedEventArgs(frame));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                when (ex is EndOfStreamException ||
                    (ex is InvalidOperationException && ex.Message == "The demodulator is not open"))
                { }
                catch
                {
                    demodulator.Close();
                    IsDecoding = false;
                    throw;
                }

                demodulator.Close();
                IsDecoding = false;
            });
        }

        /// <summary>
        /// Stops the decoding.
        /// </summary>
        public void StopDecoding()
        {
            cancellationToken?.Cancel();
            demodulator.Close();
            IsDecoding = false;
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

        public void Dispose()
        {
            StopDecoding();
            demodulator?.Dispose();
        }
    }
}
