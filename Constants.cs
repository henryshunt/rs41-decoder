namespace RSDecoder.RS41
{
    public static class Constants
    {
        public static int STANDARD_FRAME_LENGTH = 320;
        public static int EXTENDED_DATA_LENGTH = 198;
        public static int FRAME_LENGTH = STANDARD_FRAME_LENGTH + EXTENDED_DATA_LENGTH;

        public static bool[] HEADER_BITS = {
            false, false, false, false, true, false, false, false, // 00001000
            false, true, true, false, true, true, false, true, // 01101101
            false, true, false, true, false, false, true, true, // 01010011
            true, false, false, false, true, false, false, false, // 10001000
            false, true, false, false, false, true, false, false, // 01000100
            false, true, true, false, true, false, false, true, // 01101001
            false, true, false, false, true, false, false, false, // 01001000
            false, false, false, true, true, true, true, true // 00011111
        };
        public static int HEADER_LENGTH_BITS = 64;

        public static byte[] XOR_MASK = {
            0x96, 0x83, 0x3E, 0x51, 0xB1, 0x49, 0x08, 0x98, 0x32, 0x05, 0x59, 0x0E, 0xF9, 0x44, 0xC6, 0x26,
            0x21, 0x60, 0xC2, 0xEA, 0x79, 0x5D, 0x6D, 0xA1, 0x54, 0x69, 0x47, 0x0C, 0xDC, 0xE8, 0x5C, 0xF1,
            0xF7, 0x76, 0x82, 0x7F, 0x07, 0x99, 0xA2, 0x2C, 0x93, 0x7C, 0x30, 0x63, 0xF5, 0x10, 0x2E, 0x61,
            0xD0, 0xBC, 0xB4, 0xB6, 0x06, 0xAA, 0xF4, 0x23, 0x78, 0x6E, 0x3B, 0xAE, 0xBF, 0x7B, 0x4C, 0xC1
        };
        public static int XOR_MASK_LENGTH = 64;

        public static int POSITION_POST_HEADER = HEADER_LENGTH_BITS / 8;
        public const int POSITION_BLOCK_EMPTY = 0x12B;
    }
}
