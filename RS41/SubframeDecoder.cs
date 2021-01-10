using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RSDecoder.RS41
{
    internal class SubframeDecoder
    {
        List<byte[]> subframeParts = new List<byte[]>();
        private int lastSubframeNumber = -1;

        private readonly byte[] subframeBytes = new byte[Constants.SUBFRAME_LENGTH];

        public Subframe decodedSubframe { get; private set; } = null;


        public bool AddSubframePart(int subframeNumber, byte[] subframeBytes)
        {
            if (subframeNumber == lastSubframeNumber + 1)
            {
                subframeParts.Add(subframeBytes);
                if (subframeNumber == Constants.SUBFRAME_LAST_NUMBER)
                {
                    DecodeSubframe();

                    lastSubframeNumber = -1;
                    subframeParts.Clear();
                    return true;
                }
                else lastSubframeNumber++;
            }
            else
            {
                lastSubframeNumber = -1;
                subframeParts.Clear();
            }

            return false;
        }

        private void DecodeSubframe()
        {
            int i = 0;
            foreach (byte[] ba in subframeParts)
            {
                foreach (byte b in ba)
                    subframeBytes[i++] = b;
            }

            decodedSubframe = new Subframe();

            //foreach (byte b in bytes)
            //    Console.Write("{0:X2} ", b);
            //Console.WriteLine();
            //Console.WriteLine();

            if (subframeBytes[Constants.POS_SUB_BK_STATUS] == 0x0)
                decodedSubframe.IsBurstKillEnabled = false;
            else if (subframeBytes[Constants.POS_SUB_BK_STATUS] == 0x1)
                decodedSubframe.IsBurstKillEnabled = true;

            DecodeDeviceType();
            DecodeFrequency();
        }

        private void DecodeDeviceType()
        {
            char[] bytes = new char[8];

            for (int i = 0; i < 8; i++)
                bytes[i] = (char)subframeBytes[Constants.POS_SUB_TYPE + i];

            decodedSubframe.DeviceType = new string(bytes);
        }

        private void DecodeFrequency()
        {
            byte b = (byte)(subframeBytes[Constants.POS_SUB_FREQUENCY_LOWER] & 0xC0);
            double f0 = (b * 10) / 64;

            b = subframeBytes[Constants.POS_SUB_FREQUENCY_UPPER];
            double f1 = 40 * b;

            decodedSubframe.Frequency = (400000 + f1 + f0) / 1000;
        }

        public void Print()
        {
            Console.WriteLine("\n\n");

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(decodedSubframe))
                Console.WriteLine("{0} = {1}", descriptor.Name, descriptor.GetValue(decodedSubframe));
        }
    }
}
