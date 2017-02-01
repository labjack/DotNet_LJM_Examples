//-----------------------------------------------------------------------------
// StreamSequentialAIN.cs
//
// Demonstrates how to stream a range of sequential analog inputs using the
// eStream functions. Useful when streaming many analog inputs. AIN channel
// scan list is FIRST_AIN_CHANNEL to FIRST_AIN_CHANNEL + NUMBER_OF_AINS - 1.
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;


namespace StreamSequentialAIN
{
    class StreamSequentialAIN
    {
        static void Main(string[] args)
        {
            StreamSequentialAIN ss = new StreamSequentialAIN();
            ss.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            const int FIRST_AIN_CHANNEL = 0;  //0 = AIN0
            const int NUMBER_OF_AINS = 8;

            int handle = 0;
            int devType = 0;
            int conType = 0;
            int serNum = 0;
            int ipAddr = 0;
            int port = 0;
            int maxBytesPerMB = 0;
            string ipAddrStr = "";
            string []aNames;
            double []aValues;
            int errorAddress = -1;

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Note when streaming, negative channels and ranges can be
                //configured for individual analog inputs, but the stream has
                //only one settling time and resolution.
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //T4 configuration

                    //Configure the channels to analog input or digital I/O
                    //Update all digital I/O channels.
                    //b1 = Ignored. b0 = Affected.
                    double dioInhibit = (double)0x00000;  //b00000000000000000000
                    //Set AIN0-AIN3 and AIN FIRST_AIN_CHANNEL to
                    //FIRST_AIN_CHANNEL+NUMBER_OF_AINS-1 as analog inputs (b1),
                    //the rest as digital I/O (b0).
                    double dioAnalogEnable = ((int)(Math.Pow(2, NUMBER_OF_AINS) - 1) << FIRST_AIN_CHANNEL) | 0x0000F;
                    aNames = new string[] { "DIO_INHIBIT", "DIO_ANALOG_ENABLE" };
                    aValues = new double[] { dioInhibit, dioAnalogEnable };
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);

                    //Configure the analog input ranges.
                    double rangeAINHV = 10.0;  //HV channels range (AIN0-AIN3)
                    double rangeAINLV = 2.4;  //LV channels range (AIN4+)
                    aNames = new string[NUMBER_OF_AINS];
                    aValues = new double[NUMBER_OF_AINS];
                    for(int i = 0; i < NUMBER_OF_AINS; i++)
                    {
                        aNames[i] = "AIN" + (FIRST_AIN_CHANNEL + i) + "_RANGE";
                        aValues[i] = ((FIRST_AIN_CHANNEL + i) < 4) ? rangeAINHV : rangeAINLV;
                    }
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);

                    //Configure the analog input negative channels, stream
                    //settling times and stream settling time.
                    aNames = new string[] {
                        "AIN_ALL_NEGATIVE_CH",
                        "STREAM_SETTLING_US",
                        "STREAM_RESOLUTION_INDEX"
                    };
                    aValues = new double[] {
                        LJM.CONSTANTS.GND,
                        10.0,
                        0
                    };  //single-ended, 0 (default), 0 (default)
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                }
                else
                {
                    //T7 and other devices configuration

                    //Configure the analog input negative channels, ranges,
                    //stream settling times and stream resolution index.
                    aNames = new string[] {
                        "AIN_ALL_NEGATIVE_CH",
                        "AIN_ALL_RANGE",
                        "STREAM_SETTLING_US",
                        "STREAM_RESOLUTION_INDEX"
                    };
                    aValues = new double[] {
                        LJM.CONSTANTS.GND,
                        10.0,
                        0,
                        0
                    };  //single-ended, +/-10V, 0 (default), 0 (default)
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                }

                //Stream Configuration
                int scansPerRead = 1000;  //# scans returned by eStreamRead call
                //Scan list names to stream. AIN(FIRST_AIN_CHANNEL) to
                //AIN(NUMBER_OF_AINS-1).
                string[] aScanListNames = new string[NUMBER_OF_AINS];
                for (int i = 0; i < aScanListNames.Length; i++)
                    aScanListNames[i] = "AIN" + (FIRST_AIN_CHANNEL + i).ToString();
                const int numAddresses = NUMBER_OF_AINS;
                int[] aTypes = new int[numAddresses];  //Dummy
                int[] aScanList = new int[numAddresses];  //Scan list addresses to stream.
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);
                double scanRate = 1000;  //Scans per second

                try
                {
                    Console.WriteLine("\nStarting stream. Press a key to stop streaming.");
                    System.Threading.Thread.Sleep(1000);  //Delay so users can read message

                    //Configure and start Stream
                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);

                    UInt64 loop = 0;
                    UInt64 totScans = 0;
                    //# of samples per eStreamRead is
                    //scansPerRead * numAddresses.
                    double[] aData = new double[scansPerRead * numAddresses];
                    UInt64 skippedTotal = 0;
                    int skippedCur = 0;
                    int deviceScanBacklog = 0;
                    int ljmScanBacklog = 0;
                    Stopwatch sw = new Stopwatch();

                    Console.WriteLine("Starting read loop.");
                    sw.Start();
                    while (!Console.KeyAvailable)
                    {
                        LJM.eStreamRead(handle, aData, ref deviceScanBacklog, ref ljmScanBacklog);
                        totScans += (UInt64)scansPerRead;

                        //Count the skipped samples which are indicated by -9999
                        //values. Missed samples occur after a device's stream
                        //buffer overflows and are reported after auto-recover
                        //mode ends.
                        skippedCur = 0;
                        foreach (double d in aData)
                        {
                            if (d == -9999.00)
                                skippedCur++;
                        }
                        skippedTotal += (UInt64)skippedCur;
                        loop++;
                        Console.WriteLine("\neStreamRead " + loop);
                        Console.WriteLine("  1st scan out of " + scansPerRead + ":");
                        for (int j = 0; j < numAddresses; j++)
                        {
                            Console.WriteLine("    " + aScanListNames[j] +
                                " = " + aData[j].ToString("F4"));
                        }
                        Console.WriteLine("  numSkippedScans: " +
                            (skippedCur / numAddresses) + 
                            ", deviceScanBacklog: " +
                            deviceScanBacklog +
                            ", ljmScanBacklog: " +
                            ljmScanBacklog);
                    }
                    sw.Stop();

                    //Doing this to prevent Enter key from closing the program
                    //right away.
                    Console.ReadKey(true);

                    Console.WriteLine("\nTotal scans: " + totScans);
                    Console.WriteLine("Skipped scans: " +
                        (skippedTotal / numAddresses));
                    double time = sw.ElapsedMilliseconds / 1000.0;
                    Console.WriteLine("Time taken: " + time + " seconds");
                    Console.WriteLine("LJM Scan Rate: " +
                        scanRate + " scans/second");
                    Console.WriteLine("Timed Scan Rate: " +
                        (totScans / time).ToString("F2") + " scans/second");
                    Console.WriteLine("Sample Rate: " + 
                        (totScans * numAddresses / time).ToString("F2") +
                        " samples/second");
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
