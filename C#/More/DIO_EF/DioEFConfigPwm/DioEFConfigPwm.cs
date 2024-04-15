//-----------------------------------------------------------------------------
// DioEFConfigPwm.cs
//
// Enables a 10 kHz PWM output.
// For more information on the PWM DIO_EF mode see section 13.2.2 of the T-Series Datasheet.
// https://support.labjack.com/docs/13-2-2-pwm-out-t-series-datasheet
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace DioEFConfigPwm
{
    class DioEFConfigPwm
    {
        static void Main(string[] args)
        {
            DioEFConfigPwm dioef = new DioEFConfigPwm();
            dioef.connectToDevice();
        }
        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }
        public void connectToDevice()
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
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);   // Any device, Any connection, Any identifier
                //LJM.OpenS("T8", "ANY", "ANY", ref handle);  //  T8 device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  //  T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  //  T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);
                configurePWM(handle, devType); // configure the PWM signal
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  // Pause for user
        }
        void configurePWM(int handle, int devType)
        {
            int errorAddress = -1;
            int pwmDIO = 0;            // DIO Pin that will generate the PWM signal, set based on device type below.
            int coreFrequency = 0;     // Device Specific Core Clock Frequency, used to calculate Clock Roll Value
            int desiredFreq = 10000;   // We want to configure PWM to 10 kHz.
            int clockDivisor = 1;      // Clock Divisor to use in configuration.
            string[] aNames;
            double[] aValues;
            int numFrames = 0;

            // Configure device specific values
            // For detailed T-Series Device DIO_EF pin mapping tables see section 13.2 of the T-Series Data Sheet:
            // https://support.labjack.com/docs/13-2-dio-extended-features-t-series-datasheet
            switch(devType){
                // For the T4, use FIO6 (DIO6) for the PWM output. T4 Core Clock Speed is 80,000 Hz.
                case LJM.CONSTANTS.dtT4:
                    pwmDIO = 6;
                    coreFrequency = 80000;
                    break;
                // For the T7, use FIO0 (DIO0) for the PWM output. T7 Core Clock Speed is 80,000 Hz.
                case LJM.CONSTANTS.dtT7:
                    pwmDIO = 0;
                    coreFrequency = 80000;
                    break;
                // For the T8, use FIO7 (DIO7) for the PWM output. T8 Core Clock Speed is 100,000 Hz.
                case LJM.CONSTANTS.dtT8:
                    pwmDIO = 7;
                    coreFrequency = 100000;
                    break;
            };

            /* How to Configure a Clock and PWM Signal?
             * See Datasheet refrence for DIO_EF Clocks: https://support.labjack.com/docs/13-2-1-ef-clock-source-t-series-datasheet
             * Registers used for configuring Clocks: 
             * "DIO_FE_CLOCK0_DIVISOR": Divides the core clock. Valid options: 1,2,4,8,16,32,64,256.
             * "DIO_EF_CLOCK0_ROLL_VALUE": The clock count will increment continuously and then start over at zero as it reaches the roll value.
             * "DIO_EF_CLOCK0_ENABLE": Enables/Disables the Clock.
             * 
             * Registers used for configuring PWM
             * DIO#_EF_INDEX: Sets desired DIO_EF feature, DIO_EF PWM mode is index 0.
             * DIO#_EF_CONFIG_A: When the clocks count matches this value, the line will transition from high to low.
             * DIO#_EF_ENABLE: Enables/Disables the DIO_EF mode.
             * 
             * To configure a DIO_EF clock to any desired frequency, you need to calculate the Clock Frequency and then the Clock Roll Value. 
             * Clock Frequency = Core Frequency / DIO_EF_CLOCK#_DIVISOR 
             * Clock Roll Value = Clock Frequency / Desired Frequency
             * or all together:
             * Clock Roll Value = Core Frequency / DIO_EF_CLOCK#_DIVISOR / Desired Frequency
             */
            // Calculate Clock Values

            // To-Do: Does this make more sense as 2 equations or a single equation?
            int clockFreq = coreFrequency / clockDivisor;
            int clockRollValue = clockFreq / desiredFreq;
            // or 
            clockRollValue = coreFrequency / clockDivisor / desiredFreq;

            // clockRollValue should be written to DIO_EF_CLOCK0_ROLL_VALUE

            // Calculate PWM Values
            // To-Do: How to explain this to customers?, or show how to set other duty cycles.
            int pwmConfigA = clockRollValue / 2; // Calculates a 50% Duty Cycle

            /* What the PWM signal will look like based on Clock0 Count
             * PWM will go high when Clock Count = 0, and then go low halfway to the Clock Roll Value thus a 50% duty cycle.
             *  __________            __________           
             * |          |          |          |          |
             * |          |__________|          |__________|
             * 0        Roll/2      Roll      Roll/2      Roll
             * 0          50%       100%       50%        100%
             */

            // Configure and write values to connected device
            // To-Do: Use String.Format or use $"DIO{pwmDIO}_EF_..."
            aNames = new string[] { "DIO_EF_CLOCK0_DIVISOR",
                                    "DIO_EF_CLOCK0_ROLL_VALUE",
                                    "DIO_EF_CLOCK0_ENABLE",
                                    String.Format("DIO{0}_EF_INDEX",    pwmDIO),
                                    String.Format("DIO{0}_EF_CONFIG_A", pwmDIO),
                                    String.Format("DIO{0}_EF_ENABLE",   pwmDIO),
            };

            aValues = new double[] { clockDivisor,
                                     clockRollValue,
                                     1,
                                     0,
                                     pwmConfigA,
                                     1,
            };
            numFrames = aNames.Length;
            LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);
            // Clock is configured and enabled, PWM is configured and enabled.
            // A 10 kHz Signal w/ 50% Duty Cycle is being output on pwmDIO pin.
            Console.Out.WriteLine("PWM will run for 10 seconds");
            Thread.Sleep(10000);

            // Do whatever you need to to, then disable Clock and PWM.

            // Turn off Clock and PWM output.
            aNames = new string[] { "DIO_EF_CLOCK0_ENABLE",
                                    String.Format("DIO{0}_EF_ENABLE", pwmDIO),
            };

            aValues = new double[] { 0, 0 };
            numFrames = aNames.Length;
            LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);
        }
    }
}