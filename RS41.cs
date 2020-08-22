using System;
using System.IO;

namespace RS41Decoder
{
    public class RS41
    {
        private string input;

        public RS41(string input)
        {
            this.input = input;
        }

        public void StartDecoding()
        {
            Demodulation d = new Demodulation();
            Decoder dc = new Decoder();

            using (FileStream fileStream = File.OpenRead(input))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                    dc.DecodeLoop(reader, d);
            }
        }
    }
}
