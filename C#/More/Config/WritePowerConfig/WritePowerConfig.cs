//-----------------------------------------------------------------------------
// WritePowerConfig.cs
//
// Demonstrates how to configure default power settings on a LabJack.
// Note: The T8 is not supported for this example.
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
//     eWriteNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritenames
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     WiFi:
//         https://labjack.com/support/datasheets/t-series/wifi
//     Ethernet:
//         https://labjack.com/support/datasheets/t-series/ethernet
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace WritePowerConfig
{
    class WritePowerConfig
    {
        static void Main(string[] args)
        {
            WritePowerConfig wpc = new WritePowerConfig();
            wpc.performActions();
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);  // Any device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to write configuration values to
                //the LabJack.
                string[] aNames = new string[] { "POWER_ETHERNET_DEFAULT",
                    "POWER_WIFI_DEFAULT", "POWER_AIN_DEFAULT",
                    "POWER_LED_DEFAULT" };
                double[] aValues = new double[] { 1, 0, 1, 1 };  //Eth. On, WiFi Off, AIN On, LED On
                int numFrames = aNames.Length;
                int errAddr = -1;

                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nSet configuration settings:");

                for (int i = 0; i < numFrames; i++)
                    Console.WriteLine("    " + aNames[i] + " : " + aValues[i]);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
