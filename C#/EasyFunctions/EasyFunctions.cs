//-----------------------------------------------------------------------------
// EasyFunctions.cs
//
// Demonstrates easy functions usage. For eStream usage look in the Stream
// examples folder.
//
// support@labjack.com
// April 16, 2013
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace EasyFunctions
{
    class EasyFunctions
    {
        static void Main(string[] args)
        {
            EasyFunctions ef = new EasyFunctions();
            ef.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("Error: " + e.ToString());
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
                devType = LJM.CONSTANTS.dtANY; //Any device type
                conType = LJM.CONSTANTS.ctANY; //Any connection type
                LJM.Open(devType, conType, "ANY", ref handle);
                //LJM.OpenS("ANY", "ANY", "ANY", ref handle);

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);


                //Setup and call eWriteName to write a value.
                string name = "DAC0";
                double value = 2.5; //2.5 V
                LJM.eWriteName(handle, name, value);

                Console.WriteLine("\neWriteName: ");
                Console.WriteLine("  Name - " + name + ", value : " + value);


                //Setup and call eReadName to read a value.
                name = "SERIAL_NUMBER";
                LJM.eReadName(handle, name, ref value);

                Console.WriteLine("\neReadName result: ");
                Console.WriteLine("  Name - " + name + ", value : " + value);


                //Setup and call eWriteNames to write values.
                int numFrames = 2;
                string[] names = new string[2] {"DAC0", "TEST_UINT16"};
                double[] aValues = new double[2] {2.5, 12345}; // 2.5 V, 12345
                int errorAddress = 0;
                LJM.eWriteNames(handle, numFrames, names, aValues, ref errorAddress);

                Console.WriteLine("\neWriteNames:");
                for(int i = 0; i < numFrames; i++)
                    Console.WriteLine("  Name - " + names[i] + ", value : " + aValues[i]);


                //Setup and call eReadNames to read values.
                numFrames = 3;
                names = new string[3] {"SERIAL_NUMBER", "PRODUCT_ID", "FIRMWARE_VERSION"};
                aValues = new double[3] {0, 0, 0};
                errorAddress = 0;
                LJM.eReadNames(handle, numFrames, names, aValues, ref errorAddress);

                Console.WriteLine("\neReadNames results:");
                for(int i = 0; i < numFrames; i++)
                    Console.WriteLine("  Name - " + names[i] + ", value : " + aValues[i]);


                //Setup and call eNames to write/read values to/from the LabJack.
                numFrames = 3;
                names = new string[3] {"DAC0", "TEST_UINT16", "TEST_UINT16"};
                int[] aWrites = new int[3] {LJM.CONSTANTS.WRITE, LJM.CONSTANTS.WRITE, LJM.CONSTANTS.READ};
                int[] aNumValues = new int[3] {1, 1, 1};
                aValues = new double[3] {2.5, 12345, 0}; //write 2.5 V, write 12345, read
                errorAddress = 0;
                LJM.eNames(handle, numFrames, names, aWrites, aNumValues, aValues, ref errorAddress);

                Console.WriteLine("\neNames results:");
                for(int i = 0; i < numFrames; i++)
                    Console.WriteLine("  Names - " + names[i] + ", write -  " + aWrites[i] + ", values: " + aValues[i]);


                //Setup and call eWriteAddress to write a value.
                int address = 1000; // DAC0
                int type = LJM.CONSTANTS.FLOAT32;
                value = 2.5; // 2.5 V
                LJM.eWriteAddress(handle, address, type, value);

                Console.WriteLine("\neWriteAddress:");
                Console.WriteLine("  Address - " + address + ", data type - " + type +", value : " + value);


                //Setup and call eReadAddress to read a value.
                address = 60028; //Serial number
                type = LJM.CONSTANTS.UINT32;
                value = 0;
                LJM.eReadAddress(handle, address, type, ref value);

                Console.WriteLine("\neReadAddress result:");
                Console.WriteLine("  Address - " + address + ", data type - " + type +", value : " + value);


                //Setup and call eWriteAddresses to write values.
                numFrames = 2;
                int[] aAddresses = new int[2] {1000, 55110}; // DAC0, TEST_UINT16
                int[] aTypes = new int[2] {LJM.CONSTANTS.FLOAT32, LJM.CONSTANTS.UINT16};
                aValues = new double[2] {2.5, 12345}; // 2.5 V, 12345
                errorAddress = 0;
                LJM.eWriteAddresses(handle, numFrames, aAddresses, aTypes, aValues, ref errorAddress);

                Console.WriteLine("\neWriteAddresses:");
                for(int i = 0; i < numFrames; i++)
                   Console.WriteLine("  Address - " + aAddresses[i] + ", data type - " + aTypes[i] +", value : " + aValues[i]);


                //Setup and call eReadAddresses to read values.
                numFrames = 3;
                aAddresses = new int[3] {60028, 60000, 60004}; //serial number, product ID, firmware version
                aTypes = new int[3] {LJM.CONSTANTS.UINT32, LJM.CONSTANTS.FLOAT32, LJM.CONSTANTS.FLOAT32};
                aValues = new double[3] {0, 0, 0};
                errorAddress = 0;
                LJM.eReadAddresses(handle, numFrames, aAddresses, aTypes, aValues, ref errorAddress);

                Console.WriteLine("\neReadAddresses results:");
                for(int i = 0; i < numFrames; i++)
                   Console.WriteLine("  Address - " + aAddresses[i] + ", data type - " + aTypes[i] +", value : " + aValues[i]);


                //Setup and call eAddresses to write/read values.
                numFrames = 3;
                aAddresses = new int[3] {1000, 55110, 55110}; //DAC0, TEST_UINT16, TEST_UINT16
                aTypes = new int[3] {LJM.CONSTANTS.FLOAT32, LJM.CONSTANTS.UINT16, LJM.CONSTANTS.UINT16};
                aWrites = new int[3] {LJM.CONSTANTS.WRITE, LJM.CONSTANTS.WRITE, LJM.CONSTANTS.READ};
                aNumValues = new int[3] {1, 1, 1};
                aValues = new double[3] {2.5, 12345, 0}; //write 2.5 V, write 12345, read
                errorAddress = 0;
                LJM.eAddresses(handle, numFrames, aAddresses, aTypes, aWrites, aNumValues, aValues, ref errorAddress);

                Console.WriteLine("\neAddresses results:");
                for(int i = 0; i < numFrames; i++)
                    Console.WriteLine("  Address - " + aAddresses[i] + ", data type - " + aTypes[i] + ", write -  " + aWrites[i] + ", values: " + aValues[i]);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }

    }
}
