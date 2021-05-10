using System;
using System.IO;
using System.Text;

namespace RSDecoder
{
    /// <summary>
    /// Represents a WAV file demodulator for the RS41 radiosonde.
    /// </summary>
    internal class Demodulator : IDisposable
    {
        /// <summary>
        /// The number of channels in the WAV file.
        /// </summary>
        private int numberOfChannels;

        /// <summary>
        /// The sample rate of the WAV file.
        /// </summary>
        private int sampleRate;

        /// <summary>
        /// The size of each WAV sample in bits.
        /// </summary>
        private int bitsPerWavSample;

        /// <summary>
        /// The number of WAV samples that make up one demodulated bit.
        /// </summary>
        private double samplesPerDemodBit;

        /// <summary>
        /// The path of the WAV file to demodulate.
        /// </summary>
        private readonly string wavFile;

        /// <summary>
        /// A <see cref="FileStream"/> for the WAV file.
        /// </summary>
        private FileStream? wavStream = null;

        /// <summary>
        /// A <see cref="BinaryReader"/> for the WAV file.
        /// </summary>
        private BinaryReader? wavReader = null;

        /// <summary>
        /// Indicates whether the last WAV sample to be read was above (1) or below (-1) the zero-point.
        /// </summary>
        private int currentSampleSign = 1;

        /// <summary>
        /// Indicates whether the second-to-last WAV sample to be read was above (1) or below (-1) the zero-point.
        /// </summary>
        private int previousSampleSign = 1;

        /// <summary>
        /// Initialises a new instance of the <see cref="Demodulator"/> class.
        /// </summary>
        /// <param name="wavFile">
        /// The path of the WAV file to demodulate.
        /// </param>
        public Demodulator(string wavFile)
        {
            this.wavFile = wavFile;
        }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        /// <exception cref="DemodulatorException">
        /// Thrown if the number of bits per WAV sample sample is unsupported.
        /// </exception>
        public void Open()
        {
            wavStream = File.OpenRead(wavFile);
            wavReader = new BinaryReader(wavStream);

            ReadWavHeader();

            if (bitsPerWavSample != 8 && bitsPerWavSample != 16)
                throw new DemodulatorException("The number of bits per WAV sample is unsuported");
        }

        /// <summary>
        /// Closes the demodulator.
        /// </summary>
        public void Close()
        {
            wavReader?.Close();
            wavStream?.Close();
        }

        /// <summary>
        /// Reads a number of demodulated bits from the WAV file.
        /// </summary>
        /// <remarks>
        /// Demodulated bits are read until the bit value changes. This means that all values in the return array will
        /// be identical.
        /// </remarks>
        /// <returns>
        /// The read demodulated bits.
        /// </returns>
        public bool[] ReadDemodulatedBits()
        {
            int sampleCount = 0;

            // Read samples until we read one that crosses the zero-point
            do
            {
                short sample = ReadWavSample();

                previousSampleSign = currentSampleSign;
                currentSampleSign = (sample >= 0) ? 1 : -1;
                sampleCount++;
            }
            while (currentSampleSign == previousSampleSign);

            // Calculate how many bits we have in the sequence of samples
            double bitCount = sampleCount / samplesPerDemodBit;
            int bitCount2 = (int)(bitCount + 0.5);

            bool[] bits = new bool[bitCount2];

            for (int i = 0; i < bitCount2; i++)
                bits[i] = previousSampleSign != -1;

            return bits;
        }

        /// <summary>
        /// Reads a single WAV sample from the file (using <see cref="wavReader"/>).
        /// </summary>
        /// <returns>
        /// The value of the read WAV sample.
        /// </returns>
        private short ReadWavSample()
        {
            short sample = 0;

            for (int channel = 0; channel < numberOfChannels; channel++)
            {
                byte buffer = wavReader!.ReadByte();

                if (channel == 0)
                    sample = buffer;

                if (bitsPerWavSample == 16)
                {
                    buffer = wavReader.ReadByte();

                    if (channel == 0)
                        sample += (short)(buffer << 8);
                }
            }

            if (bitsPerWavSample == 8)
                return (short)(sample - 128);
            else return sample;
        }

        /// <summary>
        /// Reads the WAV header from the file (using <see cref="wavReader"/>), populating various related members, and
        /// advances to the start of the data section of the file.
        /// </summary>
        /// <exception cref="EndOfStreamException">
        /// Thrown if the end of the stream is reached.
        /// </exception>
        /// <exception cref="DemodulatorException">
        /// Thrown if the WAV header does not conform to the required format.
        /// </exception>
        private void ReadWavHeader()
        {
            byte[] buffer = new byte[4];

            // Check for RIFF chunk
            if (wavReader!.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            if (Encoding.UTF8.GetString(buffer) != "RIFF")
                throw new DemodulatorException("WAV file does not contain RIFF chunk");

            // Skip along
            if (wavReader.ReadBytes(4).Length < 4)
                throw new EndOfStreamException();

            // Check RIFF chunk format is WAVE
            if (wavReader.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            if (Encoding.UTF8.GetString(buffer) != "WAVE")
                throw new DemodulatorException("WAV file data format is not WAVE");

            // Check for fmt subchunk
            if (wavReader.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            if (Encoding.UTF8.GetString(buffer) != "fmt ")
                throw new DemodulatorException("WAV file does not contain fmt subchunk");

            // Skip along
            if (wavReader.ReadBytes(6).Length < 6)
                throw new EndOfStreamException();

            // Read number of channels
            if (wavReader.Read(buffer, 0, 2) < 2)
                throw new EndOfStreamException();

            numberOfChannels = buffer[0] + (buffer[1] << 8);

            // Read sample rate
            if (wavReader.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            sampleRate = BitConverter.ToInt32(buffer);
            samplesPerDemodBit = sampleRate / (double)Constants.BAUD_RATE;

            // Skip along
            if (wavReader.ReadBytes(6).Length < 6)
                throw new EndOfStreamException();

            // Read bits per sample
            if (wavReader.Read(buffer, 0, 2) < 2)
                throw new EndOfStreamException();

            bitsPerWavSample = buffer[0] + (buffer[1] << 8);

            // Check for data subchunk
            if (wavReader.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            if (Encoding.UTF8.GetString(buffer) != "data")
                throw new DemodulatorException("WAV file does not contain data subchunk");

            // Skip along to start of data
            if (wavReader.ReadBytes(4).Length < 4)
                throw new EndOfStreamException();
        }

        /// <summary>
        /// Disposes the demodulator.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
