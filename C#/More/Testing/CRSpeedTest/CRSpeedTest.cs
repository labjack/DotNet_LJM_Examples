//-----------------------------------------------------------------------------
// CRSpeedTest.cs
//
// Performs LabJack operations in a loop and reports the timing statistics for
// the operations.
//
// support@labjack.com
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using LabJack;


namespace CRSpeedTest
{
    class CRSpeedTest
    {
        static void Main(string[] args)
        {
            CRSpeedTest spTest = new CRSpeedTest();
            spTest.performActions();
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
            int i = 0;

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


                const int numIterations = 1000;  //Number of iterations to perform in the loop

                //Analog input settings
                const int numAIN = 1;  //Number of analog inputs to read
                const double rangeAIN = 10.0;  // T7/T8 AIN range
                const double resolutionAIN = 1.0;
                
                //Digital settings
                const bool readDigital = false;
                const bool writeDigital = false;

                //Analog output settings
                const bool writeDACs = false;

                //Use eAddresses (true) or eNames (false) in the operations
                //loop. eAddresses is faster than eNames.
                const bool useAddresses = true;

                //Variables for LJM library calls
                int numFrames = 0;
                string[] aNames;
                int[] aAddresses;
                int[] aTypes;
                int[] aWrites;
                int[] aNumValues;
                double[] aValues;
                int errAddr = 0;

                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //For the T4, configure the channels to analog input or
                    //digital I/O.

                    //Update all digital I/O channels.
                    //b1 = Ignored. b0 = Affected.
                    double dioInhibit = (double)0x00000;  //b00000000000000000000
                    //Set AIN 0 to numAIN-1 as analog inputs (b1), the rest as
                    //digital I/O (b0).
                    double dioAnalogEnable = Math.Pow(2, numAIN) - 1;
                    aNames = new string[2] { "DIO_INHIBIT", "DIO_ANALOG_ENABLE" };
                    aValues = new double[2] { dioInhibit, dioAnalogEnable };
                    LJM.eWriteNames(handle, 2, aNames, aValues, ref errAddr);
                    if(writeDigital)
                    {
                        //Update only digital I/O channels in future digital
                        //write calls. b1 = Ignored. b0 = Affected.
                        dioInhibit = dioAnalogEnable;
                        LJM.eWriteName(handle, "DIO_INHIBIT", dioInhibit);
                    }
                }

                if(numAIN > 0)
                {
                    //Configure analog input settings
                    numFrames = Math.Max(0, numAIN*2);
                    aNames = new string[numFrames];
                    aValues = new double[numFrames];
                    for(i = 0; i < numAIN; i++)
                    {
                        aNames[i*2] = "AIN" + i + "_RANGE";
                        if (devType != LJM.CONSTANTS.dtT4)
                        {
                            //T7/T8 range
                            aValues[i * 2] = rangeAIN;
                        }
                        aNames[i*2+1] = "AIN" + i + "_RESOLUTION_INDEX";
                        aValues[i*2+1] = resolutionAIN;
                    }
                    LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errAddr);
                }

                //Initialize and configure eNames parameters for loop's eNames
                //call
                numFrames = Math.Max(0, numAIN) + Convert.ToInt32(readDigital) +
                    Convert.ToInt32(writeDigital) + Convert.ToInt32(writeDACs)*2;
                aNames = new string[numFrames];
                aWrites = new int[numFrames];
                aNumValues = new int[numFrames];
                aValues = new double[numFrames];  //In this case numFrames is the size of aValue

                //Add analog input reads (AIN 0 to numAIN-1)
                for(i = 0; i < numAIN; i++)
                {
                    aNames[i] = "AIN" + i;
                    aWrites[i] = LJM.CONSTANTS.READ;
                    aNumValues[i] = 1;
                    aValues[i] = 0;
                }

                if(readDigital)
                {
                    //Add digital read
                    aNames[i] = "DIO_STATE";
                    aWrites[i] = LJM.CONSTANTS.READ;
                    aNumValues[i] = 1;
                    aValues[i] = 0;
                    i++;
                }

                if(writeDigital)
                {
                    //Add digital write
                    aNames[i] = "DIO_STATE";
                    aWrites[i] = LJM.CONSTANTS.WRITE;
                    aNumValues[i] = 1;
                    aValues[i] = 0; //output-low
                    i++;
                }

                if(writeDACs)
                {
                    //Add analog output writes (DAC0-1)
                    for(int j = 0; j < 2; j++, i++)
                    {
                        aNames[i] = "DAC" + j;
                        aWrites[i] = LJM.CONSTANTS.WRITE;
                        aNumValues[i] = 1;
                        aValues[i] = 0.0; //0.0 V
                    }
                }

                //Make arrays of addresses and data types for eAddresses.
                aAddresses = new int[numFrames];
                aTypes = new int[numFrames];
                LJM.NamesToAddresses(numFrames, aNames, aAddresses, aTypes);

                Console.WriteLine("\nTest frames:");

                string wrStr = "";
                for(i = 0; i < numFrames; i++)
                {
                    if(aWrites[i] == LJM.CONSTANTS.READ)
                        wrStr = "READ";
                    else
                        wrStr = "WRITE";
                    Console.WriteLine("    " + wrStr + " " + aNames[i] + " (" +
                        aAddresses[i] + ")");
                }
                Console.WriteLine("\nBeginning " + numIterations + " iterations...");


                //Initialize time variables
                double maxMS = 0;
                double minMS = 0;
                double totalMS = 0;
                double curMS = 0;
                Stopwatch sw;
                long freq = Stopwatch.Frequency;

                //eNames operations loop
                for(i = 0; i < numIterations; i++)
                {
                    sw = Stopwatch.StartNew();
                    if(useAddresses)
                        LJM.eAddresses(handle, numFrames, aAddresses, aTypes, aWrites, aNumValues, aValues, ref errAddr);
                    else
                        LJM.eNames(handle, numFrames, aNames, aWrites, aNumValues, aValues, ref errAddr);
                    sw.Stop();

                    curMS = sw.ElapsedTicks/(double)freq * 1000;
                    if(minMS == 0)
                        minMS = curMS;
                    minMS = Math.Min(curMS, minMS);
                    maxMS = Math.Max(curMS, maxMS);
                    totalMS += curMS;
                }

                Console.WriteLine("\n" + numIterations + " iterations performed:");
                Console.WriteLine("    Time taken: " +
                    Convert.ToDouble(totalMS).ToString("F3") + " ms");
                Console.WriteLine("    Average time per iteration: " +
                    Convert.ToDouble(totalMS/numIterations).ToString("F3") +
                    " ms");
                Console.WriteLine("    Min / Max time for one iteration: " +
                    Convert.ToDouble(minMS).ToString("F3") + " ms / " +
                    Convert.ToDouble(maxMS).ToString("F3") + " ms");

                if(useAddresses)
                    Console.WriteLine("\nLast eAddresses results:");
                else
                    Console.WriteLine("\nLast eNames results:");
                for(i = 0; i < numFrames; i++)
                {
                    if(aWrites[i] == LJM.CONSTANTS.READ)
                        wrStr = "READ";
                    else
                        wrStr = "WRITE";
                    Console.WriteLine("    " + aNames[i] +" (" + aAddresses[i] +
                        ") " + wrStr + " value : " + aValues[i]);
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
