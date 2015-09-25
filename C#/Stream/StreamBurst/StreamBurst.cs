﻿//-----------------------------------------------------------------------------
// StreamBurst.cs
//
// Demonstrates how to use the StreamBurst function for streaming.
//
// support@labjack.com
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
                uint numScans = 10000; //Number of scans to perform
                string[] aScanListNames = new String[] { "AIN0", "AIN1" }; //Scan list names to stream.
                int numAddresses = aScanListNames.Length;
                int[] aTypes = new int[numAddresses]; //Dummy
                int[] aScanList = new int[numAddresses]; //Scan list addresses to stream. StreamBurst uses Modbus addresses.
                LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);
                double scanRate = 5000; //Scans per second
                double[] aData = new double[numScans * numAddresses];

                try
                {
                    //Configure the analog inputs' negative channel, range, settling time and
                    //resolution.
                    //Note when streaming, negative channels and ranges can be configured for
                    //individual analog inputs, but the stream has only one settling time and
                    //resolution.
                    string[] aNames = new string[] { "AIN_ALL_NEGATIVE_CH", "AIN_ALL_RANGE", "STREAM_SETTLING_US", "STREAM_RESOLUTION_INDEX" };
                    double[] aValues = new double[] { LJM.CONSTANTS.GND, 10.0, 0, 0 };  //single-ended, +/-10V, 0 (default), 0 (default)
                    int errorAddress = 0;
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);

                    Console.WriteLine("\nScan list:");
                    for(int i = 0; i < numAddresses; i++)
                        Console.WriteLine("  " + aScanListNames[i]);
                    Console.WriteLine("Scan rate = " + scanRate + " Hz");
                    Console.WriteLine("Sample rate = " + (scanRate * numAddresses) + " Hz");
                    Console.WriteLine("Number of scans = " + numScans);
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
                        Console.WriteLine("  " + aScanListNames[i] + " = " + aData[(numScans-1)*numAddresses + i]);
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

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }
    }
}
