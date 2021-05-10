using System;
using System.ComponentModel;

namespace Rs41Decoder
{
    public class Frame
    {
        public int? Number { get; set; } = null;
        public DateTime? Time { get; set; } = null;
        public bool IsExtendedFrame { get; set; }
        public string? SerialNumber { get; set; } = null;
        public double? BatteryVoltage { get; set; } = null;

        public double? Temperature { get; set; } = null;
        public double? Humidity { get; set; } = null;
        public double? HumidityModuleTemp { get; set; } = null;

        public double? Latitude { get; set; } = null;
        public double? Longitude { get; set; } = null;
        public double? Elevation { get; set; } = null;

        public double? HorizontalVelocity { get; set; } = null;
        public double? VerticalVelocity { get; set; } = null;
        public double? Direction { get; set; } = null;

        public int? GpsSatelliteCount { get; set; } = null;
        public double? PositionAccuracy { get; set; } = null;
        public double? VelocityAccuracy { get; set; } = null;

        public Subframe? Subframe { get; set; } = null;

        public override string ToString()
        {
            string s = "";

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
                s += string.Format("{0} = {1}\n", descriptor.Name, descriptor.GetValue(this));

            return s;
        }
    }
}