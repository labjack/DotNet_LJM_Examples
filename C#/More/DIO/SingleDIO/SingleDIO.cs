//-----------------------------------------------------------------------------
// SingleDIO.cs
//
// Demonstrates how to set and read a single digital I/O.
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
//     eWriteName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritename
//     eReadName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmereadname
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
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
            string name = "";
            double state = 0;

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

                //Setup and call eWriteName to set the DIO state.
                if(devType == LJM.CONSTANTS.dtT4)
                {
                    //Setting FIO4 on the LabJack T4. FIO0-FIO3 are reserved for
                    //AIN0-AIN3.
                    name = "FIO4";

                    //If the FIO/EIO line is an analog input, it needs to first
                    //be changed to a digital I/O by reading from the line or
                    //setting it to digital I/O with the DIO_ANALOG_ENABLE
                    //register.

                    //Reading from the digital line in case it was previously
                    //an analog input.
                    LJM.eReadName(handle, name, ref state);
                }
                else
                {
                    //Setting FIO0 on the LabJack T7 and other devices.
                    name = "FIO0";
                }
                state = 0;  //Output-low = 0, Output-high = 1
                LJM.eWriteName(handle, name, state);

                Console.WriteLine("\nSet " + name + " state : " + state);

                //Setup and call eReadName to read the DIO state.
                if(devType == LJM.CONSTANTS.dtT4)
                {
                    //Reading from FIO5 on the LabJack T4. FIO0-FIO3 are
                    //reserved for AIN0-AIN3.
                    //Note: Reading a single digital I/O will change the line
                    //from analog to digital input.
                    name = "FIO5";
                }
                else
                {
                    //Reading from FIO1 on the LabJack T7 and other devices.
                    name = "FIO1";
                }
                state = 0;
                LJM.eReadName(handle, name, ref state);

                Console.WriteLine("\n" + name + " state : " + state);
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
