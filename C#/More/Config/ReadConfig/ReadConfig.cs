//-----------------------------------------------------------------------------
// ReadConfig.cs
//
// Demonstrates how to read configuration settings on a LabJack.
//
// support@labjack.com
//
// Relevant Documentation:
//
// LJM Library:
//     LJM Library Installer:
//         https://labjack.com/support/software/installers/ljm
//     LJM Users Guide:
//         https://labjack.com/support/software/api/ljm
//     Opening and Closing:
//         https://labjack.com/support/software/api/ljm/function-reference/opening-and-closing
//     eReadNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmereadnames
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Hardware Overview (Device Information Registers):
//         https://labjack.com/support/datasheets/t-series/hardware-overview
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
            string[] aNames;
            double[] aValues;
            int numFrames = 0;
            int errAddr = 0;

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
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //LabJack T4 configuration to read
                    aNames = new string[] {
                        "PRODUCT_ID", "HARDWARE_VERSION",
                        "FIRMWARE_VERSION", "BOOTLOADER_VERSION",
                        "SERIAL_NUMBER", "POWER_ETHERNET_DEFAULT",
                        "POWER_AIN_DEFAULT", "POWER_LED_DEFAULT"
                    };
                }
                else
                {
                    //LabJack T7 and other devices configuration to read
                    aNames = new string[] {
                        "PRODUCT_ID", "HARDWARE_VERSION",
                        "FIRMWARE_VERSION", "BOOTLOADER_VERSION",
                        "WIFI_VERSION", "SERIAL_NUMBER",
                        "POWER_ETHERNET_DEFAULT", "POWER_WIFI_DEFAULT",
                        "POWER_AIN_DEFAULT", "POWER_LED_DEFAULT"
                    };
                }
                numFrames = aNames.Length;
                aValues = new double[numFrames];
                errAddr = 0;
                LJM.eReadNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nConfiguration settings:");
                for (int i = 0; i < numFrames; i++)
                    Console.WriteLine("    " + aNames[i] + " : " + aValues[i].ToString("0.####"));
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  // Pause for user
        }
    }
}
