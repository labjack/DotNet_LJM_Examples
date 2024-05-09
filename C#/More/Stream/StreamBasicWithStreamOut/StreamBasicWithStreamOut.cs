//-----------------------------------------------------------------------------
// StreamBasicWithStreamOut.cs
//
// Demonstrates setting up stream-in and stream-out together, then reading
// stream-in values.
//
// Connect a wire from AIN0 to DAC0 to see the effect of stream-out on stream-in
// channel 0.
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
//     eWriteName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritename
//     eReadName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmereadname
//     eWriteNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritenames
//     Stream Functions (such as eStreamRead, eStreamStart and eStreamStop):
//         https://labjack.com/support/software/api/ljm/function-reference/stream-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Stream-Out:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode/stream-out
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//     DAC:
//         https://labjack.com/support/datasheets/t-series/dac
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace StreamBasicWithStreamOut
{
    class StreamBasicWithStreamOut
    {
        static void Main(string[] args)
        {
            StreamBasicWithStreamOut so = new StreamBasicWithStreamOut();
            so.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            const int MAX_REQUESTS = 10;  //The number of eStreamRead calls that will be performed.

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


                //Setup Stream Out
                const int numAddressesOut = 1;
                String[] outNames = new String[numAddressesOut] {"DAC0"};
                int[] outAddresses = new int[numAddressesOut];
                int[] aTypes = new int[numAddressesOut];  //Dummy
                LJM.NamesToAddresses(numAddressesOut, outNames, outAddresses, aTypes);

                //Allocate memory for the stream-out buffer
                LJM.eWriteName(handle, "STREAM_OUT0_TARGET", outAddresses[0]);
                LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_SIZE", 512);
                LJM.eWriteName(handle, "STREAM_OUT0_ENABLE", 1);

                //Write values to the stream-out buffer.

                //0.0 to 5.0 V with 1.0 V increments. Repeating each voltage 8
                //times to slow the output frequency, so buffering 48 values.
                double []outVolts = {0.0, 1.0, 2.0, 3.0, 4.0, 5.0};
                double numVoltRepeats = 8;
                LJM.eWriteName(handle, "STREAM_OUT0_LOOP_SIZE", outVolts.Length*numVoltRepeats);
                foreach (double volt in outVolts)
                {
                    for (int i = 0; i < numVoltRepeats; i++)
                    {
                        LJM.eWriteName(handle, "STREAM_OUT0_BUFFER_F32", volt);
                    }
                }
                LJM.eWriteName(handle, "STREAM_OUT0_SET_LOOP", 1);

                double value = 0.0;
                LJM.eReadName(handle, "STREAM_OUT0_BUFFER_STATUS", ref value);
                Console.WriteLine("\nSTREAM_OUT0_BUFFER_STATUS = " + value);

                //Stream Configuration
                double scanRate = 500;  //Scans per second
                int scansPerRead = (int)scanRate/2;  //# scans returned by eStreamRead call

                const int numAddressesIn = 2;
                string[] aScanListNames = new String[numAddressesIn] { "AIN0", "AIN1" };  //Scan list names to stream.
                aTypes = new int[numAddressesIn];  //Dummy
                int[] aScanList = new int[numAddressesIn + numAddressesOut];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                LJM.NamesToAddresses(numAddressesIn, aScanListNames, aScanList, aTypes);

                //Add the scan list outputs to the end of the scan list.
                //STREAM_OUT0 = 4800, STREAM_OUT1 = 4801, ...
                aScanList[numAddressesIn] = 4800;  //STREAM_OUT0
                //If we had more STREAM_OUTs
                //aScanList[numAddressesIn+1] = 4801;  //STREAM_OUT1
                //etc.

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

                    Console.WriteLine("\nStarting stream.");

                    //Configure and start stream
                    LJM.eStreamStart(handle, scansPerRead, aScanList.Length, aScanList, ref scanRate);

                    UInt64 loop = 0;
                    UInt64 totScans = 0;
                    double[] aData = new double[scansPerRead * numAddressesIn];  //# of samples per eStreamRead is scansPerRead * numAddressesIn
                    UInt64 skippedTotal = 0;
                    int skippedCur = 0;
                    int deviceScanBacklog = 0;
                    int ljmScanBacklog = 0;
                    string str;
                    Stopwatch sw = new Stopwatch();

                    Console.WriteLine("Starting read loop.");
                    sw.Start();
                    for (int i = 0; i < MAX_REQUESTS; i++)
                    {
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
                        for (int j = 0; j < scansPerRead; j++)
                        {
                            str = "";
                            for(int k = 0; k < numAddressesIn; k++)
                                str += "  " + aScanListNames[k] + " = " + aData[j*numAddressesIn + k].ToString("F4") + ",";
                            Console.WriteLine(str);
                        }
                        Console.WriteLine("  Skipped Scans = " + skippedCur / numAddressesIn + 
                            ", Scan Backlogs: Device = " + deviceScanBacklog + ", LJM = " + ljmScanBacklog);
                    }
                    sw.Stop();

                    Console.WriteLine("\nTotal scans: " + totScans);
                    Console.WriteLine("Skipped scans: " + (skippedTotal / numAddressesIn));
                    double time = sw.ElapsedMilliseconds / 1000.0;
                    Console.WriteLine("Time taken: " + time + " seconds");
                    Console.WriteLine("LJM Scan Rate: " + scanRate + " scans/second");
                    Console.WriteLine("Timed Scan Rate: " + (totScans / time).ToString("F2") + " scans/second");
                    Console.WriteLine("Sample Rate: " + (totScans * numAddressesIn / time).ToString("F2") + " samples/second");
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
            Console.ReadLine();  // Pause for user
        }
    }
}
