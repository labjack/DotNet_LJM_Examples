//-----------------------------------------------------------------------------
// DioEFConfig2PWM2Clock.cs
//
// Enables 2 PWM signals at different frequencies using 2 separate 16-bit clocks, and 2 Interrupt Frequency In measurements.
// Enables a PWM at 5kHz with 25% Duty Cycle using Clock1 on one line, and a 10kHz 50% Duty Cycle using Clock2 on another line.
//
// To configure PWMs to user desired frequency and duty cycle, set the "desiredFrequency" and "desiredDC" variables for each line.
//
// For more info on the DIO_EF Clocks see section 13.2.1 of the T-Series Datasheet:
// https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet
//
// For more information on the PWM DIO_EF mode see section 13.2.2 of the T-Series Datasheet:
// https://support.labjack.com/docs/13-2-2-pwm-out-t-series-datasheet
//
// For more information on the Interrupt Frequency In DIO_EF mode see section 13.2.12 of the T-Series Datasheet:
// https://support.labjack.com/docs/13-2-12-interrupt-frequency-in-t-series-datasheet
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using System.Threading;
using LabJack;

namespace DioEFConfig2PWM2Clock
{
    class DioEFConfig2PWM2Clock
    {
        static void Main(string[] args)
        {
            DioEFConfig2PWM2Clock dioef = new DioEFConfig2PWM2Clock();
            dioef.configDIOEF();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void configDIOEF()
        {
            // ------------- USER INPUT VALUES -------------
            // The pairs of DIO pins for each PWM and Frequency measurement are named like "A" and "B"
            int desiredFrequencyA = 5000;   // Set this value to your first desired PWM Frequency Hz. Default 5000 Hz
            int desiredFrequencyB = 10000;  // Set this value to your second desired PWM Frequency Hz. Default 10000 Hz
            int desiredDutyCycleA = 25;     // Set this value to your first desired PWM Duty Cycle percentage. Default 25%
            int desiredDutyCycleB = 50;     // Set this value to your second desired PWM Duty Cycle percentage. Default 50%
            // ---------------------------------------------

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
                // Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle); // Any device, Any connection, Any identifier
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  //  T8 device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  //  T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  //  T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);


                // --- Configure Clocks, PWMs, and Freqs ---
                int errorAddress = -1;

                // freqDIOA pin will measure the pwmDIOA pins signal, freqDIOB pin will measure the pwmDIOB signal.
                int pwmDIOA = 0, pwmDIOB = 0;   // DIO Pins that will generate the PWM signals, set based on device type below.
                int freqDIOA = 0, freqDIOB = 0; // DIO Pins that will measure the PWM frequency and period, set based on device type below.
                int coreFrequency = 0;          // Device Specific Core Clock Frequency, used to calculate Clock Roll Value.
                string[] aNames;
                double[] aValues;
                int numFrames = 0;

                const int MAX_16_BIT_CLOCK_ROLL_VALUE = 65535;      // Max roll value for 16-bit DIO_EF clocks (Clock1 and Clock2) = 2^16 - 1


                // --- Configure device specific values ---
                // Selecting a specific DIO# Pin is necessary for each T-Series Device,
                // only specific DIO# pins can output a PWM signal or measure an Interrupt Frequency In.
                // For detailed T-Series Device DIO_EF pin mapping tables see section 13.2 of the T-Series Data Sheet:
                // https://support.labjack.com/docs/13-2-dio-extended-features-t-series-datasheet
                switch (devType)
                {
                    // For the T4, use FIO6/7 (DIO6/7) for the PWM outputs, and FIO4/5 (DIO4/5) for Frequency measurements.
                    // T4 Core Clock Speed is 80 MHz.
                    case LJM.CONSTANTS.dtT4:
                        pwmDIOA  = 6;
                        pwmDIOB  = 7;
                        freqDIOA = 4;
                        freqDIOB = 5;
                        coreFrequency = 80000000;
                        break;
                    // For the T7, use FIO2/3 (DIO2/3) for the PWM outputs, and FIO0/1 (DIO0/1) for Frequency measurements.
                    // T7 Core Clock Speed is 80 MHz.
                    case LJM.CONSTANTS.dtT7:
                        pwmDIOA  = 2;
                        pwmDIOB  = 3;
                        freqDIOA = 0;
                        freqDIOB = 1;
                        coreFrequency = 80000000;
                        break;
                    // For the T8, use FIO2/3 (DIO2/3) for the PWM outputs, and FIO0/1 (DIO0/1) for Frequency measurements.
                    // T8 Core Clock Speed is 100 MHz.
                    case LJM.CONSTANTS.dtT8:
                        pwmDIOA  = 2;
                        pwmDIOB  = 3;
                        freqDIOA = 0;
                        freqDIOB = 1;
                        coreFrequency = 100000000;
                        break;
                }

                /* --- How to Configure PWM and Interrupt Frequency In ---
                 * To configure a DIO_EF PWM out signal, you first need to configure the clock used by the DIO_EF mode.
                 *
                 * --- Registers used for configuring Clocks ---
                 * https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet#id-13.2.1EFClockSource[T-SeriesDatasheet]-Configure
                 * "DIO_FE_CLOCK#_DIVISOR":    Divides the core clock. Valid options: 1, 2, 4, 8, 16, 32, 64, 256.
                 * "DIO_EF_CLOCK#_ROLL_VALUE": The clock count will increment continuously and then start over at zero as it reaches the roll value.
                 * "DIO_EF_CLOCK#_ENABLE":     Enables/Disables the Clock.
                 *
                 * --- Registers used for configuring PWM ---
                 * https://support.labjack.com/docs/13-2-2-pwm-out-t-series-datasheet#id-13.2.2PWMOut[T-SeriesDatasheet]-Configure
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, DIO_EF PWM mode is index 0.
                 * "DIO#_EF_CLOCK_SOURCE":     (Formerly DIO#_EF_OPTIONS). Specify which clock source to use.
                 * "DIO#_EF_CONFIG_A":         When the clocks count matches this value, the line will transition from high to low.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
                 *
                 * --- Registers used for configuring Interrupt Frequency In ---
                 * https://support.labjack.com/docs/13-2-12-interrupt-frequency-in-t-series-datasheet#id-13.2.12InterruptFrequencyIn[T-SeriesDatasheet]-Configure
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, DIO_EF Intr. Frequency In is index 11.
                 * "DIO#_EF_CONFIG_A":         Measurement type, default = 0. Bit 0: Edge select; 0 = falling, 1 = rising. Bit 1: 0 = one-shot, 1 = continuous.
                 * "DIO#_EF_CONFIG_A":         Number of periods to be measured and averaged. Default = 0 which equates to 1 period.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
                 *
                 * Interrupt Frequency In is an interrupt-based digital I/O extended feature (DIO-EF) is not purely implemented in hardware,
                 * but rather firmware must service each edge. To measure the frequency, the T-series device will measure the duration of one or more periods.
                 * There are several option available to control the way the LabJack does this:
                 * The number of periods to be averaged, the edge direction to trigger on, and whether to measure continuously or in one-shot mode can all be specified.
                 *
                 * DIO_EF CONFIG_A - Measurement Modes - Rising vs. Falling and One-Shot vs. Continuous:
                 * The Interrupt Frequency In DIO_EF mode can be configured to measure the frequency and period of a signal using the "DIO#_EF_CONFIG_A" register.
                 * The "CONFIG_A" register accepts an integer value which represent a bit field where bit0 is the edge type (0 for rising or 1 for falling),
                 * and bit1 is the measurement type (0 for one-shot or 1 continuous).
                 *
                 * Measurements can be taken between the rising edges of a signal or the falling edges of a signal.
                 *
                 * There are also 2 different types of measurements One-Shot and Continuous:
                 * - One-Shot
                 *     When one-shot mode is enabled, the DIO_EF will complete a measurement then go idle.
                 *     No more measurements will be made until the DIO_EF has been read or reset.
                 *
                 * - Continuous
                 *     When continuous mode is enabled, the DIO_EF will repeatedly make measurements.
                 *     If a new reading is completed before the old one has been read the old one will be discarded.
                 *
                 * --- Configure the DIO_EF mode to your desired measurement type ---
                 * Determine your values for edge select (bit0) and measurement type (bit1), and write the integer of the combined bit values.
                 * See the table below for the mapping between bit field value and the corresponding integer to write to "DIO#_EF_CONFIG_A":
                 * | Measurement Mode | Bit Field | Int Value |
                 * | One-Shot Falling |   0b00    |     0     |
                 * | One-Shot Rising  |   0b01    |     1     |
                 * | Cont. Falling    |   0b10    |     2     |
                 * | Cont. Rising     |   0b11    |     3     |
                 */

                // Set Frequency A and B Measurement Type. Default = Continuous Rising = 0b11 = 3.
                int freqAMeas = 3;
                int freqBMeas = 3;

                /* --- Configure Clock to output the desired PWM frequency ---
                 * T-Series devices provide 2 16-bit clocks (Clock1 and Clock2) or a combined 32-bit clock (Clock0).
                 * For this example we will use the 2 16-bit clocks each configured for a different frequency.
                 *
                 * To configure a DIO_EF clock to any desired frequency, you need to calculate the Clock Tick Rate and then the Clock Roll Value.
                 * Clock Tick Rate = Core Frequency / DIO_EF_CLOCK#_DIVISOR
                 * Clock Roll Value = Clock Tick Rate / Desired Frequency
                 *
                 * In general, a slower Clock#Frequency will increase the maximum measurable period,
                 * and a faster Clock#Frequency will increase measurement resolution.
                 *
                 * For more information on DIO_EF Clocks see section 13.2.1 - EF Clock Source of the T-Series Datasheet:
                 * https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet
                 *
                 * For a more detailed walkthrough see Configuring a PWM Output:
                 * https://support.labjack.com/docs/configuring-a-pwm-output
                 */

                // --- Calculate Clock and PWM Values for Configuration ---
                // Calculate Clock Values
                int clockDivisor = 4; // Clock divisor to use in configuration
                int clockTickRate = coreFrequency / clockDivisor;

                // Calculate Clock Roll values for each clock aka the Clock Ticks per Period. pwmDIOA will use Clock1, pwmDIOB will use Clock2.
                int clockRollValueA = clockTickRate / desiredFrequencyA; // clockRollValueA should be written to "DIO_EF_CLOCK1_ROLL_VALUE"
                int clockRollValueB = clockTickRate / desiredFrequencyB; // clockRollValueB should be written to "DIO_EF_CLOCK2_ROLL_VALUE"

                // Check that roll value is valid for setClockSettings.
                if (clockRollValueA > MAX_16_BIT_CLOCK_ROLL_VALUE || clockRollValueB > MAX_16_BIT_CLOCK_ROLL_VALUE)
                {
                    string msg = $"\nInvalid Settings Detected for DIO_EF Clock." +
                                 $"\nClock 1: Calculated Roll Value = {clockRollValueA}." +
                                 $"\nClock 2: Calculated Roll Value = {clockRollValueB}." +
                                 $"\n16-bit Clock Max Roll Value = {MAX_16_BIT_CLOCK_ROLL_VALUE}. " +
                                 $"\nTry increasing frequency or decreasing clock divisor.\n";
                    Console.WriteLine(msg);
                }

                // Calculate PWM Duty Cycle
                // To configure a desired PWM Duty Cycle, we have to calculate the DIO_EF Clock tick value where the line will transition from high to low.
                // This value is calculated by dividing the clock roll value by the decimal representation of the desired duty cycle percentage.
                int pwmStepsA = (int)(clockRollValueA * ((double)desiredDutyCycleA / 100)); // This value should be written to the pwmDIOA CONFIG_A register.
                int pwmStepsB = (int)(clockRollValueB * ((double)desiredDutyCycleB / 100)); // This value should be written to the pwmDIOB CONFIG_A register.

                // Configure DIO_EF modes and write calculated values
                // Configure Clock Registers, we are using the 2 16-bit Clocks (Clock1, Clock2) for this example.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ENABLE", 0); // Ensure the combined 32-bit Clock0 is disabled before configuring Clock1 and Clock2, otherwise an error will occur.
                LJM.eWriteName(handle, "DIO_EF_CLOCK1_ENABLE", 0);
                LJM.eWriteName(handle, "DIO_EF_CLOCK2_ENABLE", 0);

                // Configure Clock divisors, both using a divisor of 1.
                LJM.eWriteName(handle, "DIO_EF_CLOCK1_DIVISOR", clockDivisor);
                LJM.eWriteName(handle, "DIO_EF_CLOCK2_DIVISOR", clockDivisor);

                // Configure Clock Roll values, calculated above to produce desired frequency.
                LJM.eWriteName(handle, "DIO_EF_CLOCK1_ROLL_VALUE", clockRollValueA);
                LJM.eWriteName(handle, "DIO_EF_CLOCK2_ROLL_VALUE", clockRollValueB);

                // Configure PWM A and B Registers
                LJM.eWriteName(handle, $"DIO{pwmDIOA}_EF_ENABLE", 0);            // Disable the DIO_EF mode on PWM A's DIO.
                LJM.eWriteName(handle, $"DIO{pwmDIOB}_EF_ENABLE", 0);            // Disable the DIO_EF mode on PWM B's DIO.
                LJM.eWriteName(handle, $"DIO{pwmDIOA}_EF_INDEX", 0);             // Set DIO#_EF_INDEX to 0 - PWM Out.
                LJM.eWriteName(handle, $"DIO{pwmDIOB}_EF_INDEX", 0);             // Set DIO#_EF_INDEX to 0 - PWM Out.
                LJM.eWriteName(handle, $"DIO{pwmDIOA}_EF_CLOCK_SOURCE", 1);      // Set DIO for PWM A to use Clock1. You may need to use "DIO#_EF_OPTIONS" on older LJM versions.
                LJM.eWriteName(handle, $"DIO{pwmDIOB}_EF_CLOCK_SOURCE", 2);      // Set DIO for PWM B to use Clock2. You may need to use "DIO#_EF_OPTIONS" on older LJM versions.
                LJM.eWriteName(handle, $"DIO{pwmDIOA}_EF_CONFIG_A", pwmStepsA);  // Set PWM A's clock tick transition value, the clock tick value where the falling edge will occur.
                LJM.eWriteName(handle, $"DIO{pwmDIOB}_EF_CONFIG_A", pwmStepsB);  // Set PWM B's clock tick transition value, the clock tick value where the falling edge will occur.
                LJM.eWriteName(handle, $"DIO{pwmDIOA}_EF_ENABLE", 1);            // Enable the DIO_EF mode, PWM A signal will not start until set Clock 1 is enabled.
                LJM.eWriteName(handle, $"DIO{pwmDIOB}_EF_ENABLE", 1);            // Enable the DIO_EF mode, PWM B signal will not start until set Clock 2 is enabled.

                // Configure Intr. Frequency In A and B Registers
                LJM.eWriteName(handle, $"DIO{freqDIOA}_EF_ENABLE", 0);           // Disable the DIO_EF mode on Freq A's DIO.
                LJM.eWriteName(handle, $"DIO{freqDIOA}_EF_ENABLE", 0);           // Disable the DIO_EF mode on Freq B's DIO.
                LJM.eWriteName(handle, $"DIO{freqDIOA}_EF_INDEX", 11);           // Set DIO#_EF_INDEX to 11 - Interrupt Frequency In.
                LJM.eWriteName(handle, $"DIO{freqDIOB}_EF_INDEX", 11);           // Set DIO#_EF_INDEX to 11 - Interrupt Frequency In.
                LJM.eWriteName(handle, $"DIO{freqDIOA}_EF_CONFIG_A", freqAMeas); // Set Freq. A's Measurement Type.
                LJM.eWriteName(handle, $"DIO{freqDIOB}_EF_CONFIG_A", freqBMeas); // Set Freq. B's Measurement Type.
                LJM.eWriteName(handle, $"DIO{freqDIOA}_EF_ENABLE", 1);           // Enable the DIO#_EF mode, it will now be ready to take measurements as configured.
                LJM.eWriteName(handle, $"DIO{freqDIOB}_EF_ENABLE", 1);           // Enable the DIO#_EF mode, it will now be ready to take measurements as configured.

                // Enable Clocks
                LJM.eWriteName(handle, "DIO_EF_CLOCK1_ENABLE", 1);              // Enable Clock 1, PWM A's signal is now being output at the configured Freq. and Duty Cycle.
                LJM.eWriteName(handle, "DIO_EF_CLOCK2_ENABLE", 1);              // Enable Clock 2, PWM B's signal is now being output at the configured Freq. and Duty Cycle.

                Console.WriteLine($"\nPWM A at {desiredFrequencyA} Hz with {desiredDutyCycleA}% Duty Cycle is now being output on DIO/FIO{pwmDIOA}. " +
                                  $"Frequency Measured by DIO/FIO{freqDIOA}.\n" +
                                  $"PWM B at {desiredFrequencyB} Hz with {desiredDutyCycleB}% Duty Cycle is now being output on DIO/FIO{pwmDIOB}. " +
                                  $"Frequency Measured by DIO/FIO{freqDIOB}.");

                Thread.Sleep(1000); // Sleep for 1 sec = 1000 ms.

                // Variables for reading measured Frequency and Period of each signal.
                double msrdFreqA = 0, msrdFreqB = 0;
                double msrdPeriodA = 0, msrdPeriodB = 0;

                // Get PWM A's measured Period and Frequency
                LJM.eReadName(handle, $"DIO{freqDIOA}_EF_READ_A_F", ref msrdPeriodA);
                LJM.eReadName(handle, $"DIO{freqDIOA}_EF_READ_B_F", ref msrdFreqA);

                // Get PWM B's measured Period and Frequency
                LJM.eReadName(handle, $"DIO{freqDIOB}_EF_READ_A_F", ref msrdPeriodB);
                LJM.eReadName(handle, $"DIO{freqDIOB}_EF_READ_B_F", ref msrdFreqB);

                Console.WriteLine($"\nMeasured Values From Signals A and B:\n");
                Console.WriteLine("| {0,3} | {1,-10} | {2,-10} |", "PWM", "Frequency", "Period");
                Console.WriteLine("|-----|------------|------------|");
                Console.WriteLine("| {0,3} | {1,-10} | {2,-10} |", "A ", msrdFreqA.ToString("F1") + " Hz", msrdPeriodA.ToString("0.000e0") + " s");
                Console.WriteLine("| {0,3} | {1,-10} | {2,-10} |", "B ", msrdFreqB.ToString("F1") + " Hz", msrdPeriodB.ToString("0.000e0") + " s");

                Console.WriteLine("\nDisabling Clocks, PWMs and Frequency Measurements");
                aNames = new string[]
                {
                    "DIO_EF_CLOCK1_ENABLE",
                    "DIO_EF_CLOCK1_ENABLE",
                    String.Format("DIO{0}_EF_ENABLE", pwmDIOA),
                    String.Format("DIO{0}_EF_ENABLE", pwmDIOB),
                    String.Format("DIO{0}_EF_ENABLE", freqDIOA),
                    String.Format("DIO{0}_EF_ENABLE", freqDIOB),
                };

                aValues = new double[] { 0, 0, 0, 0, 0, 0 };
                numFrames = aNames.Length;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                LJM.CloseAll(); // Close all handles

                Console.WriteLine("\nDone.\nPress the enter key to exit.");
                Console.ReadLine(); // Pause for user
            }
            catch (LJM.LJMException e)
            {
                // An Error has occurred.
                showErrorMessage(e);
            }
        }
    }
}
