//-----------------------------------------------------------------------------
// ReadEthernetMac.cs
//
// Demonstrates how to read the ethernet MAC from a LabJack.
//
// support@labjack.com
// Dec. 3, 2013
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace ReadEthernetMac
{
    class ReadEthernetMac
    {
        static void Main(string[] args)
        {
            ReadEthernetMac rem = new ReadEthernetMac();
            rem.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            int handle = 0;
            int devType = 0;
            int conType = 0;
            int serNum = 0;
            int ipAddr = 0;
            int port = 0;
            int maxBytesPerMB = 0;
            string ipAddrStr = "";

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Call eAddresses to read the ethernet MAC from the LabJack.
                //Note that we are reading a byte array which is the big endian
                //binary representation of the 64-bit MAC.
                int numFrames = 1;
                int[] aAddresses = new int[] { 60020 };
                int[] aTypes = new int[] { LJM.CONSTANTS.BYTE };
                int[] aWrites = new int[] { LJM.CONSTANTS.READ };
                int[] aNumValues = new int[] { 8 };
                double[] aValues = new double[8];
                int errAddr = -1;
                LJM.eAddresses(handle, numFrames, aAddresses, aTypes, aWrites,
                    aNumValues, aValues, ref errAddr);

                //Convert returned values to bytes 
                byte[] macBytes = Array.ConvertAll<double, byte>(aValues, Convert.ToByte);
                
                //Convert big endian byte array to a 64-bit unsigned integer
                //value
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(macBytes);
                Int64 macNumber = BitConverter.ToInt64(macBytes, 0);

                //Convert the MAC value/number to its string representation
                string macString = "";
                LJM.NumberToMAC(macNumber, ref macString);

                Console.WriteLine("\nEthernet MAC : " + macNumber + " - " + macString);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }
    }
}
