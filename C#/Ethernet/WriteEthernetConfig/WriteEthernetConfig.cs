//-----------------------------------------------------------------------------
// WriteEthernetConfig.cs
//
// Demonstrates how to set ethernet configuration settings on a LabJack.
//
// support@labjack.com
// Dec. 3, 2013
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to set the ethernet configuration
                //on the LabJack.
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
                dhcpEnable = 1; //1 = Enable, 0 = Disable
                double[] aValues = new double[] { ip, subnet, gateway, dhcpEnable };
                int numFrames = aNames.Length;
                int errAddr = -1;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nSet ethernet configuration: ");
                string str = "";
                for(int i = 0; i < numFrames; i++)
                {
                    if(aNames[i] == "ETHERNET_DHCP_ENABLE_DEFAULT")
                    {
                        Console.WriteLine("    " + aNames[i] + " : " + aValues[i]);
                    }
                    else
                    {
                        LJM.NumberToIP(Convert.ToInt32(aValues[i]), ref str);
                        Console.WriteLine("    " + aNames + " : " + aValues[i] +
                            " - " + str);
                    }
                }
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
