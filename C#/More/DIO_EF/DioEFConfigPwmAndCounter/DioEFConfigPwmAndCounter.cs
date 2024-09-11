//-----------------------------------------------------------------------------
// DioEFConfigPwmAndCounter.cs
//
// Enables a 10 kHz PWM output and high speed counter, and reads the counter
// every 1 second in a loop. If you jumper the counter to PWM, it should
// increment about 10000 counts each loop iteration.
//
// To configure PWM to user desired frequency and duty cycle, modify the
// "desiredFrequency" and "desiredDutyCyclePercent" variables.
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace DioEFConfigPwmAndCounter
{
    class DioEFConfigPwmAndCounter
    {
        static void Main(string[] args)
        {
            DioEFConfigPwmAndCounter dioef = new DioEFConfigPwmAndCounter();
            dioef.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            // ------------- USER INPUT VALUES -------------
            int desiredFrequency = 10000;      // Set this value to your desired PWM Frequency Hz. Default 10000 Hz
            int desiredDutyCyclePercent = 50;  // Set this value to your desired PWM Duty Cycle percentage. Default 50%
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


                int errorAddress = -1;
                int pwmDIO = 0, counterDIO = 0;
                int coreFrequency = 0;
                int clockDivisor = 1;
                int numFrames = 0;
                double counterVal = 0;
                int intervalHandle = 1;
                int skippedIntervals = 0;
                string[] aNames;
                double[] aValues;

                /* -- High-Speed Counter Info and Hardware Conflicts ---
                * The DIO_EF High-Speed Counter uses a hardware counter to achieve high count rates up to 5 MHz.
                * The hardware counters are also used by the EF clocks which can cause potential hardware conflicts.
                * Enabling a High-Speed Counter on a DIO pin which conflicts with the EF clocks will cause an error.
                * The conflicts between hardware are outlined below.
                *
                * T4 and T7 devices support up to 4 high-speed rising-edge counters, these counters are shared with other resources as follows:
                * CounterA (DIO16/CIO0): Used by EF Clock0 and Clock1.
                * CounterB (DIO17/CIO1): Used by EF Clock0 and Clock2.
                * CounterC (DIO18/CIO2): Used by the asynchronous serial communication feature on the T4. Always available on the T7.
                * CounterD (DIO19/CIO3): Used by stream mode.
                *
                * The T8 supports up to 7 high-speed rising-edge counters,these counters are shared with other resources as follows:
                * DIO14/EIO6: Used by EF Clock0 and Clock1.
                * DIO15/EIO7: Used by EF Clock0 and Clock2.
                *
                * Selecting a specific DIO# Pin is necessary for each T-Series Device, only specific DIO# pins can measure with a High-Speed Counter.
                * For detailed T-Series Device DIO_EF pin mapping tables see section 13.2 of the T-Series Datasheet:
                * https://support.labjack.com/docs/13-2-dio-extended-features-t-series-datasheet
                */
                switch (devType)
                {
                    // For the T4, use FIO6 (DIO6) for the PWM output.
                    // Use CIO2 (DIO18) for the high speed counter.
                    // T4 Core Clock Speed is 80 MHz.
                    case LJM.CONSTANTS.dtT4:
                        pwmDIO = 6;
                        counterDIO = 18;
                        coreFrequency = 80000000;
                        break;
                    // For the T7, use FIO2 (DIO2) for the PWM output.
                    // Use CIO2 (DIO18) for the high speed counter.
                    // T7 Core Clock Speed is 80 MHz.
                    case LJM.CONSTANTS.dtT7:
                        pwmDIO = 2;
                        counterDIO = 18;
                        coreFrequency = 80000000;
                        break;
                    // For the T8, use FIO2 (DIO2) for the PWM output.
                    // Use FIO6 (DIO6) for the high speed counter.
                    // T8 Core Clock Speed is 100 MHz.
                    case LJM.CONSTANTS.dtT8:
                        pwmDIO = 2;
                        counterDIO = 6;
                        coreFrequency = 100000000;
                        break;
                };

                /* --- How to Configure PWM and Counter ---
                 * To confiure a DIO_EF PWM out signal, you first need to configure the clock used by the DIO_EF mode,
                 * Then you can configure the PWM and the High-Speed Counter.
                 *
                 *
                 * --- Registers used for configuring Clocks ---
                 * "DIO_FE_CLOCK#_DIVISOR":    Divides the core clock. Valid options: 1, 2, 4, 8,1 6, 32, 64, 256.
                 * "DIO_EF_CLOCK#_ROLL_VALUE": The clock count will increment continuously and then start over at zero as it reaches the roll value.
                 * "DIO_EF_CLOCK#_ENABLE":     Enables/Disables the Clock.
                 *
                 * --- Registers used for configuring PWM ---
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, DIO_EF PWM mode is index 0.
                 * "DIO#_EF_CLOCK_SOURCE":     (Formerly DIO#_EF_OPTIONS). Specify which clock source to use.
                 * "DIO#_EF_CONFIG_A":         When the clocks count matches this value, the line will transition from high to low.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
                 *
                 * --- Registers used for configuring High-Speed Counter ---
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, DIO_EF High-Speed Counter is index 7.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
                 *
                 * For more info on the DIO_EF Clocks see section 13.2.1 of the T-Series Datasheet:
                 * https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet
                 *
                 * For a more detailed walkthrough see Configuring a PWM Output:
                 * https://support.labjack.com/docs/configuring-a-pwm-output
                 */

                // --- Calculate Clock Values from user defined values ---
                int clockTickRate = coreFrequency / clockDivisor;
                int clockRollValue = clockTickRate / desiredFrequency; // clockRollValue should be written to "DIO_EF_CLOCK0_ROLL_VALUE"

                // Below is a single equation which calculates the same value as the above equations
                //clockRollValue = coreFrequency / clockDivisor / desiredFrequency;

                // --- Calculate PWM Values ---
                // Calculate the clock tick value where the line will transition from high to low based on user defined duty cycle percentage, rounded to the nearest integer.
                int pwmConfigA = (int)(clockRollValue * ((double)desiredDutyCyclePercent / 100));

                // --- Configure and write values to connected device ---
                // Configure Clock Registers, use 32-bit Clock0 for this example.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_DIVISOR", clockDivisor);   // Set Clock Divisor.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ROLL_VALUE", clockRollValue); // Set calculated Clock Roll Value.

                // Configure PWM Registers
                LJM.eWriteName(handle, String.Format("DIO{0}_EF_INDEX", pwmDIO), 0);              // Set DIO#_EF_INDEX to 0 - PWM Out.
                LJM.eWriteName(handle, String.Format("DIO{0}_EF_CLOCK_SOURCE", pwmDIO), 0);       // Set DIO#_EF to use clock 0. Formerly DIO#_EF_OPTIONS, you may need to switch to this name on older LJM versions.
                LJM.eWriteName(handle, String.Format("DIO{0}_EF_CONFIG_A", pwmDIO), pwmConfigA);  // Set DIO#_EF_CONFIG_A to the calculated value.
                LJM.eWriteName(handle, String.Format("DIO{0}_EF_ENABLE", pwmDIO), 1);             // Enable the DIO#_EF Mode, PWM signal will not start until DIO_EF and CLOCK are enabled.

                // Configure High-Speed Counter Registers
                LJM.eWriteName(handle, String.Format("DIO{0}_EF_INDEX", counterDIO), 7);          // Set DIO#_EF_INDEX to 7 - High-Speed Counter.
                LJM.eWriteName(handle, String.Format("DIO{0}_EF_ENABLE", counterDIO), 1);         // Enable the High-Speed Counter.

                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ENABLE", 1);   // Enable Clock0, this will start the PWM signal.


                // Start a 1 second interval
                LJM.StartInterval(intervalHandle, 1000000);

                // Reading from the counter in a loop
                for (int i = 0; i < 5; i++)
                {
                    // Wait until the 1 second interval is complete
                    LJM.WaitForNextInterval(intervalHandle, ref skippedIntervals);

                    // Read from the counter
                    LJM.eReadName(handle, String.Format("DIO{0}_EF_READ_A", counterDIO), ref counterVal);

                    Console.WriteLine("\nCounter - {0}", counterVal);
                    if (skippedIntervals > 0)
                    {
                        Console.WriteLine("SkippedIntervals: " + skippedIntervals);
                    }
                }

                // Clean up the memory for the interval handle.
                LJM.CleanInterval(intervalHandle);

                // Turn off PWM output and counter
                aNames = new string[]
                {
                    "DIO_EF_CLOCK0_ENABLE",
                    String.Format("DIO{0}_EF_ENABLE", pwmDIO),
                    String.Format("DIO{0}_EF_ENABLE", counterDIO),
                };

                aValues = new double[]
                {
                    0,
                    0,
                    0
                };
                numFrames = aNames.Length;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                LJM.CloseAll(); //Close all handles

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
