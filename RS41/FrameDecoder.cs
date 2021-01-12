using System;

namespace RSDecoder.RS41
{
    internal class FrameDecoder
    {
        private readonly bool[] frameBits;
        private readonly byte[] frameBytes = new byte[Constants.FRAME_LENGTH];

        public readonly Frame Frame = new Frame();

        private readonly SubframeDecoder subframeDecoder;
        private FrameErrorCorrection? ec = null;

        public FrameDecoder(bool[] frameBits, SubframeDecoder subframeDecoder)
        {
            if (frameBits.Length != Constants.FRAME_LENGTH * 8)
            {
                throw new ArgumentException(string.Format(
                    "Argument {0} must contain {1} items", nameof(frameBits), Constants.FRAME_LENGTH * 8),
                    nameof(frameBits));
            }

            this.frameBits = frameBits;
            this.subframeDecoder = subframeDecoder;
        }


        public Frame Decode()
        {
            FrameBitsToBytes();
            UnmaskFrameBytes();

            ec = new FrameErrorCorrection(frameBytes, Frame);
            ec.Correct();

            Frame.IsExtendedFrame = frameBytes[Constants.POS_FRAME_TYPE] == 0xF0;

            DecodeFrameNumber();
            DecodeSerialNumber();
            DecodeBatteryVoltage();

            if (ec.IsStatusBlockValid)
            {
                int subframeNumber = frameBytes[Constants.POS_SUBFRAME_NUMBER];

                byte[] subframeBytes = new byte[16];
                for (int i = 0; i < 16; i++)
                    subframeBytes[i] = frameBytes[Constants.POS_SUBFRAME_BYTES + i];

                if (subframeDecoder.AddSubframePart(subframeNumber, subframeBytes))
                    Frame.Subframe = subframeDecoder.Subframe;
            }

            DecodeTime();
            DecodePosAndVel();
            DecodeGpsSatelliteCount();
            DecodeGpsVelocityAccuracy();
            DecodeGpsPositionAccuracy();
            DecodeThermoTemp();
            DecodeThermoHumi();
            DecodeHumidity();

            //if (!Frame.IsExtendedFrame)
            //{
            //    for (int i = Constants.STANDARD_FRAME_LENGTH - 1; i < Constants.FRAME_LENGTH; i++)
            //        frameBytes[i] = 0;
            //}

            Console.WriteLine(Frame);
            return Frame;
        }

        private void FrameBitsToBytes()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
            {
                bool[] byteBits = new bool[8];

                for (int j = 0; j < 8; j++)
                    byteBits[j] = frameBits[(i * 8) + j];

                frameBytes[i] = BoolArrayToByte(byteBits);
            }
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

        private void UnmaskFrameBytes()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
                frameBytes[i] = (byte)(frameBytes[i] ^ Constants.FRAME_MASK[i % Constants.FRAME_MASK.Length]);
        }


        private void DecodeFrameNumber()
        {
            if (!ec!.IsStatusBlockValid)
                return;

            byte[] bytes = new byte[2];

            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_FRAME_NUMBER + i];

            Frame.Number = BitConverter.ToInt16(bytes);
        }

        private void DecodeSerialNumber()
        {
            if (!ec!.IsStatusBlockValid)
                return;

            char[] bytes = new char[8];

            for (int i = 0; i < 8; i++)
                bytes[i] = (char)frameBytes[Constants.POS_SERIAL_NUMBER + i];

            Frame.SerialNumber = new string(bytes);
        }

        private void DecodeBatteryVoltage()
        {
            if (!ec!.IsStatusBlockValid)
                return;

            Frame.BatteryVoltage = frameBytes[Constants.POS_BATTERY_VOLTAGE] / (double)10;
        }

        private void DecodeTime()
        {
            if (!ec!.IsStatusBlockValid || !ec!.IsGpsInfoBlockValid)
                return;

            byte[] bytes = new byte[2];
            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_GPS_WEEK + i];
            int week = BitConverter.ToInt16(bytes);

            bytes = new byte[4];
            for (int i = 0; i < 4; i++)
                bytes[i] = frameBytes[Constants.POS_GPS_TIME_OF_WEEK + i];
            int secondsOfWeek = BitConverter.ToInt32(bytes) / 1000;

            DateTime time = new DateTime(1980, 1, 6);
            time = time.AddDays(week * 7);
            time = time.AddSeconds(secondsOfWeek);
            Frame.Time = time;
        }

        private void DecodePosAndVel()
        {
            if (!ec!.IsGpsPositionBlockValid)
                return;

            (double, double, double) ecefPositions = DecodeEcefPositions();
            (double, double, double) positions = Utilities.EcefToLlh(
                ecefPositions.Item1, ecefPositions.Item2, ecefPositions.Item3);

            Frame.Latitude = positions.Item1;
            Frame.Longitude = positions.Item2;
            Frame.Elevation = positions.Item3;

            (double, double, double) ecefVelocities = DecodeEcefVelocities();
            (double, double, double) velocities = Utilities.EcefToHdv(
                (double)Frame.Latitude, (double)Frame.Longitude,
                ecefVelocities.Item1, ecefVelocities.Item2, ecefVelocities.Item3);

            Frame.HorizontalVelocity = velocities.Item1;
            Frame.Direction = velocities.Item2;
            Frame.VerticalVelocity = velocities.Item3;
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

        private void DecodeGpsSatelliteCount()
        {
            if (!ec!.IsGpsPositionBlockValid)
                return;

            Frame.GpsSatelliteCount = frameBytes[Constants.POS_GPS_SATELLITE_COUNT];
        }

        private void DecodeGpsVelocityAccuracy()
        {
            if (!ec!.IsGpsPositionBlockValid)
                return;

            Frame.VelocityAccuracy = frameBytes[Constants.POS_VELOCITY_ACCURACY] / (double)10;
        }

        private void DecodeGpsPositionAccuracy()
        {
            if (!ec!.IsGpsPositionBlockValid)
                return;

            Frame.PositionAccuracy = frameBytes[Constants.POS_POSITION_ACCURACY] / (double)10;
        }

        private void DecodeThermoTemp()
        {
            if (!ec!.IsMeasurementBlockValid || subframeDecoder.Subframe == null)
                return;

            Subframe subframe = subframeDecoder.Subframe;
            byte[] bytes = new byte[4];

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_THERMO_TEMP_MAIN + i];
            double main = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_THERMO_TEMP_REF1 + i];
            double ref1 = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_THERMO_TEMP_REF2 + i];
            double ref2 = BitConverter.ToInt32(bytes);


            double g = (ref2 - ref1) / (subframe.ReferenceResistor2 - subframe.ReferenceResistor1),       // gain
                Rb = (ref1 * subframe.ReferenceResistor2 - ref2 * subframe.ReferenceResistor1) / (ref2 - ref1), // ofs
                Rc = main / g - Rb,
                R = Rc * subframe.ThermoTempCalibration1;

            double T = (subframe.ThermoTempConstant1 + subframe.ThermoTempConstant2
                * R + subframe.ThermoTempConstant3 * R * R + subframe.ThermoTempCalibration2)
                * (1.0 + subframe.ThermoTempCalibration3);

            Frame.Temperature = T;
        }

        private void DecodeThermoHumi()
        {
            if (!ec!.IsMeasurementBlockValid || subframeDecoder.Subframe == null)
                return;

            Subframe subframe = subframeDecoder.Subframe;
            byte[] bytes = new byte[4];

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_THERMO_HUMI_MAIN + i];
            double main = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_THERMO_HUMI_REF1 + i];
            double ref1 = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_THERMO_HUMI_REF2 + i];
            double ref2 = BitConverter.ToInt32(bytes);


            double g = (ref2 - ref1) / (subframe.ReferenceResistor2 - subframe.ReferenceResistor1),       // gain
                Rb = (ref1 * subframe.ReferenceResistor2 - ref2 * subframe.ReferenceResistor1) / (ref2 - ref1), // ofs
                Rc = main / g - Rb,
                R = Rc * subframe.ThermoHumiCalibration1;

            double T = (subframe.ThermoHumiConstant1 + subframe.ThermoHumiConstant2
                * R + subframe.ThermoHumiConstant3 * R * R + subframe.ThermoHumiCalibration2)
                * (1.0 + subframe.ThermoHumiCalibration3);

            Frame.HumidityModuleTemp = T;
        }

        private void DecodeHumidity()
        {
            if (!ec!.IsMeasurementBlockValid || subframeDecoder.Subframe == null ||
                Frame.Temperature == null)
            {
                return;
            }

            byte[] bytes = new byte[4];

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_HUMIDITY_MAIN + i];
            double main = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_HUMIDITY_REF1 + i];
            double ref1 = BitConverter.ToInt32(bytes);

            for (int i = 0; i < 3; i++)
                bytes[i] = frameBytes[Constants.POS_HUMIDITY_REF2 + i];
            double ref2 = BitConverter.ToInt32(bytes);


            double a0 = 7.5;           // empirical
            double a1 = 350.0 / subframeDecoder.Subframe.HumidityCalibration; // empirical
            double fh = (main - ref1) / (float)(ref2 - ref1);
            double rh = 100.0 * (a1 * fh - a0);
            double T0 = 0.0, T1 = -25.0; // T/C

            rh += T0 - (double)Frame.Temperature / 5.5;                    // empir. temperature compensation

            if (Frame.Temperature < T1)
                rh *= 1.0 + (T1 - (double)Frame.Temperature) / 90.0; // empir. temperature compensation

            if (rh < 0.0)
                rh = 0.0;

            if (rh > 100.0)
                rh = 100.0;

            if (Frame.Temperature < -273.0)
                rh = -1.0;

            Frame.Humidity = rh;
        }
    }
}
