//-----------------------------------------------------------------------------
// SingleDIO.cs
//
// Demonstrates how to set and read a single digital I/O.
//
// support@labjack.com
// April 16, 2013
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace SingleDIO
{
    class SingleDIO
    {
        static void Main(string[] args)
        {
            SingleDIO sd = new SingleDIO();
            sd.performActions();
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
                //LJM.Open(devType, conType, "LJM_idANY", ref handle);
                
                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                
                //Setup and call eWriteName to set the DIO state.
                string name = "FIO0";
                double state = 1; //Output-low = 0, Output-high = 1
                LJM.eWriteName(handle, name, state);

                Console.WriteLine("\nSet " + name + " state : " + state);

                //Setup and call eReadName to read the DIO state.
                name = "FIO1";
                state = 0;
                LJM.eReadName(handle, name, ref state);

                Console.WriteLine("\n" + name + " state : " + state);
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
