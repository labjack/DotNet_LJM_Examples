//-----------------------------------------------------------------------------
// Asynch.cs
//
// Simple Asynch example uses the first found device and 9600/8/N/1.
// Does a write, waits 1 second, then returns whatever was read in that
// time. If you short RX to TX, then you will read back the same bytes
// that you write.
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
//     eWriteName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritename
//     Multiple Value Functions(such as eWriteNameArray and eReadNameArray):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
//     Asynchronous Serial:
//         https://labjack.com/support/datasheets/t-series/digital-io/asynchronous-serial
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace Asynch
{
    class Asynch
    {
        static void Main(string[] args)
        {
            Asynch asyn = new Asynch();
            asyn.performActions();
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
            double [] aBytes = new double[] {0x12, 0x34, 0x56, 0x78};
            int errorAddress = -1;
            int rxDIONum;
            int txDIONum;

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
                Console.WriteLine("");

                //Configure the Asynch communication.
                LJM.eWriteName(handle, "ASYNCH_ENABLE", 0);
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //Configure FIO4 and FIO5 as digital I/O.
                    LJM.eWriteName(handle, "DIO_INHIBIT", 0xFFFCF);
                    LJM.eWriteName(handle, "DIO_ANALOG_ENABLE", 0x00000);

                    //For the T4, using FIO4 and FIO5 for TX and RX pins.
                    //FIO0 to FIO3 are reserved for analog inputs, and digital
                    //lines are required.
                    rxDIONum = 4;
                    txDIONum = 5;
                }
                else
                {
                    //For the T7 and T8, using FIO0 and FIO1 for the
                    //RX and TX pins.
                    rxDIONum = 0;
                    txDIONum = 1;
                }
                Console.Write("Short FIO{0} and FIO{1} together to read back the same bytes:\n\n", rxDIONum, txDIONum);
                LJM.eWriteName(handle, "ASYNCH_RX_DIONUM", rxDIONum);
                LJM.eWriteName(handle, "ASYNCH_TX_DIONUM", txDIONum);
                LJM.eWriteName(handle, "ASYNCH_BAUD", 9600);
                LJM.eWriteName(handle, "ASYNCH_NUM_DATA_BITS", 8);
                LJM.eWriteName(handle, "ASYNCH_PARITY", 0);
                LJM.eWriteName(handle, "ASYNCH_NUM_STOP_BITS", 1);
                LJM.eWriteName(handle, "ASYNCH_ENABLE", 1);


                // Write
                Console.Write("Writing: ");
                Array.ForEach(aBytes, PrintAsByte);
                Console.WriteLine("");
                LJM.eWriteName(handle, "ASYNCH_NUM_BYTES_TX", aBytes.Length);
                LJM.eWriteNameArray(handle, "ASYNCH_DATA_TX", aBytes.Length, aBytes, ref errorAddress);

                LJM.eWriteName(handle, "ASYNCH_TX_GO", 1);

                System.Threading.Thread.Sleep(1000);

                // Read
                Array.Clear(aBytes, 0, aBytes.Length);
                LJM.eReadNameArray(handle, "ASYNCH_DATA_RX", aBytes.Length, aBytes, ref errorAddress);
                Console.Write("Read:    ");
                Array.ForEach(aBytes, PrintAsByte);
                Console.WriteLine("");
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }

        private void PrintAsByte(double value)
        {
            Console.Write(" 0x{0:x} ", (int)value);
        }
    }
}
