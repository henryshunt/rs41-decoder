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
    }
}
