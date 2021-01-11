﻿namespace RSDecoder.RS41
{
    public static class Constants
    {
        public const int BAUD_RATE = 4800;
        public const int WAV_CHANNEL = 0;

        /// <summary>
        /// The length, in bytes, of a standard RS41 data frame.
        /// </summary>
        public static int STANDARD_FRAME_LENGTH = 320;

        /// <summary>
        /// The length, in bytes, of the extended part of an extended RS41 data frame.
        /// </summary>
        public static int EXTENDED_DATA_LENGTH = 198;

        /// <summary>
        /// The length, in bytes, of an RS41 data frame.
        /// </summary>
        public static int FRAME_LENGTH = STANDARD_FRAME_LENGTH + EXTENDED_DATA_LENGTH;

        /// <summary>
        /// The header, in bits, of an RS41 data frame.
        /// </summary>
        public static bool[] FRAME_HEADER = {
            false, false, false, false, true, false, false, false, // 00001000
            false, true, true, false, true, true, false, true, // 01101101
            false, true, false, true, false, false, true, true, // 01010011
            true, false, false, false, true, false, false, false, // 10001000
            false, true, false, false, false, true, false, false, // 01000100
            false, true, true, false, true, false, false, true, // 01101001
            false, true, false, false, true, false, false, false, // 01001000
            false, false, false, true, true, true, true, true // 00011111
        };

        public static byte[] FRAME_MASK = {
            0x96, 0x83, 0x3E, 0x51, 0xB1, 0x49, 0x08, 0x98, 0x32, 0x05, 0x59, 0x0E, 0xF9, 0x44, 0xC6, 0x26,
            0x21, 0x60, 0xC2, 0xEA, 0x79, 0x5D, 0x6D, 0xA1, 0x54, 0x69, 0x47, 0x0C, 0xDC, 0xE8, 0x5C, 0xF1,
            0xF7, 0x76, 0x82, 0x7F, 0x07, 0x99, 0xA2, 0x2C, 0x93, 0x7C, 0x30, 0x63, 0xF5, 0x10, 0x2E, 0x61,
            0xD0, 0xBC, 0xB4, 0xB6, 0x06, 0xAA, 0xF4, 0x23, 0x78, 0x6E, 0x3B, 0xAE, 0xBF, 0x7B, 0x4C, 0xC1
        };


        public static int POS_ECC = 0x08; // 48 bytes

        // Status block
        public static int POS_STATUS_BLOCK = 0x039;
        public static int STATUS_BLOCK_HEADER = 0x7928;

        public static int POS_FRAME_TYPE = 0x038; // 1 byte
        public static int POS_FRAME_NUMBER = 0x03B; // 2 bytes
        public static int POS_SERIAL_NUMBER = 0x03D; // 8 bytes
        public static int POS_BATTERY_VOLTAGE = 0x045; // 1 byte
        public static int POS_SUBFRAME_NUMBER = 0x052; // 1 byte
        public static int POS_SUBFRAME_BYTES = 0x053; // 16 bytes
        public static int SUBFRAME_LAST_NUMBER = 50;
        public static int SUBFRAME_LENGTH = 816;

        // Measurement block
        public static int POS_MEASUREMENT_BLOCK = 0x065;
        public static int MEASUREMENT_BLOCK_HEADER = 0x7A2A;

        public static int POS_TEMPERATURE_MAIN = 0; // 3 bytes
        public static int POS_TEMPERATURE_REF1 = 0; // 3 bytes
        public static int POS_TEMPERATURE_REF2 = 0; // 3 bytes
        public static int POS_HUMIDITY_MAIN = 0; // 3 bytes
        public static int POS_HUMIDITY_REF1 = 0; // 3 bytes
        public static int POS_HUMIDITY_REF2 = 0; // 3 bytes
        public static int POS_HUMIDITY_TEMPERATURE_MAIN = 0; // 3 bytes
        public static int POS_HUMIDITY_TEMPERATURE_REF1 = 0; // 3 bytes
        public static int POS_HUMIDITY_TEMPERATURE_REF2 = 0; // 3 bytes
        public static int POS_PRESSURE_MAIN = 0; // 3 bytes
        public static int POS_PRESSURE_REF1 = 0; // 3 bytes
        public static int POS_PRESSURE_REF2 = 0; // 3 bytes

        // GPS info block
        public static int POS_GPS_INFO_BLOCK = 0x093;
        public static int GPS_INFO_BLOCK_HEADER = 0x7C1E;

        public static int POS_GPS_WEEK = 0x095; // 2 bytes
        public static int POS_GPS_TIME_OF_WEEK = 0x097; // 4 bytes

        // GPS raw block
        public static int POS_GPS_RAW_BLOCK = 0x0B5;
        public static int GPS_RAW_BLOCK_HEADER = 0x7D59;

        // GPS position block
        public static int POS_GPS_POSITION_BLOCK = 0x112;
        public static int GPS_POSITION_BLOCK_HEADER = 0x7B15;

        public static int POS_ECEF_POSITION_X = 0x114; // 4 bytes
        public static int POS_ECEF_POSITION_Y = 0x118; // 4 bytes
        public static int POS_ECEF_POSITION_Z = 0x11C; // 4 bytes
        public static int POS_ECEF_VELOCITY_X = 0x120; // 2 bytes
        public static int POS_ECEF_VELOCITY_Y = 0x122; // 2 bytes
        public static int POS_ECEF_VELOCITY_Z = 0x124; // 2 bytes
        public static int POS_GPS_SATELLITE_COUNT = 0x126; // 1 byte
        public static int POS_VELOCITY_ACCURACY = 0x127; // 1 byte
        public static int POS_POSITION_ACCURACY = 0x128; // 1 byte


        public static int POS_SUB_TYPE = 0x218; // 8 bytes
        public static int POS_SUB_BK_STATUS = 0x02B; // 1 byte
        public static int POS_SUB_FREQUENCY_LOWER = 0x002; // 1 byte
        public static int POS_SUB_FREQUENCY_UPPER = 0x003; // 1 byte
    }
}
