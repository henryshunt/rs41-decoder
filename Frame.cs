using System;

namespace RS41Decoder.RS41
{
    public class Frame
    {
        public bool IsExtendedFrame { get; set; }
        public int FrameNumber { get; set; }
        public string SerialNumber { get; set; }
        public double BatteryVoltage { get; set; }
        public int SubframeNumber { get; set; }
        public DateTime FrameTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
        public double HorizontalVelocity { get; set; }
        public double VerticalVelocity { get; set; }
        public double Direction { get; set; }
    }
}
