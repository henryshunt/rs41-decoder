using System;

namespace RSDecoder.RS41
{
    public class Frame
    {
        public bool IsStatusBlockValid { get; set; }
        public bool IsMeasurementBlockValid { get; set; }
        public bool IsGpsInfoBlockValid { get; set; }
        public bool IsGpsRawBlockValid { get; set; }
        public bool IsGpsPositionBlockValid { get; set; }

        public int FrameNumber { get; set; }
        public bool IsExtendedFrame { get; set; }
        public string SerialNumber { get; set; }

        public DateTime FrameTime { get; set; }
        public double BatteryVoltage { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }

        public double HorizontalVelocity { get; set; }
        public double VerticalVelocity { get; set; }
        public double Direction { get; set; }

        public int GpsSatelliteCount { get; set; }
        public double PositionAccuracy { get; set; }
        public double VelocityAccuracy { get; set; }
    }
}