using System;
using System.IO;
using System.Text;

namespace Rs41Decoder.Demodulation
{
    /// <summary>
    /// Represents a demodulator for the RS41 radiosonde where the data source is a pre-recorded WAV file.
    /// </summary>
    internal class FileDemodulator : DemodulatorBase
    {
        /// <summary>
        /// The path of the WAV file to demodulate.
        /// </summary>
        private readonly string wavFile;

        private FileStream? wavStream = null;
        private BinaryReader? wavReader = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileDemodulator"/> class.
        /// </summary>
        /// <param name="wavFile">
        /// The path of the WAV file to demodulate.
        /// </param>
        public FileDemodulator(string wavFile)
        {
            this.wavFile = wavFile;
        }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the demodulator is already open.
        /// </exception>
        /// <exception cref="DemodulatorException">
        /// Thrown if the number of bits per WAV sample is unsupported.
        /// </exception>
        public override void Open()
        {
            if (IsOpen)
                throw new InvalidOperationException("The demodulator is already open");
            IsOpen = true;

            wavStream = File.OpenRead(wavFile);
            wavReader = new BinaryReader(wavStream);

            ReadWavHeader();

            if (bitsPerWavSample != 8 && bitsPerWavSample != 16)
                throw new DemodulatorException("The number of bits per WAV sample is unsuported");
        }

        public override void Close()
        {
            wavReader?.Dispose();
            wavStream?.Dispose();
            IsOpen = false;
        }

        /// <summary>
        /// Populates relevant members with information from the WAV file header and advances to the start of the data
        /// section of the file.
        /// </summary>
        /// <exception cref="EndOfStreamException">
        /// Thrown if the end of the stream is reached.
        /// </exception>
        /// <exception cref="DemodulatorException">
        /// Thrown if the WAV header does not conform to the required format or contains invalid values.
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
            if (numberOfChannels <= 0)
                throw new DemodulatorException("Invalid number of WAV channels");

            // Read sample rate
            if (wavReader.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            int sampleRate = BitConverter.ToInt32(buffer);
            if (sampleRate <= 0)
                throw new DemodulatorException("Invalid WAV sample rate");

            samplesPerDemodBit = (double)sampleRate / Constants.BAUD_RATE;

            // Skip along
            if (wavReader.ReadBytes(6).Length < 6)
                throw new EndOfStreamException();

            // Read bits per sample
            if (wavReader.Read(buffer, 0, 2) < 2)
                throw new EndOfStreamException();

            bitsPerWavSample = buffer[0] + (buffer[1] << 8);
            if (bitsPerWavSample <= 0)
                throw new DemodulatorException("Invalid number of bits per WAV sample");

            // Check for data subchunk
            if (wavReader.Read(buffer, 0, 4) < 4)
                throw new EndOfStreamException();

            if (Encoding.UTF8.GetString(buffer) != "data")
                throw new DemodulatorException("WAV file does not contain data subchunk");

            // Skip along to start of data
            if (wavReader.ReadBytes(4).Length < 4)
                throw new EndOfStreamException();
        }

        protected override byte ReadWavByte()
        {
            return wavReader!.ReadByte();
        }
    }
}
