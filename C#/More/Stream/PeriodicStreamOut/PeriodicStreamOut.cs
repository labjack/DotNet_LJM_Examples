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

                double scanRate = 1000;  //Scans per second
                int scansPerRead = (int)(scanRate / 2);
                int runTimeMS = 5000;  //Number of seconds to stream out waveforms

                //The desired stream channels. Up to 4 out-streams can be ran at once.
                const int numAddresses = 1;
                string[] aScanListNames = new String[] { "STREAM_OUT0" };  //Scan list names to stream.
                int[] aScanList = new int[numAddresses];
                int[] aTypes = new int[numAddresses];  //Dummy
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);

                int targetAddr = 1000;  //Stream out to DAC0.

                //Stream out index can be a value of 0 to 3.
                int streamOutIndex = 0;

                //Make an arbitrary waveform that increases voltage linearly
                //from 0 to 2.5 V.
                const int samplesToWrite = 512;
                double[] writeData = new double[samplesToWrite];
                double increment = 1.0 / samplesToWrite;
                double sample = 0;
                for (int i = 0; i < samplesToWrite; i++) {
                    sample = 2.5*increment*i;
                    writeData[i] = sample;
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
                        writeData
                    );

                    Console.WriteLine("\nBeginning stream out...");
                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);
                    Console.WriteLine("Stream started with scan rate of " + scanRate + " Hz.");
                    Console.WriteLine("  Running for " + (runTimeMS/1000.0) + " seconds.\n");
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
