//-----------------------------------------------------------------------------
// StreamBurst.cs
//
// Demonstrates how to use the StreamBurst function for streaming.
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
//     StreamBurst:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmstreamburst
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Special Stream Modes (such as burst):
//         https://support.labjack.com/docs/3-2-2-special-stream-modes-t-series-datasheet
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace StreamBurst
{
    class StreamBurst
    {
        static void Main(string[] args)
        {
            StreamBurst sb = new StreamBurst();
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
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
                uint numScans = 2000;  //Number of scans to perform
                string[] aScanListNames = new String[] { "AIN0", "AIN1" };  //Scan list names to stream.
                int numAddresses = aScanListNames.Length;
                int[] aTypes = new int[numAddresses];  //Dummy
                int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. StreamBurst uses Modbus addresses.
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);
                double scanRate = 2000;  //Scans per second
                double[] aData = new double[numScans * numAddresses];

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

                    Console.WriteLine("\nScan list:");
                    for(int i = 0; i < numAddresses; i++)
                        Console.WriteLine("  " + aScanListNames[i]);
                    Console.WriteLine("Scan rate = " + scanRate + " Hz");
                    Console.WriteLine("Sample rate = " + (scanRate * numAddresses) + " Hz");
                    Console.WriteLine("Total number of scans = " + numScans);
                    Console.WriteLine("Total number of samples = " + (numScans * numAddresses));
                    Console.WriteLine("Seconds of samples = " + (numScans / scanRate) + " seconds");

                    Console.Write("\nStreaming with StreamBurst...");
                    
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    
                    //Stream data using StreamBurst
                    LJM.StreamBurst(handle, numAddresses, aScanList, ref scanRate, numScans, aData);
                    
                    sw.Stop();
                    
                    Console.WriteLine(" Done");

                    //Count the skipped samples which are indicated by -9999 values. Missed
                    //samples occur after a device's stream buffer overflows and are reported
                    //after auto-recover mode ends.
                    ulong skippedTotal = 0;
                    foreach(double d in aData)
                    {
                        if (d == -9999.00)
                            skippedTotal++;
                    }

                    Console.WriteLine("\nSkipped scans = " + (skippedTotal / (ulong)numAddresses));
                    double time = sw.ElapsedMilliseconds / 1000.0;
                    Console.WriteLine("Time taken = " + time + " seconds");

                    Console.WriteLine("\nLast scan:");
                    for (int i = 0; i < numAddresses; i++)
                        Console.WriteLine("  " + aScanListNames[i] + " = " + aData[(numScans - 1) * numAddresses + i]);
                }
                catch (LJM.LJMException e)
                {
                    showErrorMessage(e);
                }
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
