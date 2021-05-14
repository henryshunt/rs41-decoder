using System;
using System.Threading;

namespace Rs41Decoder
{
    /// <summary>
    /// Represents the base of a demodulator for an RS41 radiosonde where the data to demodulate is in the form of WAV
    /// audio samples.
    /// </summary>
    internal abstract class WavDemodulator
    {
        /// <summary>
        /// Used for cancelling the demodulation.
        /// </summary>
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Indicates whether the demodulator is open.
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        /// <summary>
        /// The number of channels in the WAV data.
        /// </summary>
        protected int? numberOfChannels = null;

        /// <summary>
        /// The size of each WAV sample in bits.
        /// </summary>
        protected int? bitsPerWavSample = null;

        /// <summary>
        /// The number of WAV samples that make up one demodulated bit.
        /// </summary>
        protected double? samplesPerDemodBit = null;

        /// <summary>
        /// Indicates whether the last WAV sample to be read was above (1) or below (-1) the zero-point.
        /// </summary>
        private int currentSampleSign = 1;

        /// <summary>
        /// Indicates whether the second-to-last WAV sample to be read was above (1) or below (-1) the zero-point.
        /// </summary>
        private int previousSampleSign = 1;

        /// <summary>
        /// Initialises a new instance of the <see cref="WavDemodulator"/> class.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/>, used for cancelling the demodulation.
        /// </param>
        public WavDemodulator(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Reads a number of demodulated bits from the WAV data.
        /// </summary>
        /// <remarks>
        /// Demodulated bits are read until the bit value changes. This means that all values in the return array will
        /// be identical.
        /// </remarks>
        /// <returns>
        /// The demodulated bits.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the demodulation is cancelled.
        /// </exception>
        public bool[] ReadDemodulatedBits()
        {
            int sampleCount = 0;

            // Read samples until we read one that crosses the zero-point
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                short sample = ReadWavSample();
                previousSampleSign = currentSampleSign;
                currentSampleSign = (sample >= 0) ? 1 : -1;
                sampleCount++;
            }
            while (currentSampleSign == previousSampleSign);

            // Calculate how many bits we have in the sequence of samples
            double bitCount = sampleCount / (double)samplesPerDemodBit!;
            int bitCount2 = (int)(bitCount + 0.5);

            bool[] bits = new bool[bitCount2];

            for (int i = 0; i < bitCount2; i++)
                bits[i] = previousSampleSign != -1;

            return bits;
        }

        /// <summary>
        /// Reads a single WAV sample from the WAV data.
        /// </summary>
        /// <returns>
        /// The WAV sample value.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown if the demodulation is cancelled.
        /// </exception>
        private short ReadWavSample()
        {
            short sample = 0;

            for (int channel = 0; channel < numberOfChannels; channel++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte buffer = ReadWavByte();
                if (channel == 0)
                    sample = buffer;

                if (bitsPerWavSample == 16)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    buffer = ReadWavByte();
                    if (channel == 0)
                        sample += (short)(buffer << 8);
                }
            }

            if (bitsPerWavSample == 8)
                return (short)(sample - 128);
            else return sample;
        }

        /// <summary>
        /// Reads a byte from the WAV data.
        /// </summary>
        /// <returns>
        /// The byte.
        /// </returns>
        protected abstract byte ReadWavByte();
    }
}
