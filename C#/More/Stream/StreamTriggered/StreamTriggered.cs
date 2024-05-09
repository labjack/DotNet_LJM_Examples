//-----------------------------------------------------------------------------
// StreamTriggered.cs
//
// Demonstrates triggered stream on DIO0 / FIO0.
// Note: The T4 is not supported for this example.
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
//     eWriteNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritenames
//     Single Value Functions (such as eWriteName):
//         https://labjack.com/support/software/api/ljm/function-reference/single-value-functions
//     Library Configuration Functions (such as WriteLibraryConfigS):
//         https://labjack.com/support/software/api/ljm/function-reference/library-configuration-functions
//     Stream Functions (such as eStreamRead, eStreamStart and eStreamStop):
//         https://labjack.com/support/software/api/ljm/function-reference/stream-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Special Stream Modes (such as triggered): 
//         https://support.labjack.com/docs/3-2-2-special-stream-modes-t-series-datasheet
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
//     Extended DIO Features:
//         https://labjack.com/support/datasheets/t-series/digital-io/extended-features
//     Pulse Width In:
//         https://labjack.com/support/datasheets/t-series/digital-io/extended-features/pulse-width
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace StreamTriggered
{
    class StreamTriggered
    {
        static void Main(string[] args)
        {
            StreamTriggered st = new StreamTriggered();
            st.performActions();
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
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                if (devType == LJM.CONSTANTS.dtT4)
                {
                    throw new Exception("The T4 does not support triggered stream.");
                }

                try
                {
                    // Settling and negative channel do not apply to the T8
                    if (devType == LJM.CONSTANTS.dtT7)
                    {
                        //All negative channels are single-ended
                        //Stream settling is 0 (default).
                        aNames = new string[] {
                            "AIN_ALL_NEGATIVE_CH",
                            "STREAM_SETTLING_US"
                        };
                        aValues = new double[] {
                            LJM.CONSTANTS.GND,
                            0
                        };
                        LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                    }
                    //Ensure internally-clocked stream.
                    //AIN ranges are set to ±10V (T7) or ±11V (T8).
                    //Stream resolution index is 0 (default).
                    aNames = new string[] {
                        "STREAM_CLOCK_SOURCE",
                        "AIN_ALL_RANGE",
                        "STREAM_RESOLUTION_INDEX"
                    };
                    aValues = new double[] {
                        0,
                        10.0,
                        0
                    };
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);

                    TriggerStream(handle);
                }
                catch (LJM.LJMException e)
                {
                    showErrorMessage(e);
                }
                Console.WriteLine("Stop Stream");
                LJM.eStreamStop(handle);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }
            catch (Exception excp)
            {
                Console.WriteLine(excp.ToString());
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }

        public void TriggerStream(int handle)
        {
            //Stream Configuration
            const int NUM_LOOP_ITERATIONS = 10;
            double scanRate = 1000;  //Scans per second
            int scansPerRead = (int)scanRate/2;  //# scans returned by eStreamRead call
            const int numAddresses = 4;
            string[] aScanListNames = new String[] { "AIN0",  "FIO_STATE",  "SYSTEM_TIMER_20HZ", "STREAM_DATA_CAPTURE_16" };  //Scan list names to stream.
            int[] aTypes = new int[numAddresses];  //Dummy
            int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
            LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);

            //Configure LJM for unpredictable stream timing
            LJM.WriteLibraryConfigS("LJM_STREAM_SCANS_RETURN", LJM.CONSTANTS.STREAM_SCANS_RETURN_ALL_OR_NONE);
            LJM.WriteLibraryConfigS("LJM_STREAM_RECEIVE_TIMEOUT_MS", 0);

            //2000 sets DIO0 / FIO0 as the stream trigger
            LJM.eWriteName(handle, "STREAM_TRIGGER_INDEX", 2000);

            //Clear any previous DIO0_EF settings
            LJM.eWriteName(handle, "DIO0_EF_ENABLE", 0);

            //5 enables a rising or falling edge to trigger stream
            LJM.eWriteName(handle, "DIO0_EF_INDEX", 5);

            //Enable DIO0_EF
            LJM.eWriteName(handle, "DIO0_EF_ENABLE", 1);

            //Configure and start stream
            LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);

            UInt64 loop = 0;
            UInt64 totScans = 0;
            double[] aData = new double[scansPerRead*numAddresses];  //# of samples per eStreamRead is scansPerRead * numAddresses
            UInt64 skippedTotal = 0;
            int skippedCur = 0;
            int deviceScanBacklog = 0;
            int ljmScanBacklog = 0;
            Stopwatch sw = new Stopwatch();

            Console.WriteLine("You can trigger stream now via a rising or falling edge on DIO0 / FIO0.");
            sw.Start();
            while(loop < NUM_LOOP_ITERATIONS)
            {
                try
                {
                    VariableStreamSleep(scansPerRead, scanRate, ljmScanBacklog);
                    LJM.eStreamRead(handle, aData, ref deviceScanBacklog, ref ljmScanBacklog);
                    totScans += (UInt64)scansPerRead;

                    //Count the skipped samples which are indicated by -9999 values. Missed
                    //samples occur after a device's stream buffer overflows and are reported
                    //after auto-recover mode ends.
                    skippedCur = 0;
                    foreach (double d in aData)
                    {
                        if (d == -9999.00)
                            skippedCur++;
                    }
                    skippedTotal += (UInt64)skippedCur;
                    loop++;
                    Console.WriteLine("\neStreamRead " + loop);
                    Console.Write("  First scan out of " + scansPerRead + ": ");
                    for (int j = 0; j < numAddresses; j++)
                        Console.Write(aScanListNames[j] + " = " + aData[j].ToString("F4") + ", ");
                    Console.WriteLine("\n  numSkippedScans: " + (skippedCur / numAddresses) + ", deviceScanBacklog: " +
                        deviceScanBacklog + ", ljmScanBacklog: " + ljmScanBacklog);
                }
                catch (LJM.LJMException e)
                {
                    if (e.LJMError == LJM.LJMERROR.NO_SCANS_RETURNED)
                    {
                        Console.Write(".");
                        Console.Out.Flush();
                    }
                }
            }
            sw.Stop();

            Console.WriteLine("\nTotal scans: " + totScans);
            Console.WriteLine("Skipped scans: " + (skippedTotal / numAddresses));
            double time = sw.ElapsedMilliseconds / 1000.0;
            Console.WriteLine("Time taken: " + time + " seconds");
            Console.WriteLine("LJM Scan Rate: " + scanRate + " scans/second");
        }

        private void VariableStreamSleep(int scansPerRead, double scanRate, int LJMScanBacklog)
        {
            const double DECREASE_TOTAL = 0.9;
            double sleepFactor = 0;
            double portionScansReady = (double)LJMScanBacklog / scansPerRead;
            if (portionScansReady <= DECREASE_TOTAL)
            {
                sleepFactor = (1 - portionScansReady) * DECREASE_TOTAL;
            }
            int sleepMS = (int)(sleepFactor * 1000 * scansPerRead / scanRate);
            if (sleepMS < 1) {
                return;
            }
            System.Threading.Thread.Sleep(sleepMS);
        }
    }
}
