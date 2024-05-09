//-----------------------------------------------------------------------------
// ReadWifiRssi.cs
//
// Demonstrates how to read the WiFi RSSI from a LabJack.
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
//     eReadName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmereadname
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     WiFi:
//         https://labjack.com/support/datasheets/t-series/wifi
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace ReadWifiRssi
{
    class ReadWifiRssi
    {
        static void Main(string[] args)
        {
            ReadWifiRssi rwr = new ReadWifiRssi();
            rwr.performActions();
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
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  // T8 device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                if (devType == LJM.CONSTANTS.dtT4)
                {
                    Console.WriteLine("\nThe LabJack T4 does not support WiFi.");
                    goto Done;
                }

                //Setup and call eReadName to read the WiFi RSSI from the
                //LabJack.
                string name = "WIFI_RSSI";
                double value = 0;
                LJM.eReadName(handle, name, ref value);

                Console.WriteLine("\n" + name + " : " + value);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

        Done:
            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
