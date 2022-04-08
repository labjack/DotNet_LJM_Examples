//-----------------------------------------------------------------------------
// StreamInWithAperiodicStreamOut.cs
//
// Demonstrates usage of aperiodic stream-out functions with stream-in.
// Streams in while streaming out arbitrary values. These arbitrary stream-out
// values act on DAC0 to cyclically increase the voltage from 0 to 2.5.
// Though these values are generated before the stream starts, the values could
// be dynamically generated, read from a file, etc.
//
// Note: This example requires LJM 1.21 or higher
//
// support@labjack.com
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
            int queueVals = 0;

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
                //Desired number of write cycles (periods of waveform to output)
                const int NUM_WRITES = 10;
                const int numAddresses = 2;
                string[] aScanListNames = new String[] { "AIN0", "STREAM_OUT0" };  //Scan list names to stream.
                int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                int[] aTypes = new int[numAddresses];  //Dummy
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);
                int targetAddr = 1000; // DAC0
                //With current T-series devices, 4 stream-outs can be ran concurrently.
                //Stream-out index should therefore be a value 0-3.
                int streamOutIndex = 0;
                const int samplesToWrite = 512;
                //Make an arbitrary waveform that increases voltage linearly from 0-2.5V
                double[] values = new double[samplesToWrite];
                double increment = 1.0 / samplesToWrite;
                for (int i = 0; i < samplesToWrite; i++) {
                    double sample = 2.5*increment*i;
                    values[i] = sample;
                }

                try
                {
                    Console.WriteLine("\nInitializing stream out buffer...");
                    LJM.InitializeAperiodicStreamOut(
                        handle,
                        streamOutIndex,
                        targetAddr,
                        scanRate
                    );

                    const int PRE_STREAM_WRITES = 2;
                    //If possible, write some data to the buffer before the
                    //stream starts
                    for (int i = 0; i < PRE_STREAM_WRITES; i++)
                    {
                        LJM.WriteAperiodicStreamOut(
                            handle,
                            streamOutIndex,
                            samplesToWrite,
                            values,
                            ref queueVals
                        );
                    }
                    double[] aData = new double[scansPerRead*numAddresses];  //# of samples per eStreamRead is scansPerRead * numAddresses
                    int deviceScanBacklog = 0;
                    int ljmScanBacklog = 0;
                    Stopwatch sw = new Stopwatch();

                    sw.Start();
                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);
                    Console.WriteLine("Stream started with scan rate of {0}Hz\n", scanRate);
                    Console.WriteLine("Performing {0} buffer updates", NUM_WRITES-PRE_STREAM_WRITES);
                    for (int i = 0; i < NUM_WRITES-PRE_STREAM_WRITES; i++)
                    {
                        LJM.WriteAperiodicStreamOut(
                            handle,
                            streamOutIndex,
                            samplesToWrite,
                            values,
                            ref queueVals
                        );
                        LJM.eStreamRead(
                            handle,
                            aData,
                            ref deviceScanBacklog,
                            ref ljmScanBacklog
                        );
                        Console.WriteLine(
                            "iteration: {0} - deviceScanBacklog: {1}, LJMScanBacklog: {2}",
                            i,
                            deviceScanBacklog,
                            ljmScanBacklog
                        );
                    }
                    sw.Stop();
                    //Since scan rate determines how quickly data can be
                    //written from the device, large chunks of data written at
                    //low scan rates can take longer to write out than it takes
                    //to call LJM_WriteAperiodicStreamOut and LJM_eStreamRead.
                    //some delay may be necessary if it is desired to write out
                    // all data then immediately close the stream
                    long runTime = sw.ElapsedMilliseconds;
                    Console.WriteLine("Looped for {0} milliseconds", runTime);
                    // 512 samples * 10 writes = 5120 samples. scan rate = 1000
                    // samples/sec, so it should take 5.12 seconds to write all data out
                    long streamOutMS = (long)(1000 * samplesToWrite * (NUM_WRITES) / scanRate);
                    if (runTime < streamOutMS) {
                        Console.WriteLine("Waiting an extra {0} milliseconds to output data", streamOutMS - runTime);
                        System.Threading.Thread.Sleep((int)(streamOutMS - runTime));
                    }
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
