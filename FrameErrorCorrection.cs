﻿using STH1123.ReedSolomon;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSDecoder.RS41
{
    public class FrameErrorCorrection
    {
        private byte[] frameBytes;
        private Frame decodedFrame;

        public FrameErrorCorrection(byte[] frameBytes, Frame decodedFrame)
        {
            this.frameBytes = frameBytes;
            this.decodedFrame = decodedFrame;
        }

        //public void Correct()
        //{
        //    for (int i = Constants.STANDARD_FRAME_LENGTH; i < Constants.FRAME_LENGTH; i++)
        //        frameBytes[i] = 0;

        //    byte[] rs_codeword_1 = new byte[255];

        //    for (int i = 0; i < 24; i++)
        //        rs_codeword_1[i] = frameBytes[Constants.POS_ECC + i];

        //    for (int i = 0; i < 225 - 24; i++)
        //        rs_codeword_1[24 + i] = frameBytes[Constants.POS_FRAME_TYPE + (2 * i)];


        //    byte[] rs_codeword_2 = new byte[255];

        //    for (int i = 0; i < 24; i++)
        //        rs_codeword_2[i] = frameBytes[Constants.POS_ECC + 24 + i];

        //    for (int i = 0; i < 225 - 24; i++)
        //        rs_codeword_2[24 + i] = frameBytes[Constants.POS_FRAME_TYPE + (2 * i) + 1];


        //    foreach (byte b in rs_codeword_1)
        //        Console.Write("{0:X2} ", b);
        //    Console.WriteLine();
        //    Console.WriteLine();
        //    foreach (byte b in rs_codeword_2)
        //        Console.Write("{0:X2} ", b);
        //    Console.WriteLine();
        //    Console.WriteLine();

        //    //GenericGF field = new GenericGF(285, 256, 0);
        //    //ReedSolomonDecoder rsd = new ReedSolomonDecoder(field);
        //    //rsd.Decode(rs_codeword_1, 9, null);
        //}

        public void Correct()
        {
            decodedFrame.IsStatusBlockValid = CheckBlockValidity(Constants.POS_STATUS_BLOCK, Constants.STATUS_BLOCK_HEADER);
            decodedFrame.IsMeasurementBlockValid = CheckBlockValidity(Constants.POS_MEASUREMENT_BLOCK, Constants.MEASUREMENT_BLOCK_HEADER);
            decodedFrame.IsGpsInfoBlockValid = CheckBlockValidity(Constants.POS_GPS_INFO_BLOCK, Constants.GPS_INFO_BLOCK_HEADER);
            decodedFrame.IsGpsRawBlockValid = CheckBlockValidity(Constants.POS_GPS_RAW_BLOCK, Constants.GPS_RAW_BLOCK_HEADER);
            decodedFrame.IsGpsPositionBlockValid = CheckBlockValidity(Constants.POS_GPS_POSITION_BLOCK, Constants.GPS_POSITION_BLOCK_HEADER);

            //for (int i = Constants.STANDARD_FRAME_LENGTH; i < Constants.FRAME_LENGTH; i++)
            //    frameBytes[i] = 0;

            //int[] rs_codeword_1 = new int[255];

            //for (int i = 0; i < 225 - 24; i++)
            //    rs_codeword_1[i] = frameBytes[Constants.POS_FRAME_TYPE + (2 * i)];

            //for (int i = 0; i < 24; i++)
            //    rs_codeword_1[231 + i] = frameBytes[Constants.POS_ECC + i];


            //int[] rs_codeword_2 = new int[255];

            //for (int i = 0; i < 225 - 24; i++)
            //    rs_codeword_2[i] = frameBytes[Constants.POS_FRAME_TYPE + (2 * i) + 1];

            //for (int i = 0; i < 24; i++)
            //    rs_codeword_2[231 + i] = frameBytes[Constants.POS_ECC + 24 + i];


            //foreach (byte b in rs_codeword_1)
            //    Console.Write("{0:X2} ", b);
            //Console.WriteLine();
            //Console.WriteLine();
            //foreach (byte b in rs_codeword_2)
            //    Console.Write("{0:X2} ", b);
            //Console.WriteLine();
            //Console.WriteLine();


            //GenericGF field = new GenericGF(285, 256, 0, 2);
            //ReedSolomonDecoder rsd = new ReedSolomonDecoder(field);

            //Console.WriteLine(rsd.Decode(rs_codeword_1, 24));
            //Console.WriteLine(rsd.Decode(rs_codeword_2, 24));
        }

        private bool CheckBlockValidity(int blockStartPos, int blockHeader)
        {
            if (frameBytes[blockStartPos] != blockHeader >> 8)
                return false;

            int crcDataLength = frameBytes[blockStartPos + 1];

            if (blockStartPos + crcDataLength + 4 > Constants.FRAME_LENGTH)
                return false;

            // Combine the two CRC-16 value bytes into a single integer
            int crc = BitConverter.ToUInt16(new byte[] {
                frameBytes[blockStartPos + 2 + crcDataLength],
                frameBytes[blockStartPos + 2 + crcDataLength + 1]
            });

            return Crc16(blockStartPos + 2, crcDataLength) == crc;
        }

        private int Crc16(int start, int len)
        {
            int crc16poly = 0x1021;
            int rem = 0xFFFF;

            if (start + len + 2 > Constants.FRAME_LENGTH)
                return -1;

            for (int i = 0; i < len; i++)
            {
                int b = frameBytes[start + i];
                rem = rem ^ (b << 8);

                for (int j = 0; j < 8; j++)
                {
                    if ((rem & 0x8000) > 0)
                        rem = (rem << 1) ^ crc16poly;
                    else rem = (rem << 1);

                    rem &= 0xFFFF;
                }
            }

            return rem;
        }
    }
}
