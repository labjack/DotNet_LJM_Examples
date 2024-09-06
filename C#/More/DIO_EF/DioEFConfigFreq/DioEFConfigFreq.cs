//-----------------------------------------------------------------------------
// DioEFConfigFreq.cs
//
// Enables a Frequency In measurement and a 10 Hz square wave on DAC1.
// To measure the DAC1 Frequency and Period, connect a jumper between DAC1 and FIO0 on T7/T8 or FIO4 on T4.
//
// Frequency In will measure the period or frequency of a digital input signal by counting the number of clock source ticks between two edges:
// rising-to-rising (DIO_EF index = 3) or falling-to-falling (DIO_EF index = 4).
//
// This example will read the DAC1 frequency and period 5 times at 1 second intervals.
//
// For more information on the Frequency In DIO_EF mode see section 13.2.5 of the T-Series Datasheet:
// https://support.labjack.com/docs/13-2-5-frequency-in-t-series-datasheet
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using System.Threading;
using LabJack;


namespace DioEFConfigFreq
{
    class DioEFConfigFreq
    {
        static void Main(string[] args)
        {
            DioEFConfigFreq obj = new DioEFConfigFreq();
            obj.ConfigureFrequencyIn();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void ConfigureFrequencyIn()
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
                // --- Connect to a LabJack Device ---
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle); // Any device, Any connection, Any identifier
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  // T8 device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);


                // --- Configure Clock and Frequency In ---
                int errorAddress = -1;
                int freqDIO      = 0;   // DIO Pin that will measure the signal, T7/T8 use FIO0, T4 use FIO4.
                int numFrames    = 0;
                int intervalHandle   = 1;
                int skippedIntervals = 0;
                string[] aNames;
                double[] aValues;


                // Selecting a specific DIO# Pin is necessary for each T-Series Device, only specific DIO# pins can measure a Frequency In signal.
                // For detailed T-Series Device DIO_EF pin mapping tables see section 13.2 of the T-Series Datasheet:
                // https://support.labjack.com/docs/13-2-dio-extended-features-t-series-datasheet
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    // For the T4, use FIO4/DIO4 for the Frequency In measurement.
                    freqDIO = 4;
                }

                /* --- How to Configure Frequency In Measurment ---
                 * To configure a DIO_EF Frequency In measurement mode, you need first to configure the clock used by the DIO_EF mode,
                 * then configure the DIO_EF mode itself.
                 *
                 * See Datasheet reference for DIO_EF Clocks:
                 * https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet
                 *
                 * See Datasheet reference for DIO_EF Frequency In:
                 * https://support.labjack.com/docs/13-2-5-frequency-in-t-series-datasheet
                 *
                 * -- Registers used for configuring DAC1 Frequency Out ---
                 * "DAC1_FREQUENCY_OUT_ENABLE": 0 = off, 1 = output 10 Hz signal on DAC1. The signal will be a square wave with peaks of 0 and 3.3V.
                 *
                 * --- Registers used for configuring DIO_EF Clock ---
                 * "DIO_FE_CLOCK#_DIVISOR":    Divides the core clock. Valid options: 1, 2, 4, 8, 16, 32, 64, 256.
                 * "DIO_EF_CLOCK#_ROLL_VALUE": The clock count will increment continuously and then start over at zero as it reaches the roll value.
                 * "DIO_EF_CLOCK#_ENABLE":     Enables/Disables the Clock.
                 *
                 * --- Registers used for configuring Frequency In ---
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, DIO_EF Frequency In uses index 3 for rising edges, 4 for falling edges.
                 * "DIO#_EF_CLOCK_SOURCE":     (Formerly DIO#_EF_OPTIONS). Specify which clock source to use.
                 * "DIO#_EF_CONFIG_A":         Measurement mode Default = 0. One-Shot measurement = 2, Continuous measurement = 0. See below for more info on measurement modes.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
                 *
                 * Frequency In will measure the period or frequency of a digital input signal by counting the number of clock source ticks between two edges:
                 * rising-to-rising (index=3) or falling-to-falling (index=4).
                 *
                 * DIO_EF Config A - Measurement Modes - One-Shot vs. Continuous:
                 * - One-Shot
                 *     When one-shot mode is enabled, the DIO_EF will complete a measurement then go idle.
                 *     No more measurements will be made until the DIO_EF has been read or reset.
                 *
                 * - Continuous
                 *     When continuous mode is enabled, the DIO_EF will repeatedly make measurements.
                 *     If a new reading is completed before the old one has been read the old one will be discarded.
                 *
                 * Continuous Measurements are the default mode and recommended for most use cases.
                 * For more info on measurement modes see:
                 * https://support.labjack.com/docs/13-2-5-frequency-in-t-series-datasheet#id-13.2.5FrequencyIn[T-SeriesDatasheet]-Configure
                 */

                // --- Clock Configuration Values ---
                int clockDivisor = 1;
                int clockRollValue = 0; // A value of 0 sets the clock to roll at its max value, allowing the maximum measurable period.

                // --- Frequency In Configuration Values ---
                int freqIndex = 3;     // Measuring between rising-to-rising edges.
                //int freqIndex = 4;   // Measuring between falling-to-falling edges.
                int freqConfigA = 0;   // Measurement mode set to continuous, the default value.
                //int freqConfigA = 2; // Measurement mode set to one-shot.

                // --- Configure and write values to connected device ---
                // Configure Clock Registers, use 32-bit Clock0.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_DIVISOR", clockDivisor);      // Set Clock Divisor.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ROLL_VALUE", clockRollValue); // Set Clock Roll Value

                // Configure DIO_EF Frequency Registers.
                LJM.eWriteName(handle, $"DIO{freqDIO}_EF_INDEX", freqIndex);        // Set DIO#_EF_INDEX to 3 for rising-to-rising, 4 for falling-to-falling
                LJM.eWriteName(handle, $"DIO{freqDIO}_EF_CLOCK_SOURCE", 0);         // Set DIO#_EF to use clock 0. Formerly DIO#_EF_OPTIONS, you may need to switch to this name on older LJM versions.
                LJM.eWriteName(handle, $"DIO{freqDIO}_EF_CONFIG_A", freqConfigA);   // Set DIO#_EF_CONFIG_A to set measurement mode.
                LJM.eWriteName(handle, $"DIO{freqDIO}_EF_ENABLE", 1);               // Enable the DIO#_EF Mode, DIO will not start measurement until DIO and Clock are enabled.

                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ENABLE", 1);                  // Enable Clock0, this will start the measurements.

                LJM.eWriteName(handle, "DAC1_FREQUENCY_OUT_ENABLE", 1);             // Enable 10 Hz square wave on DAC1.

                Console.WriteLine($"\n--- Outputting a 10 Hz signal on DAC1, measuring signal on FIO{freqDIO} ---\n");

                /*
                 * --- How to read the measured frequency ---
                 * There are multiple registers available to read results from for this DIO_EF mode.
                 * DIO#_EF_READ_A: Returns the period in ticks. If a full period has not yet been observed this value will be zero.
                 * DIO#_EF_READ_B: Returns the same value as READ_A.
                 * DIO#_EF_READ_A_F: Returns the period in seconds. If a full period has not yet been observed this value will be zero.
                 * DIO#_EF_READ_B_F: Returns the frequency in Hz. If a full period has not yet been observed this value will be zero.
                 *
                 * Note that all "READ_B" registers are capture registers.
                 * All "READ_B" registers are only updated when any "READ_A" register is read.
                 * Thus it would be unusual to read any B registers without first reading at least one A register.
                 *
                 * To Reset the READ registers:
                 * DIO#_EF_READ_A_AND_RESET: Returns the same data as DIO#_EF_READ_A and then clears the result
                 * so that zero is returned by subsequent reads until another full period is measured (2 new edges).
                 *
                 * DIO#_EF_READ_A_AND_RESET_F: Returns the same data as DIO#_EF_READ_A_F and then clears the result
                 * so that zero is returned by subsequent reads until another full period is measured (2 new edges).
                 *
                 * Note that when One-Shot mode is enabled, there is conflicting behavior
                 * between one-shot and READ_A_AND_RESET registers which can lead to unexpected results.
                 * For more info see:
                 * https://support.labjack.com/docs/13-2-12-interrupt-frequency-in-t-series-datasheet#id-13.2.12InterruptFrequencyIn[T-SeriesDatasheet]-One-shot,Continuous,Read,ReadandReset
                 */

                double periodTicks = 0;
                double periodSec   = 0;
                double freqHz      = 0;

                // Start a 1 second interval.
                LJM.StartInterval(intervalHandle, 1000000);

                // Read all of the measured values.
                aNames = new string[]
                {
                    $"DIO{freqDIO}_EF_READ_A",
                    $"DIO{freqDIO}_EF_READ_A_F",
                    $"DIO{freqDIO}_EF_READ_B_F"
                };
                aValues = new double[] { 0, 0, 0 };
                numFrames = aNames.Length;

                for (int i = 0; i < 5; i++)
                {
                    LJM.WaitForNextInterval(intervalHandle, ref skippedIntervals); // Wait for 1 Second = 1000 ms.

                    // Get the period ticks, period seconds and frequency.
                    LJM.eReadNames(handle, numFrames, aNames, aValues, ref errorAddress);
                    periodTicks = aValues[0];
                    periodSec = aValues[1];
                    freqHz = aValues[2];

                    Console.WriteLine($"DIO_EF Measured Values - Frequency: {freqHz} Hz | Period: {periodSec} sec {periodTicks} ticks.");
                }

                // Clean up the memory for the interval handle.
                LJM.CleanInterval(intervalHandle);

                // Disable Clock, Frequency Measurement, and DAC1 Frequency Out.
                aNames = new string[]
                {
                    "DIO_EF_CLOCK0_ENABLE",
                    $"DIO{freqDIO}_EF_ENABLE",
                    "DAC1_FREQUENCY_OUT_ENABLE"
                };
                aValues = new double[] { 0, 0, 0 };
                numFrames = aNames.Length;

                Console.WriteLine($"\n--- Disabling Clock0, Frequency In, and DAC1_FREQUENCY_OUT ---");
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);


                LJM.CloseAll(); // Close all handles

                Console.WriteLine("\nDone.\nPress the enter key to exit.");
                Console.ReadLine(); // Pause for user
            }
            catch (LJM.LJMException e)
            {
                // An Error has occured.
                showErrorMessage(e);
            }
        }
    }
}
