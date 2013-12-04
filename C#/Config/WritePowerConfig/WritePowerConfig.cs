﻿//-----------------------------------------------------------------------------
// WritePowerConfig.cs
//
// Demonstrates how to configure default power settings on a LabJack.
//
// support@labjack.com
// Dec. 3, 2013
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);

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
                double[] aValues = new double[] { 1, 0, 1, 1 }; //Eth. On, WiFi Off, AIN On, LED On
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

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); //Pause for user
        }
    }
}
