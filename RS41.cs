using System.IO;

namespace RS41Decoder
{
    public class RS41
    {
        private string filePath;

        private Demodulator demodulator;
        private Decoder decoder;

        public RS41(string filePath)
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

                    decoder.DecodeLoop(reader, demodulator);
                }
            }

            return true;
        }

        public void StopDecoding()
        {

        }

        public static void PrintFrameTable(byte[] frame)
        {

        }
    }
}
