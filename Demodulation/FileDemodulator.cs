using System;
using System.IO;
using System.Text;
using System.Threading;

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
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/>, used for cancelling the demodulation.
        /// </param>
        public FileDemodulator(string wavFile, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            this.wavFile = wavFile;
        }

        /// <summary>
        /// Opens the demodulator.
        /// </summary>
        /// <exception cref="DemodulatorException">
        /// Thrown if the number of bits per WAV sample sample is unsupported.
        /// </exception>
        public override void Open()
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
        public override void Close()
        {
            wavReader?.Close();
            wavStream?.Close();
        }

        /// <summary>
        /// Populates relevant members with information from the WAV file header and advances to the start of the data
        /// section of the file.
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

            int sampleRate = BitConverter.ToInt32(buffer);
            samplesPerDemodBit = (double)sampleRate / Constants.BAUD_RATE;

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

        protected override byte ReadWavByte()
        {
            return wavReader!.ReadByte();
        }

        /// <summary>
        /// Disposes the demodulator.
        /// </summary>
        public override void Dispose()
        {
            Close();
            wavStream?.Dispose();
            wavReader?.Dispose();
        }
    }
}
