using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Rs41Decoder
{
    internal class SubframeDecoder
    {
        private readonly List<byte[]> subframeParts = new List<byte[]>();
        private int lastSubframeNumber = -1;

        private readonly byte[] subframeBytes = new byte[Constants.SUBFRAME_LENGTH];

        public Rs41Subframe? Subframe { get; private set; } = null;


        public bool AddSubframePart(int subframeNumber, byte[] subframeBytes)
        {
            if (subframeNumber == lastSubframeNumber + 1)
            {
                subframeParts.Add(subframeBytes);
                if (subframeNumber == Constants.SUBFRAME_NUMBER_FINAL)
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

            Subframe = new Rs41Subframe();

            if (subframeBytes[Constants.POS_SUB_BURST_KILL_STATUS] == 0x0)
                Subframe.IsBurstKillEnabled = false;
            else if (subframeBytes[Constants.POS_SUB_BURST_KILL_STATUS] == 0x1)
                Subframe.IsBurstKillEnabled = true;

            DecodeDeviceType();
            DecodeFrequency();
            DecodeCalibration();
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
    }
}
