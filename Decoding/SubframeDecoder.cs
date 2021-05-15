using System;

namespace Rs41Decoder.Decoding
{
    /// <summary>
    /// Represents a decoder for the subframe of an RS41 radiosonde.
    /// </summary>
    internal class SubframeDecoder
    {
        /// <summary>
        /// The subframe bytes to decode. Has a length of <see cref="Constants.SUBFRAME_LENGTH"/>.
        /// </summary>
        private readonly byte[] subframeBytes = new byte[Constants.SUBFRAME_LENGTH];

        /// <summary>
        /// The index within <see cref="subframeBytes"/> at which to place the next item.
        /// </summary>
        private int subframeBytesPos = 0;

        /// <summary>
        /// The number of the last subframe part added.
        /// </summary>
        private int prevPartNumber = -1;

        /// <summary>
        /// The decoded subframe.
        /// </summary>
        public readonly Rs41Subframe Subframe = new Rs41Subframe();

        /// <summary>
        /// Initialises a new instance of the <see cref="SubframeDecoder"/> class.
        /// </summary>
        public SubframeDecoder() { }

        /// <summary>
        /// Caches a subframe part and, if all the other parts have been added, decodes the subframe. Once the subframe
        /// is decoded, everything is reset and the parts of a new subframe can be added.
        /// </summary>
        /// <param name="partNumber">
        /// The number of the subframe part to add. Must be between 0 and <see cref="Constants.SUBFRAME_NUMBER_FINAL"/>.
        /// </param>
        /// <param name="partBytes">
        /// The bytes of the subframe part. Must have a length of <see cref="Constants.SUBFRAME_PART_LENGTH"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the subframe was decoded, otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="partNumber"/> is out of range.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="partBytes"/> does not have the correct length.
        /// </exception>
        public bool AddSubframePart(int partNumber, byte[] partBytes)
        {
            if (partNumber < 0 || partNumber > Constants.SUBFRAME_NUMBER_FINAL)
            {
                throw new ArgumentOutOfRangeException(nameof(partNumber),
                    nameof(partNumber) + " is out of range ");
            }

            if (partBytes.Length != Constants.SUBFRAME_PART_LENGTH)
            {
                throw new ArgumentException(nameof(partBytes) +
                    " does not have the correct length ", nameof(partBytes));
            }

            if (partNumber - 1 == prevPartNumber)
            {
                partBytes.CopyTo(subframeBytes, subframeBytesPos);

                if (partNumber == Constants.SUBFRAME_NUMBER_FINAL)
                {
                    Decode();

                    subframeBytesPos = 0;
                    prevPartNumber = -1;
                    return true;
                }
                else
                {
                    subframeBytesPos += partBytes.Length;
                    prevPartNumber++;
                }
            }
            else
            {
                subframeBytesPos = 0;
                prevPartNumber = -1;
            }

            return false;
        }

        /// <summary>
        /// Decodes the subframe bytes into a subframe.
        /// </summary>
        private void Decode()
        {
            DecodeBurstKill();
            DecodeDeviceType();
            DecodeFrequency();
            DecodeCalibration();
        }

        #region Subframe Part Decoding
        private void DecodeBurstKill()
        {
            if (subframeBytes[Constants.POS_SUB_BURST_KILL_STATUS] == 0x0)
                Subframe.IsBurstKillEnabled = false;
            else if (subframeBytes[Constants.POS_SUB_BURST_KILL_STATUS] == 0x1)
                Subframe.IsBurstKillEnabled = true;
        }

        private void DecodeDeviceType()
        {
            char[] bytes = new char[8];

            for (int i = 0; i < 8; i++)
                bytes[i] = (char)subframeBytes[Constants.POS_SUB_TYPE + i];

            Subframe.DeviceType = new string(bytes);
        }

        private void DecodeFrequency()
        {
            byte b = (byte)(subframeBytes[Constants.POS_SUB_FREQUENCY_LOWER] & 0xC0);
            double f0 = (b * 10) / 64;

            b = subframeBytes[Constants.POS_SUB_FREQUENCY_UPPER];
            double f1 = 40 * b;

            Subframe.Frequency = (400000 + f1 + f0) / 1000;
        }

        private void DecodeCalibration()
        {
            byte[] bytes = new byte[4];

            // Reference resistors
            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_REF_RES1 + i];
            Subframe.ReferenceResistor1 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_REF_RES2 + i];
            Subframe.ReferenceResistor2 = BitConverter.ToSingle(bytes);

            // Temperature thermometer constants
            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_TEMP_CONST1 + i];
            Subframe.ThermoTempConstant1 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_TEMP_CONST2 + i];
            Subframe.ThermoTempConstant2 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_TEMP_CONST3 + i];
            Subframe.ThermoTempConstant3 = BitConverter.ToSingle(bytes);

            // Temperature thermometer calibration
            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_TEMP_CALIB1 + i];
            Subframe.ThermoTempCalibration1 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_TEMP_CALIB2 + i];
            Subframe.ThermoTempCalibration2 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_TEMP_CALIB3 + i];
            Subframe.ThermoTempCalibration3 = BitConverter.ToSingle(bytes);

            // Humidity calibration
            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_HUMIDITY_CALIB + i];
            Subframe.HumidityCalibration = BitConverter.ToSingle(bytes);

            // Humidity thermometer constants
            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_HUMI_CONST1 + i];
            Subframe.ThermoHumiConstant1 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_HUMI_CONST2 + i];
            Subframe.ThermoHumiConstant2 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_HUMI_CONST3 + i];
            Subframe.ThermoHumiConstant3 = BitConverter.ToSingle(bytes);

            // Humidity thermometer calibration
            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_HUMI_CALIB1 + i];
            Subframe.ThermoHumiCalibration1 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_HUMI_CALIB2 + i];
            Subframe.ThermoHumiCalibration2 = BitConverter.ToSingle(bytes);

            for (int i = 0; i < 4; i++)
                bytes[i] = subframeBytes[Constants.POS_SUB_THERMO_HUMI_CALIB3 + i];
            Subframe.ThermoHumiCalibration3 = BitConverter.ToSingle(bytes);
        }
        #endregion
    }
}
