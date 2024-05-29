//-----------------------------------------------------------------------------
// PeriodicStreamOut.cs
//
// Demonstrates usage of the periodic stream-out function.
//
// Streams out arbitrary values. These arbitrary stream-out values act on DAC0
// to cyclically increase the voltage from 0 to 2.5 V.
//
// Note: This example requires LJM 1.21 or higher.
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
//     NamesToAddresses:
//         https://labjack.com/support/software/api/ljm/function-reference/utility/ljmnamestoaddresses
//     LJM Single Value Functions (such as eReadName, eReadAddress):
//         https://labjack.com/support/software/api/ljm/function-reference/single-value-functions
//     Stream Functions (such as eStreamStart, eStreamStop and PeriodicStreamOut):
//         https://labjack.com/support/software/api/ljm/function-reference/stream-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Stream-Out:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode/stream-out
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
//     DAC:
//         https://labjack.com/support/datasheets/t-series/dac
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace PeriodicStreamOut
{
    class PeriodicStreamOut
    {
        static void Main(string[] args)
        {
            PeriodicStreamOut pso = new PeriodicStreamOut();
            pso.performActions();
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
            string[] aNames;
            double[] aValues;
            int errorAddress = -1;

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

                //Scans per second
                double scanRate = 1000;
                int scansPerRead = (int)(scanRate / 2);
                // Desired duration to run the stream out
                int runTimeMS = 5000;
                const int numAddresses = 1;
                string[] aScanListNames = new String[] { "STREAM_OUT0" };  //Scan list names to stream.
                int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                int[] aTypes = new int[numAddresses];  //Dummy
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);
                int targetAddr = 1000; // DAC0
                // With current T-series devices, 4 stream-outs can be ran concurrently.
                // Stream-out index should therefore be a value 0-3.
                int streamOutIndex = 0;
                const int samplesToWrite = 512;
                // Make an arbitrary waveform that increases voltage linearly from 0-2.5V
                double[] values = new double[samplesToWrite];
                double increment = 1.0 / samplesToWrite;
                for (int i = 0; i < samplesToWrite; i++) {
                    double sample = 2.5*increment*i;
                    values[i] = sample;
                }

                try
                {
                    Console.WriteLine("\nInitializing stream out...");
                    LJM.PeriodicStreamOut(
                        handle,
                        streamOutIndex,
                        targetAddr,
                        scanRate,
                        samplesToWrite,
                        values
                    );

                    Console.WriteLine("\nBeginning stream out...");
                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);
                    System.Threading.Thread.Sleep(runTimeMS);  // Delay for the desired runtime
                }
                catch (LJM.LJMException e)
                {
                    showErrorMessage(e);
                }
                Console.WriteLine("Stopping Stream...");
                LJM.eStreamStop(handle);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles
            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
