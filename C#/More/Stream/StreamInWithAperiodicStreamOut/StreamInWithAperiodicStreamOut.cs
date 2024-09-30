//-----------------------------------------------------------------------------
// StreamInWithAperiodicStreamOut.cs
//
// Demonstrates usage of aperiodic stream-out functions with stream-in.
// Streams in while streaming out arbitrary values. These arbitrary stream-out
// values act on DAC0 to cyclically increase the voltage from 0 to 2.5.
// Though these values are generated before the stream starts, the values could
// be dynamically generated, read from a file, etc.
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
//     Stream Functions (InitializeAperiodicStreamOut, WriteAperiodicStreamOut,
//     eStreamStart, eStreamRead and eStreamStop):
//         https://labjack.com/support/software/api/ljm/function-reference/stream-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Stream-Out:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode/stream-out/stream-out-description
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//     DAC:
//         https://labjack.com/support/datasheets/t-series/dac
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace StreamInWithAperiodicStreamOut
{
    class StreamInWithAperiodicStreamOut
    {
        static void Main(string[] args)
        {
            StreamInWithAperiodicStreamOut siwaso = new StreamInWithAperiodicStreamOut();
            siwaso.performActions();
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
            int ljmBufferStatus = 0;
            UInt64 skippedScansTotal = 0;

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

                //Desired number of write cycles (periods of waveform to output).
                const int NUM_WRITES = 10;

                const int numAddressesIn = 1;
                const int numAddressesOut = 1;
                const int numAddresses = numAddressesIn + numAddressesOut;
                string[] aScanListNames = new String[] { "AIN0", "STREAM_OUT0" };  //Scan list names to stream.
                int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                int[] aTypes = new int[numAddresses];  //Dummy
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);

                int targetAddr = 1000;  // DAC0

                //With current T-series devices, 4 stream-outs can be ran
                //concurrently. Stream-out index should therefore be a value 0-3.
                int streamOutIndex = 0;

                //Make an arbitrary waveform that increases voltage linearly
                //from 0-2.5V.
                const int samplesToWrite = 512;
                double[] values = new double[samplesToWrite];
                double increment = 1.0 / samplesToWrite;
                for (int i = 0; i < samplesToWrite; i++)
                {
                    double sample = 2.5*increment*i;
                    values[i] = sample;
                }

                try
                {
                    Console.WriteLine("\nInitializing stream out buffer...");
                    LJM.InitializeAperiodicStreamOut(handle, streamOutIndex, targetAddr, scanRate);

                    //Write some data to the buffer before the stream starts.
                    const int PRE_STREAM_WRITES = 2;
                    for (int i = 0; i < PRE_STREAM_WRITES; i++)
                    {
                        LJM.WriteAperiodicStreamOut(handle, streamOutIndex, samplesToWrite, values, ref ljmBufferStatus);
                    }

                    double[] aData = new double[scansPerRead*numAddressesIn];  //# of samples per eStreamRead is scansPerRead * numAddressesIn
                    int skippedScansCur = 0;
                    int deviceScanBacklog = 0;
                    int ljmScanBacklog = 0;
                    string str = "";

                    Console.WriteLine("\nscanList: {0}", string.Join(", ", aScanList));
                    Console.WriteLine("scansPerRead: {0}", scansPerRead);

                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    Console.WriteLine("\nStream started with scan rate of {0} Hz", scanRate);
                    Console.WriteLine("\nPerforming {0} buffer updates", NUM_WRITES-PRE_STREAM_WRITES);

                    for (int i = 0; i < NUM_WRITES - PRE_STREAM_WRITES; i++)
                    {
                        LJM.WriteAperiodicStreamOut(handle, streamOutIndex, samplesToWrite, values, ref ljmBufferStatus);

                        LJM.eStreamRead(handle, aData, ref deviceScanBacklog, ref ljmScanBacklog);

                        //Count the skipped samples which are indicated by -9999
                        //values. Missed samples occur after a device's stream
                        //buffer overflows and are reported after auto-recover
                        //mode ends.
                        skippedScansCur = 0;
                        foreach(double d in aData)
                        {
                            if(d == -9999.00)
                                skippedScansCur++;
                        }
                        skippedScansTotal += (UInt64)skippedScansCur;

                        Console.WriteLine("\neStreamRead {0}", i);

                        str = "";
                        for (int j = 0; j < numAddressesIn; j++)
                        {
                            str += aScanListNames[j] + " = " + aData[j].ToString("F4") + ", ";
                        }
                        if (str != "")
                        {
                            Console.WriteLine("  1st scan out of {0}: {1}", scansPerRead, str);
                        }

                        if (skippedScansCur > 0)
                        {
                            Console.WriteLine(
                                "  **** Samples skipped = {0} (of {1}) ****",
                                skippedScansCur,
                                aData.Length);
                        }

                        str = "";
                        if (deviceScanBacklog > 0)
                        {
                            str += "Device scan backlog = " + deviceScanBacklog + ", ";
                        }
                        if (ljmScanBacklog > 0)
                        {
                            str += "LJM scan backlog = " + ljmScanBacklog;
                        }
                        if (str != "")
                        {
                            Console.WriteLine("  {0}", str);
                        }
                    }

                    sw.Stop();

                    //Since scan rate determines how quickly data can be
                    //written from the device, large chunks of data written at
                    //low scan rates can take longer to write out than it takes
                    //to call LJM_WriteAperiodicStreamOut and LJM_eStreamRead.
                    //Some delay may be necessary if it is desired to write out
                    //all data then immediately close the stream.
                    long runTime = sw.ElapsedMilliseconds;
                    Console.WriteLine("\nLooped for {0} milliseconds", runTime);

                    //512 samples * 10 writes = 5120 samples.
                    //Scan rate = 1000 samples/sec, so it should take 5.12
                    //seconds to write all data out.
                    long streamOutMS = (long)(1000 * samplesToWrite * (NUM_WRITES) / scanRate);
                    if (runTime < streamOutMS)
                    {
                        Console.WriteLine("Waiting an extra {0} milliseconds to output data", streamOutMS - runTime);
                        System.Threading.Thread.Sleep((int)(streamOutMS - runTime));
                    }
                }
                catch (LJM.LJMException e)
                {
                    showErrorMessage(e);
                }

                Console.WriteLine("\nStopping Stream");
                LJM.eStreamStop(handle);

                Console.WriteLine("Total number of skipped scans: {0}", skippedScansTotal);
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
