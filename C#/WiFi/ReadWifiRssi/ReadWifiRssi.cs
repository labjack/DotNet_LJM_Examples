﻿//-----------------------------------------------------------------------------
// ReadWifiRssi.cs
//
// Demonstrates how to read the WiFi RSSI from a LabJack.
//
// support@labjack.com
// Dec. 4, 2013
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

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

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }
    }
}
