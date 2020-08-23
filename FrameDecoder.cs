using RS41Decoder.RS41;
using System;
using System.ComponentModel;

namespace RSDecoder.RS41
{
    public class FrameDecoder
    {
        private bool[] frameBits = new bool[Constants.FRAME_LENGTH * 8];
        private byte[] frameBytes = new byte[Constants.FRAME_LENGTH];

        private Frame decodedFrame = new Frame();

        public FrameDecoder(bool[] frameBits)
        {
            if (frameBits == null)
                throw new ArgumentNullException();

            if (frameBits.Length != Constants.FRAME_LENGTH * 8)
                throw new ArgumentException();

            this.frameBits = frameBits;
        }


        public Frame Decode()
        {
            DecodeBytes();
            DecodeXor();

            DecodeFrameType();
            DecodeFrameNumber();
            DecodeSerialNumber();
            DecodeBatteryVoltage();
            DecodeSubframeNumber();

            DecodeFrameTime(DecodeGpsWeek(), DecodeGpsSecondsIntoWeek());

            if (!decodedFrame.IsExtendedFrame)
            {
                for (int i = 0; i < Constants.EXTENDED_DATA_LENGTH; i++)
                    frameBytes[Constants.STANDARD_FRAME_LENGTH + i] = 0;
            }

            return decodedFrame;
        }

        private void DecodeBytes()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
            {
                bool[] byteBits = new bool[8];

                for (int j = 0; j < 8; j++)
                    byteBits[j] = frameBits[(i * 8) + j];

                frameBytes[i] = BoolArrayToByte(byteBits);
            }
        }

        private void DecodeXor()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
                frameBytes[i] = (byte)(frameBytes[i] ^ Constants.XOR_MASK[i % Constants.XOR_MASK_LENGTH]);
        }

        private byte BoolArrayToByte(bool[] bits)
        {
            byte d = 1;
            byte value = 0;

            for (int i = 0; i < 8; i++) // little endian
            {
                if (bits[i] == true)
                    value += d;
                else if (bits[i] == false)
                    value += 0;

                d <<= 1;
            }

            return value;
        }

        private void DecodeFrameType()
        {
            if (Constants.POS_FRAME_TYPE == 0xF0)
                decodedFrame.IsExtendedFrame = true;
            else decodedFrame.IsExtendedFrame = false;
        }

        private void DecodeFrameNumber()
        {
            byte[] bytes = new byte[2];

            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_FRAME_NUMBER + i];

            decodedFrame.FrameNumber = bytes[0] + (bytes[1] << 8);
        }

        private void DecodeSerialNumber()
        {
            char[] bytes = new char[8];

            for (int i = 0; i < 8; i++)
                bytes[i] = (char)frameBytes[Constants.POS_SERIAL_NUMBER + i];

            decodedFrame.SerialNumber = new string(bytes);
        }

        private void DecodeBatteryVoltage()
        {
            decodedFrame.BatteryVoltage = frameBytes[Constants.POS_BATTERY_VOLTAGE] / (double)10;
        }

        private void DecodeSubframeNumber()
        {
            decodedFrame.SubframeNumber = frameBytes[Constants.POS_SUBFRAME_NUMBER];
        }

        private void DecodeFrameTime(int week, int secondsIntoWeek)
        {
            DateTime gpsTimeOrigin = new DateTime(1980, 1, 6);
            gpsTimeOrigin = gpsTimeOrigin.AddDays(week * 7);
            gpsTimeOrigin = gpsTimeOrigin.AddSeconds(secondsIntoWeek);

            decodedFrame.FrameTime = gpsTimeOrigin;
        }

        private int DecodeGpsWeek()
        {
            byte[] bytes = new byte[2];

            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_GPS_WEEK + i];

            return bytes[0] + (bytes[1] << 8);
        }

        private int DecodeGpsSecondsIntoWeek()
        {
            byte[] bytes = new byte[4];

            for (int i = 0; i < 4; i++)
                bytes[i] = frameBytes[Constants.POS_GPS_TIME_OF_WEEK + i];

            return BitConverter.ToInt32(bytes) / 1000;
        }


        public override string ToString()
        {
            string value = "";

            foreach (byte b in frameBytes)
                value += b.ToString("X2");

            return value;
        }

        public void PrintFrameTable()
        {
            Console.Write("      ");

            for (int i = 0; i <= 0xF; i++)
                Console.Write("{0:X1}  ", i);

            Console.WriteLine();
            bool stop = false;

            for (int i = 0; i <= 0x20; i++)
            {
                Console.Write("0x{0:X2}0 ", i);

                for (int j = 0; j <= 0xF; j++)
                {
                    if ((i * 16) + j == 0x206)
                    {
                        stop = true;
                        break;
                    }

                    if ((i * 16) + j >= 0 && (i * 16) + j <= 0x7)
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                    else if ((i * 16) + j >= 0x8 && (i * 16) + j <= 0x37)
                        Console.BackgroundColor = ConsoleColor.DarkBlue;

                    else if ((i * 16) + j >= 0x39 && (i * 16) + j <= 0x64)
                        Console.BackgroundColor = ConsoleColor.Red;
                    else if ((i * 16) + j >= 0x65 && (i * 16) + j <= 0x92)
                        Console.BackgroundColor = ConsoleColor.Magenta;
                    else if ((i * 16) + j >= 0x93 && (i * 16) + j <= 0xB4)
                        Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    else if ((i * 16) + j >= 0xB5 && (i * 16) + j <= 0x111)
                        Console.BackgroundColor = ConsoleColor.Blue;
                    else if ((i * 16) + j >= 0x112 && (i * 16) + j <= 0x12A)
                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                    else if ((i * 16) + j >= 0x12B && (i * 16) + j <= 0x13F)
                        Console.BackgroundColor = ConsoleColor.Green;

                    Console.Write("{0:X2}", frameBytes[(i * 16) + j]);
                    Console.ResetColor();
                    Console.Write(" ");
                }

                Console.WriteLine();

                if (stop)
                    break;
            }

            Console.WriteLine();

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(decodedFrame))
                Console.WriteLine("{0} = {1}", descriptor.Name, descriptor.GetValue(decodedFrame));
        }
    }
}
