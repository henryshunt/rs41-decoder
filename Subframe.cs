using System.ComponentModel;

namespace RSDecoder
{
    public class Subframe
    {
        public string DeviceType { get; set; }
        public bool IsBurstKillEnabled { get; set; }
        public double Frequency { get; set; }

        public double ReferenceResistor1 { get; set; }
        public double ReferenceResistor2 { get; set; }
        public double ThermoTempConstant1 { get; set; }
        public double ThermoTempConstant2 { get; set; }
        public double ThermoTempConstant3 { get; set; }
        public double ThermoTempCalibration1 { get; set; }
        public double ThermoTempCalibration2 { get; set; }
        public double ThermoTempCalibration3 { get; set; }
        public double HumidityCalibration { get; set; }
        public double ThermoHumiConstant1 { get; set; }
        public double ThermoHumiConstant2 { get; set; }
        public double ThermoHumiConstant3 { get; set; }
        public double ThermoHumiCalibration1 { get; set; }
        public double ThermoHumiCalibration2 { get; set; }
        public double ThermoHumiCalibration3 { get; set; }

        public override string ToString()
        {
            string s = "";

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
                s += string.Format("    {0} = {1}\n", descriptor.Name, descriptor.GetValue(this));

            return s;
        }
    }
}
