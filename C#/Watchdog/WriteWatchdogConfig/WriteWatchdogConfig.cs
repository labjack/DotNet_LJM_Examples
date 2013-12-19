//-----------------------------------------------------------------------------
// WriteWatchdogConfig.cs
//
// Demonstrates how to configure the Watchdog on a LabJack.
//
// support@labjack.com
// Dec. 19, 2013
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace WriteWatchdogConfig
{
    class WriteWatchdogConfig
    {
        static void Main(string[] args)
        {
            WriteWatchdogConfig wwc = new WriteWatchdogConfig();
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to configure the Watchdog on a
                //LabJack. Disable the Watchdog first before any other
                //configuration.
                string[] aNames = new string[] {
                    "WATCHDOG_ENABLE_DEFAULT", "WATCHDOG_ADVANCED_DEFAULT",
                    "WATCHDOG_TIMEOUT_S_DEFAULT", "WATCHDOG_STARTUP_DELAY_S_DEFAULT",
                    "WATCHDOG_STRICT_ENABLE_DEFAULT", "WATCHDOG_STRICT_KEY_DEFAULT",
                    "WATCHDOG_RESET_ENABLE_DEFAULT", "WATCHDOG_DIO_ENABLE_DEFAULT",
                    "WATCHDOG_DIO_STATE_DEFAULT", "WATCHDOG_DIO_DIRECTION_DEFAULT",
                    "WATCHDOG_DIO_INHIBIT_DEFAULT", "WATCHDOG_DAC0_ENABLE_DEFAULT",
                    "WATCHDOG_DAC0_DEFAULT", "WATCHDOG_DAC1_ENABLE_DEFAULT",
                    "WATCHDOG_DAC1_DEFAULT", "WATCHDOG_ENABLE_DEFAULT"};
                double[] aValues = new double[] {
                    0, 0,
                    20, 0,
                    0, 0,
                    1, 0,
                    0, 0,
                    0, 0,
                    0, 0,
                    0, 0}; //Set WATCHDOG_ENABLE_DEFAULT to 1 to enable
                int numFrames = aNames.Length;
                int errAddr = -1;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);

                Console.WriteLine("\nSet Watchdog configuration:");
                for(int i = 0; i < numFrames; i++)
                    Console.WriteLine("    " + aNames[i] +" : " + aValues[i]);

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
