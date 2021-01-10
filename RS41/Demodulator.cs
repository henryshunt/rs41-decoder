using System;
using System.IO;
using System.Text;

namespace RSDecoder.RS41
{
    public class Demodulator
    {
        public int NumberOfChannels { get; private set; }
        public int SampleRate { get; private set; }
        public int BitsPerSample { get; private set; }
        public double SamplesPerBit { get; private set; }

        private string filePath;
        private FileStream fileStream;
        private BinaryReader reader;

        private bool hasReadWavHeader = false;

        private int currentSampleSign = 1;
        private int previousSampleSign = 1;

        public Demodulator(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            this.filePath = filePath;
        }


        private void OpenStream()
        {
            fileStream = File.OpenRead(filePath);
            reader = new BinaryReader(fileStream);
        }

        private bool ReadWavHeader()
        {
            byte[] buffer = new byte[4];

            // Read RIFF header
            if (reader.Read(buffer, 0, 4) < 4)
                return false;

            if (Encoding.UTF8.GetString(buffer) != "RIFF")
                return false;

            // Skip along
            if (reader.Read(buffer, 0, 4) < 4)
                return false;

            // Read WAVE header
            if (reader.Read(buffer, 0, 4) < 4)
                return false;

            if (Encoding.UTF8.GetString(buffer) != "WAVE")
                return false;

            // Read until "fmt " is found
            string buffer2 = "";

            while (true)
            {
                try
                {
                    buffer2 += reader.ReadChar();
                }
                catch (EndOfStreamException)
                {
                    return false;
                }

                if (buffer2.Contains("fmt "))
                    break;
            }

            // Skip along
            if (reader.Read(buffer, 0, 4) < 4)
                return false;
            if (reader.Read(buffer, 0, 2) < 2)
                return false;

            // Read channel count
            if (reader.Read(buffer, 0, 2) < 2)
                return false;

            NumberOfChannels = buffer[0] + (buffer[1] << 8);

            // Read sample rate
            if (reader.Read(buffer, 0, 4) < 4)
                return false;

            SampleRate = BitConverter.ToInt32(buffer);
            SamplesPerBit = SampleRate / (double)Constants.BAUD_RATE;

            // Skip along
            if (reader.Read(buffer, 0, 4) < 4)
                return false;
            if (reader.Read(buffer, 0, 2) < 2)
                return false;

            // Read bits per sample
            if (reader.Read(buffer, 0, 2) < 2)
                return false;

            BitsPerSample = buffer[0] + (buffer[1] << 8);

            // Read until "data" is found
            while (true)
            {
                try
                {
                    buffer2 += reader.ReadChar();
                }
                catch (EndOfStreamException)
                {
                    return false;
                }

                if (buffer2.Contains("data"))
                    break;
            }

            // Skip along
            if (reader.Read(buffer, 0, 4) < 4)
                return false;

            return true;
        }

        public short ReadWavSample()
        {
            if (!hasReadWavHeader)
            {
                hasReadWavHeader = true;

                OpenStream();
                ReadWavHeader();

                if (BitsPerSample != 8 && BitsPerSample != 16)
                    return 0;
            }

            short sample = 0;

            for (int channel = 0; channel < 2; channel++)
            {
                byte buffer = reader.ReadByte();

                if (channel == Constants.WAV_CHANNEL)
                    sample = buffer;

                if (BitsPerSample == 16)
                {
                    buffer = reader.ReadByte();

                    if (channel == Constants.WAV_CHANNEL)
                        sample += (short)(buffer << 8);
                }
            }

            if (BitsPerSample == 8)
                return (short)(sample - 128);
            else return sample;
        }

        public bool[] ReadBits()
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
            double bitCount = sampleCount / SamplesPerBit;
            int bitCount2 = (int)(bitCount + 0.5);

            bool[] bits = new bool[bitCount2];

            for (int i = 0; i < bitCount2; i++)
                bits[i] = previousSampleSign == -1 ? false : true;

            return bits;
        }
    }
}
