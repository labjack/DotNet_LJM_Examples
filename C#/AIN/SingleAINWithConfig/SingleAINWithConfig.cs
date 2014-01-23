//-----------------------------------------------------------------------------
// SingleAINWithConfig.cs
//
// Demonstrates configuring and reading a single analog input (AIN) with a
// LabJack.
//
// support@labjack.com
//-----------------------------------------------------------------------------

using System;
using LabJack;

namespace SingleAINWithConfig
{
    class SingleAINWithConfig
    {
        static void Main(string[] args)
        {
            SingleAINWithConfig sAINWC = new SingleAINWithConfig();
            sAINWC.performActions();
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

                //Setup and call eWriteNames to configure the AIN on the LabJack.
                string[] aNames = { "AIN0_NEGATIVE_CH", "AIN0_RANGE",
                                     "AIN0_RESOLUTION_INDEX" };
                double[] aValues = { 199, 10, 0 };
                int numFrames = aNames.Length;
                int errAddr = 0;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nSet configuration:");
                for(int i = 0; i < numFrames; i++)
                {
                    Console.WriteLine("    " + aNames[i] +  " : " + aValues[i] + " ");
                }
                
                //Setup and call eReadName to read an AIN from the LabJack.
                string name = "AIN0";
                double value = 0;
                LJM.eReadName(handle, name, ref value);

                Console.WriteLine("\n" + name + " reading : " + value.ToString("F4") + " V");
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
