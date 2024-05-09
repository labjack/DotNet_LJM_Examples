//-----------------------------------------------------------------------------
// ListAll.cs
//
// Demonstrates usage of the ListAll functions (LJM_ListAll) which scans for
// LabJack devices and returns information about the found devices. This
// will only find LabJack devices supported by the LJM library.
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
//     ListAll:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmlistall
//     ListAllS:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmlistalls
//     NumberToIP:
//         https://labjack.com/support/software/api/ljm/function-reference/utility/ljmnumbertoip
//     Constants:
//         https://labjack.com/support/software/api/ljm/constants
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using LabJack;


namespace ListAll
{
    class ListAll
    {
        static void Main(string[] args)
        {
            ListAll la = new ListAll();
            la.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            Dictionary<int, string> DEVICE_NAMES = new Dictionary<int, string> {
                {LJM.CONSTANTS.dtT8, "T8"},
                {LJM.CONSTANTS.dtT7, "T7"},
                {LJM.CONSTANTS.dtT4, "T4"},
                {LJM.CONSTANTS.dtDIGIT, "Digit"}
            };

            Dictionary<int, string> CONN_NAMES = new Dictionary<int, string> {
                {LJM.CONSTANTS.ctUSB, "USB"},
                {LJM.CONSTANTS.ctTCP, "TCP"},
                {LJM.CONSTANTS.ctETHERNET, "Ethernet"},
                {LJM.CONSTANTS.ctWIFI, "WiFi"},
            };

            const int MAX_SIZE = LJM.CONSTANTS.LIST_ALL_SIZE;
            int numFound = 0;
            int[] aDeviceTypes = new int[MAX_SIZE];
            int[] aConnectionTypes = new int[MAX_SIZE];
            int[] aSerialNumbers = new int[MAX_SIZE];
            int[] aIPAddresses = new int[MAX_SIZE];

            try
            {
                //Find and display LabJack devices with listAllS.
                LJM.ListAllS("ANY", "ANY", ref numFound, aDeviceTypes, aConnectionTypes, aSerialNumbers, aIPAddresses);
                Console.WriteLine("ListAllS found " + numFound + " LabJacks:\n");

                /*
                //Find and display LabJack devices with listAll.
                LJM.ListAll(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, ref numFound, aDeviceTypes, aConnectionTypes, aSerialNumbers, aIPAddresses);
                Console.WriteLine("ListAll found " + numFound + " LabJacks:\n");
                */

                Console.WriteLine(String.Format("{0, -18}{1, -18}{2, -18}{3, -18}", "Device Type", "Connection Type", "Serial Number", "IP Address"));
                for(int i = 0; i < numFound; i++)
                {
                    string dev;
                    if(!DEVICE_NAMES.TryGetValue(aDeviceTypes[i], out dev))
                        dev = aDeviceTypes[i].ToString();
                    string con;
                    if(!CONN_NAMES.TryGetValue(aConnectionTypes[i], out con))
                        con = aConnectionTypes[i].ToString();
                    string ip = "";
                    LJM.NumberToIP(aIPAddresses[i], ref ip);
                    Console.WriteLine(String.Format("{0, -18}{1, -18}{2, -18}{3, -18}", dev, con, aSerialNumbers[i], ip));
                }
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
