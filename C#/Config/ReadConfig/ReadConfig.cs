//-----------------------------------------------------------------------------
// ReadConfig.cs
//
// Demonstrates how to read configuration settings on a LabJack.
//
// support@labjack.com
// Nov. 22, 2013
//-----------------------------------------------------------------------------

using System;
using LabJack;

namespace ReadConfig
{
    class ReadConfig
    {
        static void Main(string[] args)
        {
            ReadConfig rc = new ReadConfig();
            rc.performActions();
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

                //Setup and call eReadNames to read config. values from the
                //LabJack.
                string[] aNames = { "PRODUCT_ID", "HARDWARE_VERSION",
                                      "FIRMWARE_VERSION", "BOOTLOADER_VERSION",
                                      "WIFI_VERSION", "SERIAL_NUMBER",
                                      "POWER_ETHERNET_DEFAULT",
                                      "POWER_WIFI_DEFAULT", "POWER_AIN_DEFAULT",
                                      "POWER_LED_DEFAULT" };
                int numFrames = aNames.Length;
                double[] aValues = new double[numFrames];
                int errAddr = 0;
                LJM.eReadNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nConfiguration settings:");
                for (int i = 0; i < numFrames; i++)
                    Console.WriteLine("    " + aNames[i] + " : " + aValues[i].ToString("0.####"));
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
