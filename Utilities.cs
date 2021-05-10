using System;

namespace RSDecoder.RS41
{
    class Utilities
    {
        private const double EARTH_A = 6378137;
        private const double EARTH_B = 6356752.31424518;
        private const double EARTH_A2_B2 = EARTH_A * EARTH_A - EARTH_B * EARTH_B;

        private const double e2 = EARTH_A2_B2 / (EARTH_A * EARTH_A);
        private const double ee2 = EARTH_A2_B2 / (EARTH_B * EARTH_B);

        public static (double, double, double) EcefToLlh(double x, double y, double z)
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

        public static (double, double, double) EcefToHdv(double latitude, double longitude, double x, double y, double z)
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
    }
}
