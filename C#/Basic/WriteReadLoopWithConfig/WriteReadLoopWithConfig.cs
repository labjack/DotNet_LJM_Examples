//-----------------------------------------------------------------------------
// WriteReadLoopWithConfig.cs
//
// Performs an initial call to eWriteNames to write configuration values, and
// then calls eWriteNames and eReadNames repeatedly in a loop.
//
// For documentation on register names to use, see the T-series Datasheet or
// the Modbus Map:
//
// https://labjack.com/support/datasheets/t-series
// https://labjack.com/support/software/examples/modbus/modbus-map
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace WriteReadLoopWithConfig
{
    class WriteReadLoopWithConfig
    {
        static void Main(string[] args)
        {
            WriteReadLoopWithConfig wrlwc = new WriteReadLoopWithConfig();
            wrlwc.performActions();
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
            int numFrames = 0;
            string[] aNames;
            double[] aValues;
            int errorAddress = -1;
            int intervalHandle = 1;
            int skippedIntervals = 0;

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);  // Any device, Any connection, Any identifier
                //LJM.OpenS("T7", "ANY", "ANY", ref handle);  // T7 device, Any connection, Any identifier
                //LJM.OpenS("T4", "ANY", "ANY", ref handle);  // T4 device, Any connection, Any identifier
                //LJM.Open(LJM.CONSTANTS.dtANY, LJM.CONSTANTS.ctANY, "ANY", ref handle);  // Any device, Any connection, Any identifier

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("  Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("  Max bytes per MB: " + maxBytesPerMB);

                //Setup and call eWriteNames to configure AIN0 (all devices)
                //and digital I/O (T4 only)
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //LabJack T4 configuration

                    //Set FIO5 (DIO5) and FIO6 (DIO6) lines to digital I/O.
                    //    DIO_INHIBIT = 0xF9F, b111110011111.
                    //                  Update only DIO5 and DIO6.
                    //    DIO_ANALOG_ENABLE = 0x000, b000000000000.
                    //                        Set DIO5 and DIO6 to digital I/O (b0).
                    aNames = new string[] { "DIO_INHIBIT", "DIO_ANALOG_ENABLE" };
                    aValues = new double[] { 0xF9F, 0x000 };
                    numFrames = aNames.Length;
                    LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                    //AIN0:
                    //    The T4 only has single-ended analog inputs.
                    //    The range of AIN0-AIN3 is +/-10 V.
                    //    The range of AIN4-AIN11 is 0-2.5 V.
                    //    Resolution index = 0 (default)
                    //    Settling = 0 (auto)
                    aNames = new string[] { "AIN0_RESOLUTION_INDEX",
                        "AIN0_SETTLING_US" };
                    aValues = new double[] { 0, 0 };
                }
                else
                {
                    //LabJack T7 and other devices configuration

                    //AIN0:
                    //    Negative Channel = 199 (Single-ended)
                    //    Range = +/-10 V
                    //    Resolution index = 0 (default).
                    //    Settling = 0 (auto)
                    aNames = new string[] { "AIN0_NEGATIVE_CH", "AIN0_RANGE",
                        "AIN0_RESOLUTION_INDEX", "AIN0_SETTLING_US" };
                    aValues = new double[] { 199, 10, 0, 0 };
                }
                numFrames = aNames.Length;
                LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                Console.WriteLine("\nSet configuration:");
                for (int i = 0; i < numFrames; i++)
                {
                    Console.WriteLine("  " + aNames[i] + " : " + aValues[i]);
                }

                int it = 0;
                double dacVolt = 0.0;
                int fioState = 0;
                Console.WriteLine("\nStarting read loop.  Press a key to stop.");
                LJM.StartInterval(intervalHandle, 1000000);
                while (!Console.KeyAvailable)
                {
                    //Setup and call eWriteNames to write to DAC0, and FIO5 (T4)
                    //or FIO1 (T7 and other devices).
                    //DAC0 will cycle ~0.0 to ~5.0 volts in 1.0 volt increments.
                    //FIO5/FIO1 will toggle output high (1) and low (0) states.
                    if (devType == LJM.CONSTANTS.dtT4)
                    {
                        aNames = new string[] { "DAC0", "FIO5" };
                    }
                    else
                    {
                        aNames = new string[] { "DAC0", "FIO1" };
                    }
                    dacVolt = it % 6.0;  //0-5
                    fioState = it % 2;  //0 or 1
                    aValues = new double[] { dacVolt, (double)fioState };
                    numFrames = aNames.Length;
                    LJM.eWriteNames(handle, numFrames, aNames, aValues, ref errorAddress);

                    Console.Write("\neWriteNames :");
                    for (int i = 0; i < numFrames; i++)
                        Console.Write(" " + aNames[i] + " = " + aValues[i].ToString("F4") + ", ");
                    Console.WriteLine("");

                    //Setup and call eReadNames to read AIN0, and FIO6 (T4) or
                    //FIO2 (T7 and other devices).
                    if (devType == LJM.CONSTANTS.dtT4)
                    {
                        aNames = new string[] { "AIN0", "FIO6" };
                    }
                    else
                    {
                        aNames = new string[] { "AIN0", "FIO2" };
                    }
                    aValues = new double[] { 0, 0 };
                    numFrames = aNames.Length;
                    LJM.eReadNames(handle, numFrames, aNames, aValues, ref errorAddress);
                    Console.Write("eReadNames  :");
                    for (int i = 0; i < numFrames; i++)
                        Console.Write(" " + aNames[i] + " = " + aValues[i].ToString("F4") + ", ");
                    Console.WriteLine("");

                    it++;

                    //Wait for next 1 second interval
                    LJM.WaitForNextInterval(intervalHandle, ref skippedIntervals);
                    if (skippedIntervals > 0)
                    {
                        Console.WriteLine("SkippedIntervals: " + skippedIntervals);
                    }
                }
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            //Close interval and device handles
            LJM.CleanInterval(intervalHandle);
            LJM.CloseAll();

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
