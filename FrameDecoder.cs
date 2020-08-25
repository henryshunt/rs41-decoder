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

            DecodePosition();
            DecodeGpsSatelliteCount();
            DecodeGpsVelocityAccuracy();
            DecodeGpsPositionAccuracy();

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
                frameBytes[i] = (byte)(frameBytes[i] ^ Constants.FRAME_MASK[i % Constants.FRAME_MASK.Length]);
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

            decodedFrame.FrameNumber = BitConverter.ToInt16(bytes);
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

            return BitConverter.ToInt16(bytes);
        }

        private int DecodeGpsSecondsIntoWeek()
        {
            byte[] bytes = new byte[4];

            for (int i = 0; i < 4; i++)
                bytes[i] = frameBytes[Constants.POS_GPS_TIME_OF_WEEK + i];

            return BitConverter.ToInt32(bytes) / 1000;
        }

        private void DecodePosition()
        {
            (double, double, double) ecefPositions = DecodeEcefPositions();
            (double, double, double) positions = EcefToLlh(ecefPositions.Item1, ecefPositions.Item2, ecefPositions.Item3);

            decodedFrame.Latitude = positions.Item1;
            decodedFrame.Longitude = positions.Item2;
            decodedFrame.Elevation = positions.Item3;

            (double, double, double) ecefVelocities = DecodeEcefVelocities();
            (double, double, double) velocities = EcefToHdv(decodedFrame.Latitude,
                decodedFrame.Longitude, ecefVelocities.Item1, ecefVelocities.Item2, ecefVelocities.Item3);

            decodedFrame.HorizontalVelocity = velocities.Item1;
            decodedFrame.Direction = velocities.Item2;
            decodedFrame.VerticalVelocity = velocities.Item3;
        }

        private (double, double, double) DecodeEcefPositions()
        {
            byte[] bytes = new byte[4];

            for (int i = 0; i < 4; i++)
                bytes[i] = frameBytes[Constants.POS_ECEF_POSITION_X + i];

            int x = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = frameBytes[Constants.POS_ECEF_POSITION_Y + i];

            int y = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = frameBytes[Constants.POS_ECEF_POSITION_Z + i];

            int z = BitConverter.ToInt32(bytes);

            return (x / (double)100, y / (double)100, z / (double)100);
        }

        private (double, double, double) DecodeEcefVelocities()
        {
            byte[] bytes = new byte[2];

            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_ECEF_VELOCITY_X + i];

            int x = BitConverter.ToInt16(bytes);

            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_ECEF_VELOCITY_Y + i];

            int y = BitConverter.ToInt16(bytes);

            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_ECEF_VELOCITY_Z + i];

            int z = BitConverter.ToInt16(bytes);

            return (x / (double)100, y / (double)100, z / (double)100);
        }


        private const double EARTH_A = 6378137;
        private const double EARTH_B = 6356752.31424518;
        private const double EARTH_A2_B2 = (EARTH_A * EARTH_A - EARTH_B * EARTH_B);

        private const double e2 = EARTH_A2_B2 / (EARTH_A * EARTH_A);
        private const double ee2 = EARTH_A2_B2 / (EARTH_B * EARTH_B);

        private (double, double, double) EcefToLlh(double x, double y, double z)
        {
            double lam = Math.Atan2(y, x);
            double p = Math.Sqrt(x * x + y * y);
            double t = Math.Atan2(z * EARTH_A, p * EARTH_B);

            double phi = Math.Atan2(z + ee2 * EARTH_B * Math.Sin(t) * Math.Sin(t) * Math.Sin(t),
                p - e2 * EARTH_A * Math.Cos(t) * Math.Cos(t) * Math.Cos(t));

            double R = EARTH_A / Math.Sqrt(1 - e2 * Math.Sin(phi) * Math.Sin(phi));

            double latitude = phi * 180 / Math.PI;
            double longitude = lam * 180 / Math.PI;
            double elevation = p / Math.Cos(phi) - R;

            return (latitude, longitude, elevation);
        }

        private (double, double, double) EcefToHdv(double latitude, double longitude, double x, double y, double z)
        {
            // First convert from ECEF to NEU (north, east, up)
            double phi = latitude * Math.PI / 180.0;
            double lam = longitude * Math.PI / 180.0;

            double vN = -x * Math.Sin(phi) * Math.Cos(lam) - y * Math.Sin(phi) * Math.Sin(lam) + z * Math.Cos(phi);
            double vE = -x * Math.Sin(lam) + y * Math.Cos(lam);
            double vU = x * Math.Cos(phi) * Math.Cos(lam) + y * Math.Cos(phi) * Math.Sin(lam) + z * Math.Sin(phi);

            // Then convert from NEU to HDV (horizontal, direction, vertical)
            double vH = Math.Sqrt(vN * vN + vE * vE);
            double direction = Math.Atan2(vE, vN) * 180 / Math.PI;

            if (direction < 0)
                direction += 360;

            return (vH, direction, vU);
        }


        private void DecodeGpsSatelliteCount()
        {
            decodedFrame.GpsSatelliteCount = frameBytes[Constants.POS_GPS_SATELLITE_COUNT];
        }

        private void DecodeGpsVelocityAccuracy()
        {
            decodedFrame.VelocityAccuracy = frameBytes[Constants.POS_VELOCITY_ACCURACY] / (double)10;
        }

        private void DecodeGpsPositionAccuracy()
        {
            decodedFrame.PositionAccuracy = frameBytes[Constants.POS_POSITION_ACCURACY] / (double)10;
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
