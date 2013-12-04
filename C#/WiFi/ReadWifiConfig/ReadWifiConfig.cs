//-----------------------------------------------------------------------------
// ReadWifiConfig.cs
//
// Demonstrates how to read the WiFi configuration from a LabJack.
//
// support@labjack.com
// Dec. 4, 2013
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace ReadWifiConfig
{
    class ReadWifiConfig
    {
        static void Main(string[] args)
        {
            ReadWifiConfig rwc = new ReadWifiConfig();
            rwc.performActions();
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

                //Setup and call eReadNames to read WiFi configuration from the LabJack.
                string[] aNames = new string[] { "WIFI_IP", "WIFI_SUBNET",
                    "WIFI_GATEWAY", "WIFI_DHCP_ENABLE", "WIFI_IP_DEFAULT",
                    "WIFI_SUBNET_DEFAULT", "WIFI_GATEWAY_DEFAULT",
                    "WIFI_DHCP_ENABLE_DEFAULT", "WIFI_STATUS" };
                double[] aValues = new double[aNames.Length];
                int numFrames = aNames.Length;
                int errAddr = -1;
                LJM.eReadNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\neWifi configuration: ");
                string str = "";
                for(int i = 0; i < numFrames; i++)
                {
                    if(aNames[i] == "WIFI_STATUS" || aNames[i].StartsWith("WIFI_DHCP_ENABLE"))
                    {
                        Console.WriteLine("    " + aNames[i] + " : " + aValues[i]);
                    }
                    else
                    {
                        LJM.NumberToIP((int)Convert.ToUInt32(aValues[i]), ref str);
                        Console.WriteLine("    " + aNames[i] + " : " +
                            aValues[i] + " - " + str);
                    }
                }

                //Setup and call eReadNameString to read the WiFi SSID string
                //from the LabJack.
                string name = "WIFI_SSID";
                str = "";
                LJM.eReadNameString(handle, name, ref str);

                Console.WriteLine("    " + name + " : " + str);
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

