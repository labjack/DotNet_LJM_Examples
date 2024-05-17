//-----------------------------------------------------------------------------
// WriteWifiConfig.cs
//
// Demonstrates how to configure the WiFi settings on a LabJack.
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
//     eWriteName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritename
//     eWriteNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritenames
//     IPToNumber:
//         https://labjack.com/support/software/api/ljm/function-reference/utility/ljmiptonumber
//     NumberToIP:
//         https://labjack.com/support/software/api/ljm/function-reference/utility/ljmnumbertoip
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     WiFi:
//         https://labjack.com/support/datasheets/t-series/wifi
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace WriteWifiConfig
{
    class WriteWifiConfig
    {
        static void Main(string[] args)
        {
            WriteWifiConfig wwc = new WriteWifiConfig();
            wwc.performActions();
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

                //Setup and call eWriteNames to configure WiFi default settings
                //on the LabJack.
                string[] aNames = new string[] { "WIFI_IP_DEFAULT",
                    "WIFI_SUBNET_DEFAULT", "WIFI_GATEWAY_DEFAULT" };
                int ip = 0;
                int subnet = 0;
                int gateway = 0;
                LJM.IPToNumber("192.168.1.207", ref ip);
                LJM.IPToNumber("255.255.255.0", ref subnet);
                LJM.IPToNumber("192.168.1.1", ref gateway);
                double[] aValues = new double[] { (uint)ip, (uint)subnet,
                    (uint)gateway };
                int numFrames = aNames.Length;
                int errAddr = -1;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nSet WiFi configuration:");
                string str = "";
                for(int i = 0; i < numFrames; i++)
                {
                    LJM.NumberToIP((int)Convert.ToUInt32(aValues[i]), ref str);
                    Console.WriteLine("    " + aNames[i] + " : " + aValues[i] +
                        " - " + str);
                }

                //Setup and call eWriteString to configure the default WiFi
                //SSID on the LabJack.
                string name = "WIFI_SSID_DEFAULT";
                str = "LJOpen";
                LJM.eWriteNameString(handle, name, str);
                Console.WriteLine("    " + name + " : " + str);

                //Setup and call eWriteString to configure the default WiFi
                //password on the LabJack.
                name = "WIFI_PASSWORD_DEFAULT";
                str = "none";
                LJM.eWriteNameString(handle, name, str);
                Console.WriteLine("    " + name + " : " + str);

                //Setup and call eWriteName to apply the new WiFi configuration
                //on the LabJack.
                name = "WIFI_APPLY_SETTINGS";
                double value = 1;  //1 = apply
                LJM.eWriteName(handle, name, value);
                Console.WriteLine("    " + name + " : " + value);
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

