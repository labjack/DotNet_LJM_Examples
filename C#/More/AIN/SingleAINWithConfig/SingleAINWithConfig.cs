//-----------------------------------------------------------------------------
// SingleAINWithConfig.cs
//
// Demonstrates configuring and reading a single analog input (AIN) with a
// LabJack.
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
//     eReadName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmereadname
//     Multiple Value Functions (such as eWriteNames):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//-----------------------------------------------------------------------------
using System;
using System.Threading;
using LabJack;

namespace SingleAINWithConfig
{
    class SingleAINWithConfig
    {
        static void Main(string[] args)
        {
            SingleAINWithConfig sAINWC = new SingleAINWithConfig();
            sAINWC.performActions();
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
            string name = "";
            double value = 0;

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

                //Setup and call eWriteNames to configure the AIN on the LabJack.
                if (devType == LJM.CONSTANTS.dtT8)
                {
                    //LabJack T8 configuration
                    
                    //AIN0:
                    //    Range = ±11V (T8).
                    //    Resolution index = 0 (default)
                    //    Sampling rate Hz = 0 (auto)
                    aNames = new string[] { "AIN0_RANGE",
                        "AIN0_RESOLUTION_INDEX",
                        "AIN_SAMPLING_RATE_HZ" };
                    aValues = new double[] { 11,
                        0,
                        0 };
                }
                else if (devType == LJM.CONSTANTS.dtT4)
                {
                    //LabJack T4 configuration

                    //AIN0:
                    //    Resolution index = 0 (default)
                    //    Settling = 0 (auto)
                    aNames = new string[] { "AIN0_RESOLUTION_INDEX",
                        "AIN0_SETTLING_US" };
                    aValues = new double[] { 0,
                        0 };
                }
                else
                {
                    //LabJack T7 configuration

                    //AIN0:
                    //    Negative Channel = 199 (Single-ended)
                    //    Settling = 0 (auto)
                    //    Range = ±10V (T7)
                    //    Resolution index = 0 (default)
                    aNames = new string[] { "AIN0_NEGATIVE_CH",
                        "AIN0_SETTLING_US",
                        "AIN0_RANGE",
                        "AIN0_RESOLUTION_INDEX" };
                    aValues = new double[] { 199,
                        0,
                        10,
                        0};
                }
                numFrames = aNames.Length;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                if (devType == LJM.CONSTANTS.dtT8)
                {
                    //Delay for updated settings to take effect on the T8.
                    Thread.Sleep(50);
                }

                //Setup and call eReadName to read an AIN from the LabJack.
                name = "AIN0";
                value = 0;
                LJM.eReadName(handle, name, ref value);

                Console.WriteLine("\n" + name + " reading : " + value.ToString("F4") + " V");
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
