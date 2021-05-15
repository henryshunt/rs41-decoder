using System;
using static Rs41Decoder.Decoding.Utilities;

namespace Rs41Decoder.Decoding
{
    /// <summary>
    /// Represents a decoder for a single RS41 radiosonde frame.
    /// </summary>
    internal class FrameDecoder
    {
        /// <summary>
        /// The frame bits to decode. Has a length of <see cref="Constants.FRAME_LENGTH"/> * 8.
        /// </summary>
        private readonly bool[] frameBits;

        /// <summary>
        /// The frame bytes to decode (same as the contents of <see cref="frameBits"/>, but as bytes). Has a length of
        /// <see cref="Constants.FRAME_LENGTH"/>.
        /// </summary>
        private byte[] frameBytes = new byte[Constants.FRAME_LENGTH];

        /// <summary>
        /// The decoded frame.
        /// </summary>
        private readonly Rs41Frame frame = new Rs41Frame();

        private readonly SubframeDecoder subframeDecoder;
        private FrameErrorCorrection? errorCorr = null;

        /// <summary>
        /// Initialises a new instance of the <see cref="FrameDecoder"/> class.
        /// </summary>
        /// <param name="frameBits">
        /// The frame bits to decode. Must have a length of <see cref="Constants.FRAME_LENGTH"/> * 8.
        /// </param>
        /// <param name="subframeDecoder">
        /// A subframe decoder.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="frameBits"/> does not have the required length.
        /// </exception>
        public FrameDecoder(bool[] frameBits, SubframeDecoder subframeDecoder)
        {
            if (frameBits.Length != Constants.FRAME_LENGTH * 8)
            {
                throw new ArgumentException(nameof(frameBits) +
                    " does not have the correct length", nameof(frameBits));
            }

            this.frameBits = frameBits;
            this.subframeDecoder = subframeDecoder;
        }

        /// <summary>
        /// Decodes the frame bits into a frame.
        /// </summary>
        public Rs41Frame Decode()
        {
            FrameBitsToBytes();
            UnscrambleFrameBytes();

            errorCorr = new FrameErrorCorrection(frameBytes);
            frameBytes = errorCorr.Correct();

            frame.IsExtendedFrame =
                frameBytes[Constants.POS_FRAME_TYPE] == Constants.EXTENDED_FRAME_VALUE;

            DecodeFrameNumber();
            DecodeSerialNumber();
            DecodeBatteryVoltage();

            if (errorCorr.IsStatusBlockValid)
            {
                int subframeNumber = frameBytes[Constants.POS_SUBFRAME_NUMBER];

                byte[] subframeBytes = new byte[Constants.SUBFRAME_PART_LENGTH];
                for (int i = 0; i < Constants.SUBFRAME_PART_LENGTH; i++)
                    subframeBytes[i] = frameBytes[Constants.POS_SUBFRAME_BYTES + i];

                if (subframeDecoder.AddSubframePart(subframeNumber, subframeBytes))
                    frame.Subframe = subframeDecoder.Subframe;
            }

            DecodeTime();
            DecodePositionAndVelocity();
            DecodeGpsSatelliteCount();
            DecodeGpsVelocityAccuracy();
            DecodeGpsPositionPrecision();
            DecodeTemperature();
            DecodeHumidityTemperature();
            DecodeHumidity();

            return frame;
        }

        /// <summary>
        /// Converts the contents of <see cref="frameBits"/> to bytes and stores them in <see cref="frameBytes"/>.
        /// </summary>
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

        /// <summary>
        /// Reverses the XOR scrambling applied to the frame data before it was transmitted.
        /// </summary>
        private void UnscrambleFrameBytes()
        {
            for (int i = 0; i < Constants.FRAME_LENGTH; i++)
            {
                frameBytes[i] = (byte)(frameBytes[i] ^
                    Constants.FRAME_MASK[i % Constants.FRAME_MASK.Length]);
            }
        }

        #region Frame Part Decoding
        private void DecodeFrameNumber()
        {
            if (!errorCorr!.IsStatusBlockValid)
                return;

            byte[] bytes = new byte[2];
            for (int i = 0; i < 2; i++)
                bytes[i] = frameBytes[Constants.POS_FRAME_NUMBER + i];

            frame.Number = BitConverter.ToInt16(bytes);
        }

        private void DecodeSerialNumber()
        {
            if (!errorCorr!.IsStatusBlockValid)
                return;

            char[] bytes = new char[8];
            for (int i = 0; i < 8; i++)
                bytes[i] = (char)frameBytes[Constants.POS_SERIAL_NUMBER + i];

            frame.SerialNumber = new string(bytes);
        }

        private void DecodeBatteryVoltage()
        {
            if (!errorCorr!.IsStatusBlockValid)
                return;

            frame.BatteryVoltage = frameBytes[Constants.POS_BATTERY_VOLTAGE] / (double)10;
        }

        private void DecodeTime()
        {
            if (!errorCorr!.IsStatusBlockValid || !errorCorr!.IsGpsInfoBlockValid)
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
            frame.Time = time;
        }

        private void DecodePositionAndVelocity()
        {
            if (!errorCorr!.IsGpsPositionBlockValid)
                return;

            (double, double, double) ecefPositions = DecodeEcefPositions();
            (double, double, double) positions = EcefToLlh(
                ecefPositions.Item1, ecefPositions.Item2, ecefPositions.Item3);

            frame.Latitude = positions.Item1;
            frame.Longitude = positions.Item2;
            frame.Elevation = positions.Item3;

            (double, double, double) ecefVelocities = DecodeEcefVelocities();
            (double, double, double) velocities = EcefToHdv(
                (double)frame.Latitude, (double)frame.Longitude,
                ecefVelocities.Item1, ecefVelocities.Item2, ecefVelocities.Item3);

            frame.HorizontalVelocity = velocities.Item1;
            frame.Direction = velocities.Item2;
            frame.VerticalVelocity = velocities.Item3;
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
            if (!errorCorr!.IsGpsPositionBlockValid)
                return;

            frame.GpsSatelliteCount = frameBytes[Constants.POS_GPS_SATELLITE_COUNT];
        }

        private void DecodeGpsVelocityAccuracy()
        {
            if (!errorCorr!.IsGpsPositionBlockValid)
                return;

            frame.VelocityAccuracy = frameBytes[Constants.POS_VELOCITY_ACCURACY] / (double)10;
        }

        private void DecodeGpsPositionPrecision()
        {
            if (!errorCorr!.IsGpsPositionBlockValid)
                return;

            frame.PositionAccuracy = frameBytes[Constants.POS_POSITION_ACCURACY] / (double)10;
        }

        private void DecodeTemperature()
        {
            if (!errorCorr!.IsMeasurementBlockValid || subframeDecoder.Subframe == null)
                return;

            Rs41Subframe subframe = subframeDecoder.Subframe;
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

            frame.Temperature = T;
        }

        private void DecodeHumidityTemperature()
        {
            if (!errorCorr!.IsMeasurementBlockValid || subframeDecoder.Subframe == null)
                return;

            Rs41Subframe subframe = subframeDecoder.Subframe;
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

            frame.HumidityModuleTemp = T;
        }

        private void DecodeHumidity()
        {
            if (!errorCorr!.IsMeasurementBlockValid || subframeDecoder.Subframe == null ||
                frame.Temperature == null)
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

            rh += T0 - (double)frame.Temperature / 5.5;                    // empir. temperature compensation

            if (frame.Temperature < T1)
                rh *= 1.0 + (T1 - (double)frame.Temperature) / 90.0; // empir. temperature compensation

            if (rh < 0.0)
                rh = 0.0;

            if (rh > 100.0)
                rh = 100.0;

            if (frame.Temperature < -273.0)
                rh = -1.0;

            frame.Humidity = rh;
        }
        #endregion
    }
}
