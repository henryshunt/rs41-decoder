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
                {
                    for (int i = 0; i < 10; i++)
                        reader.ReadByte();

                    //for (int i = 0; i < 25; i++)
                    //{
                    //    var x = d.ReadBits(reader);
                    //    Console.WriteLine("value: {0}, count: {1}", x.Item1, x.Item2);
                    //    //Console.WriteLine(d.ReadWavSample(reader));
                    //}

                    dc.DecodeLoop(reader, d);
                }
            }
        }
    }
}
