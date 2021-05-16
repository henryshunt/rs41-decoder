using System;

namespace Rs41Decoder.Demodulation
{
    /// <summary>
    /// Represents the base of a demodulator for an RS41 radiosonde where the data to demodulate is in the form of WAV
    /// audio samples.
    /// </summary>
    internal abstract class DemodulatorBase : IDisposable
    {
        /// <summary>
        /// Indicates whether the demodulator is open.
        /// </summary>
        public bool IsOpen { get; protected set; } = false;

        /// <summary>
        /// The size of each WAV sample in bits.
        /// </summary>
        protected int bitsPerWavSample = 16;

        /// <summary>
        /// The number of channels in the WAV data.
        /// </summary>
        protected int numberOfChannels = 1;

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
        /// Initialises a new instance of the <see cref="DemodulatorBase"/> class.
        /// </summary>
        public DemodulatorBase() { }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// Closes the demodulator.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Reads a number of demodulated bits from the WAV data.
        /// </summary>
        /// <remarks>
        /// Demodulated bits are read until the bit value changes. This means that all values in the return array will
        /// be identical.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the demodulator is not open.
        /// </exception>
        public bool[] ReadDemodulatedBits()
        {
            int sampleCount = 0;

            // Read samples until we read one that crosses the zero-point
            do
            {
                if (!IsOpen)
                    throw new InvalidOperationException("The demodulator is not open");

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
        /// <exception cref="InvalidOperationException">
        /// Thrown if the demodulator is not open.
        /// </exception>
        private short ReadWavSample()
        {
            if (!IsOpen)
                throw new InvalidOperationException("The demodulator is not open");

            short sample = 0;

            for (int channel = 0; channel < numberOfChannels; channel++)
            {
                if (!IsOpen)
                    throw new InvalidOperationException("The demodulator is not open");

                byte buffer = ReadWavByte();
                if (channel == 0)
                    sample = buffer;

                if (bitsPerWavSample == 16)
                {
                    if (!IsOpen)
                        throw new InvalidOperationException("The demodulator is not open");

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
        protected abstract byte ReadWavByte();

        public void Dispose() => Close();
    }
}
