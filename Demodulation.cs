using System;
using System.IO;
using System.Text;

namespace RS41Decoder
{
    public class Demodulation
    {
        public int NumberOfChannels { get; set; }
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public double SamplesPerBit { get; set; }

        private static int BAUD_RATE = 4800;

        private bool hasReadWavHeader = false;

        private int par = 1;
        private int parOld = 1;


        public bool ReadWavHeader(BinaryReader reader)
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
            SamplesPerBit = SampleRate / (float)BAUD_RATE;

            // Skip along
            if (reader.Read(buffer, 0, 4) < 4)
                return false;
            if (reader.Read(buffer, 0, 2) < 2)
                return false;

            // Read bits per sample
            if (reader.Read(buffer, 0, 2) < 2)
                return false;

            BitsPerSample = buffer[0] + (buffer[1] << 8);

            if (BitsPerSample != 8 && BitsPerSample != 16)
                return false;

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

        public short ReadWavSample(BinaryReader reader)
        {
            int sample = 0;

            for (int channel = 0; channel < 2; channel++)
            {
                byte b = reader.ReadByte();

                if (channel == 0)
                    sample = b;

                b = reader.ReadByte();

                if (channel == 0)
                    sample += b << 8;
            }

            return (short)sample;
        }

        public Tuple<int, int> ReadBits(BinaryReader reader)
        {
            int sampleCount = 0;

            do
            {
                int sample = ReadWavSample(reader);

                parOld = par;
                par = (sample >= 0) ? 1 : -1;
                sampleCount++;
            }
            while (par * parOld > 0);

            double bitCount = sampleCount / 7.81;

            Tuple<int, int> ret = new Tuple<int, int>((1 + parOld) / 2, (int)(bitCount + 0.5));
            return ret;
        }
    }
}
