//-----------------------------------------------------------------------------
// DioEFConfigPwmAndCounter.cs
//
// Enables a 10 kHz PWM output and high speed counter, waits 1 second and
// reads the counter. If you jumper the counter to PWM, it should return
// around 10000.
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
                dio_ef_pwm_and_counter(handle, devType);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  // Pause for user
        }
        void dio_ef_pwm_and_counter(int handle, int devType)
        {
            int errorAddress = -1;
            int pwmDIO = 0, counterDIO = 0;
            string[] aNames;
            double[] aValues;
            int numFrames = 0;
            double counterVal = 0;
            int intervalHandle = 1;
            int skippedIntervals = 0;

            // Configure the PWM output and counter.
            switch(devType){
                // For the T4, use FIO6 (DIO6) for the PWM output
                // Use CIO2 (DIO18) for the high speed counter
                case LJM.CONSTANTS.dtT4:
                    pwmDIO = 6;
                    counterDIO = 18;
                    break;
                // For the T7, use FIO0 (DIO0) for the PWM output
                // Use CIO2 (DIO18) for the high speed counter
                case LJM.CONSTANTS.dtT7:
                    pwmDIO = 0;
                    counterDIO = 18;
                    break;
                // For the T8, use FIO7 (DIO7) for the PWM output
                // Use FIO6 (DIO6) for the high speed counter
                case LJM.CONSTANTS.dtT8:
                    pwmDIO = 7;
                    counterDIO = 6;
                    break;
            };

            // Set up for reading DIO state
            aNames = new string[] { "DIO_EF_CLOCK0_DIVISOR",
                                    "DIO_EF_CLOCK0_ROLL_VALUE",
                                    "DIO_EF_CLOCK0_ENABLE",
                                    String.Format("DIO{0}_EF_INDEX", pwmDIO),
                                    String.Format("DIO{0}_EF_CONFIG_A", pwmDIO),
                                    String.Format("DIO{0}_EF_ENABLE", pwmDIO),
                                    String.Format("DIO{0}_EF_INDEX", counterDIO),
                                    String.Format("DIO{0}_EF_ENABLE", counterDIO)
            };

            aValues = new double[] { 1,
                                    8000,
                                    1,
                                    0,
                                    2000,
                                    1,
                                    7,
                                    1
            };
            numFrames = aNames.Length;
            LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);
            // Start a 1 second interval
            LJM.StartInterval(intervalHandle, 1000000);

            // Wait until the 1 second interval is complete
            LJM.WaitForNextInterval(intervalHandle, ref skippedIntervals);
            if (skippedIntervals > 0)
            {
                Console.WriteLine("SkippedIntervals: " + skippedIntervals);
            }

            // Read from the counter.
            LJM.eReadName(handle, String.Format("DIO{0}_EF_READ_A", counterDIO), ref counterVal);
            Console.WriteLine("\nCounter - {0}", counterVal);

            // Turn off PWM output and counter
            aNames = new string[] { "DIO_EF_CLOCK0_ENABLE",
                                    String.Format("DIO{0}_EF_ENABLE", pwmDIO),
                                    String.Format("DIO{0}_EF_ENABLE", counterDIO),
            };

            aValues = new double[] {
                0,
                0,
                0
            };
            numFrames = aNames.Length;
            LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);
        }
    }
}
