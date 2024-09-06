//-----------------------------------------------------------------------------
// DioEFConfigCounter.cs
//
// Enables an Interrupt Counter measurement to rising edges and a 10 Hz square wave on DAC1.
// To measure the rising edges on DAC1, connect a jumper between DAC1 and FIO0 on T7/T8 or FIO4 on T4.
//
// The Interrupt Counter counts the rising edge of pulses on the associated IO line.
// This interrupt-based digital I/O extended feature (DIO-EF) is not purely implemented in hardware, but rather firmware must service each edge.
//
// This example will read the DAC1 rising edge count at 1 second intervals 5 times.
// Then the count will be read and reset, and after 1 second, the count is read again.
//
// For more information on the Interrupt Counter DIO_EF mode see section 13.2.9 of the T-Series Datasheet.
// https://support.labjack.com/docs/13-2-9-interrupt-counter-t-series-datasheet
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using System.Threading;
using LabJack;


namespace DioEFConfigCounter
{
    class DioEFConfigCounter
    {
        static void Main(string[] args)
        {
            DioEFConfigCounter dioef = new DioEFConfigCounter();
            dioef.ConfigureCounter();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void ConfigureCounter()
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


                // --- Interrupt Counter ---
                int errorAddress = -1;
                int counterDIO = 0;   // DIO Pin that will measure the signal, T7/T8 use FIO0, T4 use FIO4.
                string[] aNames;
                double[] aValues;
                int numFrames = 0;


                // Selecting a specific DIO# Pin is necessary for each T-Series Device, only specific DIO# pins can do an Interrupt Counter measurement.
                // For detailed T-Series Device DIO_EF pin mapping tables see section 13.2 of the T-Series Datasheet:
                // https://support.labjack.com/docs/13-2-dio-extended-features-t-series-datasheet
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    // For the T4, use FIO4/DIO4 for the Interrupt Counter measurement.
                    counterDIO = 4;
                }

                /* --- How to Configure Interrupt Counter Measurment ---
                 * See Datasheet reference for DIO_EF Interrupt Counter:
                 * https://support.labjack.com/docs/13-2-9-interrupt-counter-t-series-datasheet
                 *
                 * -- Registers used for configuring DAC1 Frequency Out ---
                 * "DAC1_FREQUENCY_OUT_ENABLE": 0 = off, 1 = output 10 Hz signal on DAC1. The signal will be a square wave with peaks of 0 and 3.3V.
                 *
                 * --- Registers used for configuring Interrupt Counter ---
                 * "DIO#_EF_INDEX":            Sets desired DIO_EF feature, Interrupt Counter is DIO_EF Index 8.
                 * "DIO#_EF_ENABLE":           Enables/Disables the DIO_EF mode.
                 *
                 * Interrupt Counter counts the rising edge of pulses on the associated IO line.
                 * This interrupt-based digital I/O extended feature (DIO-EF) is not purely implemented in hardware, but rather firmware must service each edge.
                 *
                 * For a more detailed walkthrough see Configuring and Reading a Counter:
                 * https://support.labjack.com/docs/configuring-reading-a-counter
                 *
                 * For a more accurate measurement for counting Rising edges, use the hardware clocked High-Speed Counter mode.
                 * See the docs for High-Speed Counter here:
                 * https://support.labjack.com/docs/13-2-8-high-speed-counter-t-series-datasheet
                 */


                // Configure Interrupt Counter Registers
                LJM.eWriteName(handle, $"DIO{counterDIO}_EF_INDEX",  8);     // Set DIO#_EF_INDEX to 8 for Interrupt Counter.
                LJM.eWriteName(handle, "DAC1_FREQUENCY_OUT_ENABLE",  1);     // Enable 10 Hz square wave on DAC1.
                LJM.eWriteName(handle, $"DIO{counterDIO}_EF_ENABLE", 1);     // Enable the DIO#_EF Mode.

                Console.WriteLine($"\n--- Outputting a 10 Hz signal on DAC1, measuring signal on FIO{counterDIO} ---\n");

                /*
                 * --- How to read the measured count of rising edges? ---
                 * To read the count of Rising Edges, use the register below.
                 * DIO#_EF_READ_A: Returns the current Count.
                 *
                 * To read and reset the count:
                 * DIO#_EF_READ_A_AND_RESET: Reads the current count then clears the counter.
                 *
                 * Note that there is a brief period of time between reading and clearing during which edges can be missed.
                 * During normal operation this time period is 10-30 µs.
                 * If missed edges at this point can not be tolerated then reset should not be used.
                 */

                // If measuring at 1 second intervals, you should expect to see ~10 rising edges per second on the 10 Hz DAC1_FREQUENCY_OUT signal.

                double numRisingEdges = 0;
                double numRisingEdgesBeforeReset = 0;
                double numRisingEdgesAfterReset  = 0;

                // Read all of the measured values.
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(1000); // Sleep for 1 Second = 1000 ms.
                    LJM.eReadName(handle, $"DIO{counterDIO}_EF_READ_A", ref numRisingEdges);

                    Console.WriteLine($"DIO_EF Measured Values - Rising Edges: {numRisingEdges}");
                }

                Console.WriteLine($"\n--- Reading and Resetting the count of DIO{counterDIO} ---\n");

                LJM.eReadName(handle, $"DIO{counterDIO}_EF_READ_A_AND_RESET", ref numRisingEdgesBeforeReset);
                Thread.Sleep(1000); // Sleep for 1 Second = 1000 ms.
                LJM.eReadName(handle, $"DIO{counterDIO}_EF_READ_A_AND_RESET", ref numRisingEdgesAfterReset);

                Console.WriteLine($"DIO_EF Edges Before Read and Reset: {numRisingEdgesBeforeReset}");
                Console.WriteLine($"DIO_EF Edges After Read and Reset + 1 sec sleep: {numRisingEdgesAfterReset}");


                // Disable Counter and DAC1 Frequency Out.
                aNames = new string[]
                {
                    $"DIO{counterDIO}_EF_ENABLE",
                    "DAC1_FREQUENCY_OUT_ENABLE"
                };

                aValues = new double[] { 0, 0, 0 };
                numFrames = aNames.Length;
                Console.WriteLine("\n--- Disabling Interrupt Counter and DAC1_FREQUENCY_OUT ---");
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
