using System;
using System.IO;

namespace RS41Decoder
{
    public class Demodulation
    {
        private int par = 1;
        private int parOld = 1;

        public void ReadWavHeader(BinaryReader reader)
        {

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
