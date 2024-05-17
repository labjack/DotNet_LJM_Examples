//-----------------------------------------------------------------------------
// WriteEthernetConfig.cs
//
// Demonstrates how to set ethernet configuration settings on a LabJack.
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
//     IPToNumber:
//         https://labjack.com/support/software/api/ljm/function-reference/utility/ljmiptonumber
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Ethernet:
//         https://labjack.com/support/datasheets/t-series/ethernet
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace WriteEthernetConfig
{
    class WriteEthernetConfig
    {
        static void Main(string[] args)
        {
            WriteEthernetConfig wec = new WriteEthernetConfig();
            wec.performActions();
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
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to set the ethernet configuration.
                string[] aNames = new string[] { "ETHERNET_IP_DEFAULT",
                    "ETHERNET_SUBNET_DEFAULT", "ETHERNET_GATEWAY_DEFAULT",
                    "ETHERNET_DHCP_ENABLE_DEFAULT" };
                int ip = 0;
                int subnet = 0;
                int gateway = 0;
                int dhcpEnable = 0;
                LJM.IPToNumber("192.168.1.207", ref ip);
                LJM.IPToNumber("255.255.255.0", ref subnet);
                LJM.IPToNumber("192.168.1.1", ref gateway);
                dhcpEnable = 1;  //1 = Enable, 0 = Disable
                double[] aValues = new double[] { (uint)ip, (uint)subnet,
                    (uint)gateway, dhcpEnable };
                int numFrames = aNames.Length;
                int errAddr = -1;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nSet ethernet configuration:");
                string str = "";
                for(int i = 0; i < numFrames; i++)
                {
                    if(aNames[i] == "ETHERNET_DHCP_ENABLE_DEFAULT")
                    {
                        Console.WriteLine("    " + aNames[i] + " : " + aValues[i]);
                    }
                    else
                    {
                        LJM.NumberToIP((int)Convert.ToUInt32(aValues[i]), ref str);
                        Console.WriteLine("    " + aNames[i] + " : " + aValues[i] +
                            " - " + str);
                    }
                }
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
