//-----------------------------------------------------------------------------
// SimpleStream.cs
//
// Demonstrates how to stream using the eStream functions.
//
// support@labjack.com
// Sept 18, 2013
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using LabJack;

namespace SimpleStream
{
    class SimpleStream
    {
        static void Main(string[] args)
        {
            SimpleStream es = new SimpleStream();
            es.performActions();
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

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);
                
                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);

                //Stream Configuration
                int scansPerRead = 1000; //# scans returned by eStreamRead call
                const int numAddresses = 2;
                string[] aScanListNames = new String[numAddresses] { "AIN0", "AIN1" }; //Scan list names to stream.
                int[] aTypes = new int[numAddresses]; //Dummy
                int[] aScanList = new int[numAddresses]; //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);
                double scanRate = 1000; //Scans per second

                //Configure the negative channels for single ended readings
                string[] aNames = new string[numAddresses] { aScanListNames[0]+"_NEGATIVE_CH", aScanListNames[1]+"_NEGATIVE_CH" };
                double[] aValues = new double[numAddresses] { LJM.CONSTANTS.GND, LJM.CONSTANTS.GND };
                int errorAddress = 0;
                LJM.eWriteNames(handle, numAddresses, aNames, aValues, ref errorAddress);

                try
                {
                    Console.WriteLine("\nStarting stream. Press a key to stop streaming.");
                    System.Threading.Thread.Sleep(1000); //Delay so user's can read message

                    //Configure and start Stream
                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);
                    
                    UInt64 loop = 0;
                    UInt64 totScans = 0;
                    double[] aData = new double[scansPerRead*numAddresses]; //# of samples per eStreamRead is scansPerRead * numAddresses
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
                        Console.WriteLine("\n  numSkippedScans: " + skippedCur/numAddresses + ", deviceScanBacklog: " + deviceScanBacklog + ", ljmScanBacklog: " + ljmScanBacklog);
                    }
                    sw.Stop();

                    Console.ReadKey(true); //Doing this to prevent Enter key from closing the program right away.

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

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }
    }
}
