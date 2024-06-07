//-----------------------------------------------------------------------------
// StreamExternalClock.cs
//
// Demonstrates how to stream with the T7 or T8 in external clock stream mode.
// Connect CIO3 (T7) or FIO2 (T8) to FIO3 to use a PWM for the stream clock.
//
// Note: The T4 is not supported in this example.
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
//     eWriteNames:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritenames
//     Stream Functions (such as eStreamStart, eStreamRead and eStreamStop):
//         https://labjack.com/support/software/api/ljm/function-reference/stream-functions
//     eStreamStart (for eStreamStart and externally clock stream in LJM)
//         https://labjack.com/support/ljm/users-guide/function-reference/ljmestreamstart
//     Library Configuration Functions (such as WriteLibraryConfigS and stream parameters):
//         https://labjack.com/support/software/api/ljm/function-reference/library-configuration-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Stream Mode:
//         https://labjack.com/support/datasheets/t-series/communication/stream-mode
//     Special Stream Modes (such as externally clocked):
//         https://support.labjack.com/docs/3-2-2-special-stream-modes-t-series-datasheet
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
//     Hardware Overview (such as register CORE_TIMER):
//         https://labjack.com/support/datasheets/t-series/hardware-overview
//     Pulse Out:
//         https://labjack.com/support/datasheets/t-series/digital-io/extended-features/pulse-out
//-----------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Text;
using LabJack;


namespace StreamExternalClock
{
    class StreamExternalClock
    {
        static void Main(string[] args)
        {
            StreamExternalClock sec = new StreamExternalClock();
            sec.performActions();
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
                LJM.OpenS("ANY", "USB", "ANY", ref handle);  // Any device, Any connection, Any identifier
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
                    throw new Exception("The T4 does not support externally clocked stream.");
                }

                try
                {
                    //Configure negative channels and stream settling.
                    //Not applicable to the T8.
                    if (devType == LJM.CONSTANTS.dtT7)
                    {
                        //All negative channels are single-ended.
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
                    //AIN ranges are set to ±10V (T7) or ±11V (T8).
                    //Stream resolution index is 0 (default).
                    aNames = new string[] {
                        "STREAM_TRIGGER_INDEX",
                        "AIN_ALL_RANGE",
                        "STREAM_RESOLUTION_INDEX"
                    };
                    aValues = new double[] {
                        0,
                        10.0,
                        0
                    };
                    LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);

                    //Stream Configuration
                    const bool FIO3_CLOCK = true;  //Use Pulse Out on FIO3 for the stream clock
                    const int NUM_LOOP_ITERATIONS = 10;
                    double scanRate = 1000;  //Scans per second
                    int scansPerRead = (int)scanRate / 2;  //Number of scans returned by eStreamRead call
                    const int numAddresses = 4;
                    string[] aScanListNames = new String[] { "AIN0", "FIO_STATE", "CORE_TIMER", "STREAM_DATA_CAPTURE_16" };  //Scan list names to stream.
                    int[] aTypes = new int[numAddresses];  //Dummy
                    int[] aScanList = new int[numAddresses];  //Scan list addresses to stream. eStreamStart uses Modbus addresses.
                    LJM.NamesToAddresses(numAddresses, aScanListNames, aScanList, aTypes);

                    //Configure LJM for unpredictable stream timing.
                    LJM.WriteLibraryConfigS("LJM_STREAM_SCANS_RETURN", LJM.CONSTANTS.STREAM_SCANS_RETURN_ALL_OR_NONE);
                    LJM.WriteLibraryConfigS("LJM_STREAM_RECEIVE_TIMEOUT_MODE", LJM.CONSTANTS.STREAM_RECEIVE_TIMEOUT_MODE_MANUAL);
                    LJM.WriteLibraryConfigS("LJM_STREAM_RECEIVE_TIMEOUT_MS", 100);

                    //Configure stream clock source to external clock source on
                    //CIO3 for the T7, or FIO2 for the T8.
                    Console.WriteLine("\nSetting up externally clocked stream.\n");
                    LJM.eWriteName(handle, "STREAM_CLOCK_SOURCE", 2);
                    LJM.eWriteName(handle, "STREAM_EXTERNAL_CLOCK_DIVISOR", 1);

                    if (FIO3_CLOCK)
                    {
                        //Enable Pulse Out with frequency of 1 kHz and 50% duty
                        //# cycle on FIO3.
                        double pulseRate = scanRate;
                        double numPulses = scanRate * NUM_LOOP_ITERATIONS + 5000;
                        double rollValue = 0;
                        double clockDivisor = 0;

                        //Set the clock divisor so that the clock frequency is
                        //10 MHz.
                        if(devType == LJM.CONSTANTS.dtT8)
                        {
                            //ClockFrequency = 100 MHz / 4 = 25 MHz
                            //PulseOutFrequency = 25 MHz / 25 KHz = 1 KHz
                            clockDivisor = 4;
                            rollValue = 25000;
                        }
                        else
                        {
                            //ClockFrequency = 80 MHz / 8 = 10 MHz
                            //PulseOutFrequency = 10 MHz / 10 KHz = 1 KHz
                            clockDivisor = 8;
                            rollValue = 10000;
                        }

                        //DutyCycle% = 100 * CONFIG_A / RollValue
                        //CONFIG_A = DutyCycle% / 100 * RollValue
                        double dutyCyclePercent = 50;
                        double dutyCycleValue = dutyCyclePercent / 100 * rollValue;

                        Console.WriteLine("Enabling {0} pulses on FIO3 at a {1} Hz pulse rate.\n", numPulses, pulseRate);

                        LJM.eWriteName(handle, "DIO3_EF_ENABLE", 0);
                        LJM.eWriteName(handle, "DIO3", 0);
                        LJM.eWriteName(handle, "DIO_EF_CLOCK0_DIVISOR", clockDivisor);
                        LJM.eWriteName(handle, "DIO_EF_CLOCK0_ROLL_VALUE", rollValue);
                        LJM.eWriteName(handle, "DIO_EF_CLOCK0_ENABLE", 1);
                        LJM.eWriteName(handle, "DIO3_EF_INDEX", 2);
                        LJM.eWriteName(handle, "DIO3_EF_CLOCK_SOURCE", 0);
                        LJM.eWriteName(handle, "DIO3_EF_CONFIG_A", dutyCycleValue);
                        LJM.eWriteName(handle, "DIO3_EF_CONFIG_B", 0);
                        LJM.eWriteName(handle, "DIO3_EF_CONFIG_C", numPulses);
                        LJM.eWriteName(handle, "DIO3_EF_ENABLE", 1);
                    }

                    //Configure and start stream
                    LJM.eStreamStart(handle, scansPerRead, numAddresses, aScanList, ref scanRate);

                    UInt64 loop = 0;
                    UInt64 totScans = 0;
                    double[] aData = new double[scansPerRead * numAddresses];  //# of samples per eStreamRead is scansPerRead * numAddresses
                    UInt64 skippedTotal = 0;
                    int skippedCur = 0;
                    int deviceScanBacklog = 0;
                    int ljmScanBacklog = 0;
                    Stopwatch sw = new Stopwatch();

                    sw.Start();
                    while (loop < NUM_LOOP_ITERATIONS)
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

                            //Parse out first scan of samples to display.
                            //CORE_TIMER and STREAM_DATA_CAPTURE_16 samples
                            //are combined for the full 32-bit CORE_TIMER value.
                            StringBuilder scanStr = new StringBuilder();
                            uint coreTimer32bits = 0;
                            for (int j = 0; j < numAddresses; j++)
                            {
                                if (j == 2)
                                {
                                    //Sample 2 = CORE_TIMER
                                    //CORE_TIMER is the lower 16-bits of the full
                                    //32-bit CORE_TIMER value.
                                    coreTimer32bits = System.Convert.ToUInt32(aData[j]);
                                }
                                else if (j == 3)
                                {
                                    //Sample 3 = STREAM_DATA_CAPTURE_16
                                    //STREAM_DATA_CAPTURE_16 is the upper 16-bits of the
                                    //full 32-bit CORE_TIMER value.
                                    coreTimer32bits += System.Convert.ToUInt32(aData[j]) << 16;
                                    scanStr.AppendFormat("CORE_TIMER and STREAM_DATA_CAPTURE_16 = {0:D}", coreTimer32bits);
                                }
                                else
                                {
                                    //Sample 0 = AIN0
                                    //Sample 1 = FIO_STATE
                                    scanStr.AppendFormat("{0} = {1:F5}, ", aScanListNames[j], aData[j]);
                                }
                            }

                            Console.WriteLine("  1st scan out of " + scansPerRead + ": " + scanStr);
                            Console.WriteLine("  Scans Skipped: " + (skippedCur / numAddresses) + 
                                ", Scan Backlogs: Device = " + deviceScanBacklog + ", LJM = " +
                                ljmScanBacklog);
                        }
                        catch (LJM.LJMException e)
                        {
                            if (e.LJMError == LJM.LJMERROR.NO_SCANS_RETURNED)
                            {
                                Console.Write(".");
                                Console.Out.Flush();
                            }
                            else
                            {
                                //Error other than NO_SCANS_RETURNED. Stopping stream loop.
                                showErrorMessage(e);
                                break;
                            }
                        }
                    }
                    sw.Stop();

                    Console.WriteLine("\nTotal scans: " + totScans);
                    double time = sw.ElapsedMilliseconds / 1000.0;
                    Console.WriteLine("Time taken: " + time + " seconds");
                    Console.WriteLine("LJM Scan Rate: " + scanRate + " scans/second");
                    Console.WriteLine("Timed Scan Rate: " + (totScans / time).ToString("F2") + " scans/second");
                    Console.WriteLine("Timed Sample Rate: " + (totScans * numAddresses / time).ToString("F2") + " samples/second");
                    Console.WriteLine("Skipped scans: " + (skippedTotal / numAddresses));
                }
                catch (LJM.LJMException e)
                {
                    showErrorMessage(e);
                }
                Console.WriteLine("\nStop Stream");
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
            if (sleepMS < 1)
            {
                return;
            }
            System.Threading.Thread.Sleep(sleepMS);
        }
    }
}
