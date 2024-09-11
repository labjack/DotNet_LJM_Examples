//-----------------------------------------------------------------------------
// DioEFConfigPwm.cs
//
// Enables a 10 kHz PWM output for 10 seconds.
// To configure PWM to user desired frequency and duty cycle, modify the "desiredFrequency" and "desiredDutyCycle" variables.
//
// For more information on the PWM DIO_EF mode see section 13.2.2 of the T-Series Datasheet:
// https://support.labjack.com/docs/13-2-2-pwm-out-t-series-datasheet
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using System.Threading;
using LabJack;


namespace DioEFConfigPwm
{
    class DioEFConfigPwm
    {
        static void Main(string[] args)
        {
            DioEFConfigPwm pwm = new DioEFConfigPwm();
            pwm.ConfigurePWM();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void ConfigurePWM()
        {
            // ------------- USER INPUT VALUES -------------
            int desiredFrequency = 10000;  // Set this value to your desired PWM Frequency Hz. Defualt 10000 Hz
            int desiredDutyCycle = 50;     // Set this value to your desired PWM Duty Cycle percentage. Default 50%
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
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  // T8 device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);


                // --- Configure Clock and PWM ---
                int errorAddress = -1;
                int pwmDIO = 0;  // DIO Pin that will generate the PWM signal, set based on device type below.
                int coreFrequency = 0;  // Device Specific Core Clock Frequency, used to calculate Clock Roll Value.
                int clockDivisor = 1;  // Clock Divisor to use in configuration.
                string[] aNames;
                double[] aValues;
                int numFrames = 0;


                // --- Configure device specific values ---
                // Selecting a specific DIO# Pin is necessary for each T-Series Device, only specific DIO# pins can output a PWM signal.
                // For detailed T-Series Device DIO_EF pin mapping tables see section 13.2 of the T-Series Datasheet:
                // https://support.labjack.com/docs/13-2-dio-extended-features-t-series-datasheet
                switch (devType)
                {
                    // For the T4, use FIO6 (DIO6) for the PWM output. T4 Core Clock Speed is 80 MHz.
                    case LJM.CONSTANTS.dtT4:
                        pwmDIO = 6;
                        coreFrequency = 80000000;
                        break;
                    // For the T7, use FIO2 (DIO2) for the PWM output. T7 Core Clock Speed is 80 MHz.
                    case LJM.CONSTANTS.dtT7:
                        pwmDIO = 2;
                        coreFrequency = 80000000;
                        break;
                    // For the T8, use FIO2 (DIO2) for the PWM output. T8 Core Clock Speed is 100 MHz.
                    case LJM.CONSTANTS.dtT8:
                        pwmDIO = 2;
                        coreFrequency = 100000000;
                        break;
                }

                /* --- How to Configure a Clock and PWM Signal? ---
                 * See Datasheet reference for DIO_EF Clocks:
                 * https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet
                 *
                 * To configure a DIO_EF PWM out signal, you first need to configure the clock used by the DIO_EF mode.
                 *
                 * --- Registers used for configuring Clocks ---
                 * "DIO_FE_CLOCK#_DIVISOR":    Divides the core clock. Valid options: 1, 2, 4, 8, 16, 32, 64, 256.
                 * "DIO_EF_CLOCK#_ROLL_VALUE": The clock count will increment continuously and then start over at zero as it reaches the roll value.
                 * "DIO_EF_CLOCK#_ENABLE":     Enables/Disables the Clock.
                 *
                 * --- Registers used for configuring PWM ---
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, DIO_EF PWM mode is index 0.
                 * "DIO#_EF_CLOCK_SOURCE":     (Formerly DIO#_EF_OPTIONS). Specify which clock source to use.
                 * "DIO#_EF_CONFIG_A":         When the clocks count matches this value, the line will transition from high to low.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
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

                // Calculate Clock Values
                int clockTickRate = coreFrequency / clockDivisor;
                int clockRollValue = clockTickRate / desiredFrequency; // clockRollValue should be written to "DIO_EF_CLOCK0_ROLL_VALUE"

                // Below is a single equation which calculates the same value as the above equations
                //clockRollValue = coreFrequency / clockDivisor / desiredFrequency;


                // --- Calculate PWM Values ---
                // Calculate the clock tick value where the line will transition from high to low based on user defined duty cycle percentage, rounded to the nearest integer.
                int pwmConfigA = (int)(clockRollValue * ((double)desiredDutyCycle / 100));

                /* What the PWM signal will look like based on Clock0 Count for a 50% Duty Cycle.
                 * PWM will go high when Clock Count = 0, and then go low halfway to the Clock Roll Value thus a 50% duty cycle.
                 *  __________            __________
                 * |          |          |          |          |
                 * |          |__________|          |__________|
                 * 0        Roll/2      Roll      Roll/2      Roll
                 * 0          50%       100%       50%        100%
                 */

                // --- Configure and write values to connected device ---
                // Configure Clock Registers, use 32-bit Clock0 for this example.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_DIVISOR", clockDivisor);   // Set Clock Divisor.
                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ROLL_VALUE", clockRollValue); // Set calculated Clock Roll Value.

                // Configure PWM Registers
                LJM.eWriteName(handle, $"DIO{pwmDIO}_EF_INDEX", 0);              // Set DIO#_EF_INDEX to 0 - PWM Out.
                LJM.eWriteName(handle, $"DIO{pwmDIO}_EF_CLOCK_SOURCE", 0);       // Set DIO#_EF to use clock 0. Formerly DIO#_EF_OPTIONS, you may need to switch to this name on older LJM versions.
                LJM.eWriteName(handle, $"DIO{pwmDIO}_EF_CONFIG_A", pwmConfigA);  // Set DIO#_EF_CONFIG_A to the calculated value.
                LJM.eWriteName(handle, $"DIO{pwmDIO}_EF_ENABLE", 1);             // Enable the DIO#_EF Mode, PWM signal will not start until DIO_EF and CLOCK are enabled.

                LJM.eWriteName(handle, "DIO_EF_CLOCK0_ENABLE", 1);               // Enable Clock0, this will start the PWM signal.


                Console.Out.WriteLine($"A PWM Signal at {desiredFrequency} Hz with a duty cycle of {desiredDutyCycle} % is now being output on DIO{pwmDIO} for 10 seconds.");

                Thread.Sleep(10000); // Sleep for 10 Seconds = 10000 ms, remove this line to allow PWM to run until stopped.

                // Turn off Clock and PWM output.
                aNames = new string[]
                {
                    "DIO_EF_CLOCK0_ENABLE",
                    String.Format("DIO{0}_EF_ENABLE", pwmDIO),
                };

                aValues = new double[] { 0, 0 };
                numFrames = aNames.Length;
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
