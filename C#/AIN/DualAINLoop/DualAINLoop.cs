//-----------------------------------------------------------------------------
// DualAINLoop.cs
//
// Demonstrates reading 2 analog inputs (AINs) in a loop from a LabJack.
//
// support@labjack.com
// July 16, 2013
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
            Console.Out.WriteLine("Error: " + e.ToString());
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
                devType = LJM.CONSTANTS.dtANY; //Any device type
                conType = LJM.CONSTANTS.ctANY; //Any connection type
                LJM.Open(devType, conType, "ANY", ref handle);
                //LJM.OpenS("ANY", "ANY", "ANY", ref handle);

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to configure AINs
                //Setting AIN0-1 Negative Channel to 199 (Single-ended), Range to +-10 V, Resolution
                //index to 0 (default: index 8 or 9 for Pro) and Settling to 0 (automatic)
                int numFrames = 6;
                string[] names = new string[6] {"AIN0_NEGATIVE_CH", "AIN0_RANGE", "AIN0_RESOLUTION_INDEX",
                    "AIN1_NEGATIVE_CH", "AIN1_RANGE", "AIN1_RESOLUTION_INDEX"};
                double[] aValues = new double[6] {199, 10, 0, 199, 10, 0};
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
                
                Console.WriteLine("Starting read loop.  Press a key to stop.");
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

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }
    }
}
