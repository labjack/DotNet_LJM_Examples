//-----------------------------------------------------------------------------
// OneWire.cs
//
// Demonstrates 1-Wire communication with a DS1822 sensor and a LabJack. This
// demonstration:
//   - Searches for and displays the ROM ID and path of the connected 1-Wire
//     device on EIO0.
//   - Reads temperature from a DS1822 sensor.
//
// support@labjack.com
//
// Relevant Documentation:
//
// LJM Library:
//     LJM Library Installer
//         https://labjack.com/support/software/installers/ljm
//     LJM Users Guide:
//         https://labjack.com/support/software/api/ljm
//     Opening and Closing:
//         https://labjack.com/support/software/api/ljm/function-reference/opening-and-closing
//     eWriteName:
//         https://labjack.com/support/software/api/ljm/function-reference/ljmewritename
//     Multiple Value Functions (such as eWriteNames, eReadNames,
//     eWriteNameByteArray and eReadNameByteArray):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//
// T-Series and I/O:
//     1-Wire:
//         https://labjack.com/support/datasheets/t-series/digital-io/1-wire
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace OneWire
{
    class OneWire
    {
        static void Main(string[] args)
        {
            OneWire oneWire = new OneWire();
            oneWire.performActions();
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
            string []aNames;
            double []aValues;
            int errorAddress = -1;
            byte []dataTX;
            byte []dataRX;

            try
            {
                // Open first found LabJack
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

                if (devType == LJM.CONSTANTS.dtT4)
                {
                    // Configure EIO0 as digital I/O.
                    LJM.eWriteName(handle, "DIO_INHIBIT", 0xFFEFF);
                    LJM.eWriteName(handle, "DIO_ANALOG_ENABLE", 0x00000);
                }

                // Configure 1-Wire pins and options.
                int dqPin = 8;  // EIO0
                int dpuPin = 0;  // Not used
                int options = 0;  // bit 2 = 0 (DPU disabled), bit 3 = 0 (DPU polarity low, ignored)
                aNames = new string[] {
                    "ONEWIRE_DQ_DIONUM",
                    "ONEWIRE_DPU_DIONUM",
                    "ONEWIRE_OPTIONS"
                };
                aValues = new double[] {
                    dqPin,
                    dpuPin,
                    options
                };
                LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                Console.WriteLine("\nUsing the DS1822 sensor with 1-Wire communications.");
                Console.WriteLine("  DQ pin = {0}", dqPin);
                Console.WriteLine("  DPU pin = {0}", dpuPin);
                Console.WriteLine("  Options  = {0}", options);

                // Search for the 1-Wire device and get its ROM ID and path.
                int function = 0xF0;  // Search
                int numTX = 0;
                int numRX = 0;
                aNames = new string[] {
                    "ONEWIRE_FUNCTION",
                    "ONEWIRE_NUM_BYTES_TX",
                    "ONEWIRE_NUM_BYTES_RX"
                };
                aValues = new double[] {
                    function,
                    numTX,
                    numRX
                };
                LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                LJM.eWriteName(handle, "ONEWIRE_GO", 1);
                aNames = new string[] {
                    "ONEWIRE_SEARCH_RESULT_H",
                    "ONEWIRE_SEARCH_RESULT_L",
                    "ONEWIRE_ROM_BRANCHS_FOUND_H",
                    "ONEWIRE_ROM_BRANCHS_FOUND_L"
                };
                aValues = new double[aNames.Length];
                LJM.eReadNames(handle, aNames.Length, aNames, aValues, ref errorAddress);

                uint romH = (uint)aValues[0];
                uint romL = (uint)aValues[1];
                ulong rom = ((ulong)romH<<32) + (ulong)romL;
                uint pathH = (uint)aValues[2];
                uint pathL = (uint)aValues[3];
                ulong path = ((ulong)pathH<<32) + (ulong)pathL;
                Console.WriteLine("  ROM ID = {0}", rom);
                Console.WriteLine("  Path = {0}", path);

                // Setup the binary temperature read.
                Console.WriteLine("Setup the binary temperature read.");
                function = 0x55;  // Match
                numTX = 1;
                dataTX = new byte[] { 0x44 };  // 0x44 = DS1822 Convert T command
                numRX = 0;
                aNames = new string[] {
                    "ONEWIRE_FUNCTION",
                    "ONEWIRE_NUM_BYTES_TX",
                    "ONEWIRE_NUM_BYTES_RX",
                    "ONEWIRE_ROM_MATCH_H",
                    "ONEWIRE_ROM_MATCH_L",
                    "ONEWIRE_PATH_H",
                    "ONEWIRE_PATH_L"
                };
                aValues = new double[] {
                    function,
                    numTX,
                    numRX,
                    romH,
                    romL,
                    pathH,
                    pathL
                };
                LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                LJM.eWriteNameByteArray(handle, "ONEWIRE_DATA_TX", numTX, dataTX, ref errorAddress);
                LJM.eWriteName(handle, "ONEWIRE_GO", 1);

                // Read the binary temperature.
                Console.WriteLine("Read the binary temperature.");
                function = 0x55;  // Match
                numTX = 1;
                dataTX = new byte[] {0xBE};  // 0xBE = DS1822 Read scratchpad command
                numRX = 2;
                aNames = new string[] {
                    "ONEWIRE_FUNCTION",
                    "ONEWIRE_NUM_BYTES_TX",
                    "ONEWIRE_NUM_BYTES_RX",
                    "ONEWIRE_ROM_MATCH_H",
                    "ONEWIRE_ROM_MATCH_L",
                    "ONEWIRE_PATH_H",
                    "ONEWIRE_PATH_L"
                };
                aValues = new double[] {
                    function,
                    numTX,
                    numRX,
                    romH,
                    romL,
                    pathH,
                    pathL
                };
                LJM.eWriteNames(handle, aNames.Length, aNames, aValues, ref errorAddress);
                LJM.eWriteNameByteArray(handle, "ONEWIRE_DATA_TX", numTX, dataTX, ref errorAddress);
                LJM.eWriteName(handle, "ONEWIRE_GO", 1);
                dataRX = new byte[numRX];
                LJM.eReadNameByteArray(handle, "ONEWIRE_DATA_RX", numRX, dataRX, ref errorAddress);

                double temperature = dataRX[0] + (dataRX[1]<<8);
                if ((int)temperature == 0x0550)
                {
                    Console.WriteLine("The DS1822 power on reset value is 85 C.");
                    Console.WriteLine("Read again get the real temperature.");
                }
                else
                {
                    temperature = temperature * 0.0625;
                    Console.WriteLine("Temperature = {0} C", temperature);
                }
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  // Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  // Pause for user
        }
    }
}
