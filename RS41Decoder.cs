using System.IO;

namespace RSDecoder.RS41
{
    public class RS41Decoder
    {
        private string filePath;

        private Demodulator demodulator;
        private Decoder decoder;

        public RS41Decoder(string filePath)
        {
            this.filePath = filePath;
        }

        public bool StartDecoding()
        {
            demodulator = new Demodulator();
            decoder = new Decoder();

            using (FileStream fileStream = File.OpenRead(filePath))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    demodulator.ReadWavHeader(reader);

                    if (demodulator.BitsPerSample != 8 && demodulator.BitsPerSample != 16)
                        return false;

                    try
                    {
                        decoder.DecodingLoop(reader, demodulator);
                    }
                    catch (EndOfStreamException)
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        public void StopDecoding()
        {

        }
    }
}
