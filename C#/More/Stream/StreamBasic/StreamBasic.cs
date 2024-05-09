//-----------------------------------------------------------------------------
// StreamBasic.cs
//
// Demonstrates how to stream using the eStream functions.
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
//     Constants:
//         https://labjack.com/support/software/api/ljm/constants
//     NamesToAddresses:
//         https://labjack.com/support/software/api/ljm/function-reference/utility/ljmnamestoaddresses
//     eWriteNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritenames
//     Stream Functions (such as eStreamStart, eStreamRead and eStreamStop):
//         https://labjack.com/support/software/api/ljm/function-reference/stream-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace StreamBasic
{
    class StreamBasic
    {
        static void Main(string[] args)
        {
            StreamBasic sb = new StreamBasic();
            sb.performActions();
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

                //Stream Configuration
                double scanRate = 1000;  //Scans per second
                int scansPerRead = (int)scanRate/2;  //# scans returned by eStreamRead call
                const int numAddresses = 2;
                string[] aScanListNames = new String[] { "AIN0", "AIN1" };  //Scan list names to stream.
                int[] aTypes = new int[numAddresses];  //Dummy
                int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);

                try
                {
                    //When streaming, negative channels and ranges can be configured for
                    //individual analog inputs, but the stream has only one settling time and
                    //resolution.

                    if (devType == LJM.CONSTANTS.dtT4)
                    {
                        //LabJack T4 configuration

                        //Stream settling is 0 (default) and stream resolution index is 0 (default).
                        aNames = new string[] {
                            "STREAM_SETTLING_US",
                            "STREAM_RESOLUTION_INDEX"
                        };
                        aValues = new double[] {
                            0,
                            0
                        };
                        LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                    }
                    else
                    {
                        //LabJack T7 and T8 configuration

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

                        //Ensure triggered stream is disabled.
                        //Ensure internally-clocked stream.
                        //AIN0 and AIN1 ranges are set to ±10V (T7) or ±11V (T8).
                        //Stream resolution index is 0 (default).
                        aNames = new string[] {
                            "STREAM_TRIGGER_INDEX",
                            "STREAM_CLOCK_SOURCE",
                            "AIN0_RANGE",
                            "AIN1_RANGE",
                            "STREAM_RESOLUTION_INDEX"
                        };
                        aValues = new double[] {
                            0,
                            0,
                            10.0,
                            10.0,
                            0
                        };
                        LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                    }

                    Console.WriteLine("\nStarting stream. Press a key to stop streaming.");
                    System.Threading.Thread.Sleep(1000);  //Delay so user's can read message

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

                    Console.WriteLine("Starting read loop.");
                    sw.Start();
                    while(!Console.KeyAvailable)
                    {
                        LJM.eStreamRead(handle, aData, ref deviceScanBacklog, ref ljmScanBacklog);
                        totScans += (UInt64)scansPerRead;

                        //Count the skipped samples which are indicated by -9999 values. Missed
                        //samples occur after a device's stream buffer overflows and are reported
                        //after auto-recover mode ends.
                        skippedCur = 0;
                        foreach(double d in aData)
                        {
                            if(d == -9999.00)
                                skippedCur++;
                        }
                        skippedTotal += (UInt64)skippedCur;
                        loop++;
                        Console.WriteLine("\neStreamRead " + loop);
                        Console.Write("  First scan out of " + scansPerRead + ": ");
                        for(int j = 0; j < numAddresses; j++)
                            Console.Write(aScanListNames[j] + " = " + aData[j].ToString("F4") + ", ");
                        Console.WriteLine("\n  numSkippedScans: " + (skippedCur / numAddresses) + ", deviceScanBacklog: " +
                            deviceScanBacklog + ", ljmScanBacklog: " + ljmScanBacklog);
                    }
                    sw.Stop();

                    Console.ReadKey(true);  //Doing this to prevent Enter key from closing the program right away.

                    Console.WriteLine("\nTotal scans: " + totScans);
                    Console.WriteLine("Skipped scans: " + (skippedTotal / numAddresses));
                    double time = sw.ElapsedMilliseconds / 1000.0;
                    Console.WriteLine("Time taken: " + time + " seconds");
                    Console.WriteLine("LJM Scan Rate: " + scanRate + " scans/second");
                    Console.WriteLine("Timed Scan Rate: " + (totScans / time).ToString("F2") + " scans/second");
                    Console.WriteLine("Sample Rate: " + (totScans * numAddresses / time).ToString("F2") + " samples/second");
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

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
