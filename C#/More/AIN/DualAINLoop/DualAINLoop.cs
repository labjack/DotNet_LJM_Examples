//-----------------------------------------------------------------------------
// DualAINLoop.cs
//
// Demonstrates reading 2 analog inputs (AINs) in a loop from a LabJack.
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
//     Multiple Value Functions (such as eWriteNames and eReadNames):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//     Timing Functions (such as StartInterval, WaitForNextInterval and
//     CleanInterval):
//         https://labjack.com/support/software/api/ljm/function-reference/timing-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//-----------------------------------------------------------------------------

using System;
using LabJack;

namespace DualAINLoop
{
    class DualAINLoop
    {
        static void Main(string[] args)
        {
            DualAINLoop dal = new DualAINLoop();
            dal.performActions();
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
            int numFrames = 0;
            string[] aNames;
            double[] aValues;
            int errorAddress = -1;
            int intervalHandle = 1;
            int skippedIntervals = 0;

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

                //Setup and call eWriteNames to configure AINs.
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //LabJack T4 configuration
                    //    Resolution index = 0 (default)
                    //    Settling = 0 (auto)
                    aNames = new string[] { "AIN0_RESOLUTION_INDEX", "AIN1_RESOLUTION_INDEX",
                        "AIN0_SETTLING_US", "AIN1_SETTLING_US" };
                    aValues = new double[] { 0, 0, 0, 0};
                    numFrames = aNames.Length;
                    LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);
                }
                else
                {
                    //LabJack T7 and T8 configuration

                    // Settling and negative channel do not apply to the T8
                    if (devType == LJM.CONSTANTS.dtT7)
                    {
                        // Negative Channel = 199 (Single-ended)
                        // Settling = 0 (auto)
                        aNames = new string[] {  "AIN0_NEGATIVE_CH","AIN1_NEGATIVE_CH",
                        "AIN0_SETTLING_US", "AIN1_SETTLING_US"};
                        aValues = new double[] { 199, 199, 0, 0 };
                        numFrames = aNames.Length;
                        LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);
                    }
                    //    Range = ±10V (T7) or ±11V (T8).
                    //    Resolution index = 0 (default).
                    aNames = new string[] {  "AIN0_RANGE", "AIN1_RANGE",
                        "AIN0_RESOLUTION_INDEX", "AIN1_RESOLUTION_INDEX"};
                    aValues = new double[] { 10, 10, 0, 0 };
                    numFrames = aNames.Length;
                    LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                }

                //Setup and call eReadNames to read AINs.
                aNames = new string[] {"AIN0", "AIN1"};
                aValues = new double[] {0, 0};
                numFrames = aNames.Length;

                Console.WriteLine("\nStarting read loop.  Press a key to stop.");
                LJM.StartInterval(intervalHandle, 1000000);
                while(!Console.KeyAvailable)
                {
                    LJM.eReadNames(handle, numFrames, aNames, aValues, ref errorAddress);
                    Console.WriteLine("\n" + aNames[0] + " : " + aValues[0].ToString("F4") + " V, " +
                        aNames[1] + " : " + aValues[1].ToString("F4") + " V");
                    LJM.WaitForNextInterval(intervalHandle, ref skippedIntervals);  //Wait 1 second
                    if(skippedIntervals > 0)
                    {
                        Console.WriteLine("SkippedIntervals: " + skippedIntervals);
                    }
                }
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            //Close interval and all device handles
            LJM.CleanInterval(intervalHandle);
            LJM.CloseAll();

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  // Pause for user
        }
    }
}
