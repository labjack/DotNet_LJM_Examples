//-----------------------------------------------------------------------------
// DualAINLoop.cs
//
// Demonstrates reading 2 analog inputs (AINs) in a loop from a LabJack.
//
// support@labjack.com
//-----------------------------------------------------------------------------

using System;
using System.Threading;
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

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);  // Any device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to configure AINs.
                //AIN0 and AIN1:
                //    Negative Channel = 199 (Single-ended)
                //    Range = +/-10 V
                //        T4 note: Only AIN0-AIN3 can support +/-10 V range.
                //    Resolution index = 0 (default)
                //        T7 note: 0 (default) is index 8, or 9 for Pro.
                //    Settling = 0 (auto)
                int numFrames = 8;
                string[] names = new string[8] { "AIN0_NEGATIVE_CH", "AIN0_RANGE", "AIN0_RESOLUTION_INDEX",
                    "AIN0_SETTLING_US", "AIN1_NEGATIVE_CH", "AIN1_RANGE", "AIN1_RESOLUTION_INDEX",
                    "AIN1_SETTLING_US" };
                double[] aValues = new double[8] {199, 10, 0, 0, 199, 10, 0, 0};
                int errorAddress = 0;
                LJM.eWriteNames(handle, numFrames, names, aValues, ref errorAddress);

                Console.WriteLine("\nSet configuration:");
                for(int i = 0; i < numFrames; i++)
                {
                    Console.WriteLine("  " + names[i] + " : " + aValues[i]);
                }

                //Setup and call eReadNames to read AINs.
                numFrames = 2;
                names = new string[2] {"AIN0", "AIN1"};
                aValues = new double[2] {0, 0};
                
                Console.WriteLine("\nStarting read loop.  Press a key to stop.");
                while(!Console.KeyAvailable)
                {
                    LJM.eReadNames(handle, numFrames, names, aValues, ref errorAddress);
                    Console.WriteLine("\n" + names[0] + " : " + aValues[0].ToString("F4") + " V, " + names[1] + " : " + aValues[1].ToString("F4") + " V");
                    Thread.Sleep(1000); //Wait 1 second
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
