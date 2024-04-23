//-----------------------------------------------------------------------------
// I2CEeprom.cs
//
// Demonstrates I2C communication using a LabJack. The demonstration uses a
// LJTick-DAC connected to FIO0/FIO1 for the T8 and T7, or FIO4/FIO5 for the
// T4, and configures the I2C settings. Then a read, write and again a read are
// performed on the LJTick-DAC EEPROM.
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
//     Multiple Value Functions (such as eWriteNameByteArray and
//     eReadNameByteArray):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Digital I/O:
//         https://labjack.com/support/datasheets/t-series/digital-io
//     I2C:
//         https://labjack.com/support/datasheets/t-series/digital-io/i2c
//     LJTick-DAC:
//         https://labjack.com/support/datasheets/accessories/ljtick-dac
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace I2CEeprom
{
    class I2CEeprom
    {
        static void Main(string[] args)
        {
            I2CEeprom ie = new I2CEeprom();
            ie.performActions();
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
            int numBytes = 0;
            byte[] aBytes = new byte[5];  //TX/RX bytes go here. Sending/receiving 5 bytes max.
            int errorAddress = -1;
            Random rand = new Random();

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

                //Configure the I2C communication.
                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //Configure FIO4 and FIO5 as digital I/O.
                    LJM.eWriteName(handle, "DIO_INHIBIT", 0xFFFCF);
                    LJM.eWriteName(handle, "DIO_ANALOG_ENABLE", 0x00000);

                    //For the T4, using FIO4 and FIO5 for SCL and SDA pins.
                    //FIO0 to FIO3 are reserved for analog inputs, and digital
                    //lines are required.
                    LJM.eWriteName(handle, "I2C_SDA_DIONUM", 5);  //SDA pin number = 5 (FIO5)
                    LJM.eWriteName(handle, "I2C_SCL_DIONUM", 4);  //SCL pin number = 4 (FIO4)
                }
                else
                {
                    //For the T8, T7 and other devices, using FIO0 and FIO1
                    //for the SCL and SDA pins.
                    LJM.eWriteName(handle, "I2C_SDA_DIONUM", 1);  //SDA pin number = 1 (FIO1)
                    LJM.eWriteName(handle, "I2C_SCL_DIONUM", 0);  //SCL pin number = 0 (FIO0)
                }

                //Speed throttle is inversely proportional to clock frequency.
                //0 = max.
                LJM.eWriteName(handle, "I2C_SPEED_THROTTLE", 65516);  //Speed throttle = 65516 (~100 kHz)

                //Options bits:
                //  bit0: Reset the I2C bus.
                //  bit1: Restart w/o stop
                //  bit2: Disable clock stretching.
                LJM.eWriteName(handle, "I2C_OPTIONS", 0);  //Options = 0

                LJM.eWriteName(handle, "I2C_SLAVE_ADDRESS", 80);  //Slave Address of the I2C chip = 80 (0x50)

                //Initial read of EEPROM bytes 0-3 in the user memory area. We
                //need a single I2C transmission that writes the chip's memory
                //pointer and then reads the data.
                LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1);  //Set the number of bytes to transmit
                LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4);  //Set the number of bytes to receive

                //Set the TX bytes. We are sending 1 byte for the address.
                numBytes = 1;
                aBytes[0] = 0;  //Byte 0: Memory pointer = 0
                LJM.eWriteNameByteArray(handle, "I2C_DATA_TX", numBytes, aBytes, ref errorAddress);

                LJM.eWriteName(handle, "I2C_GO", 1);  //Do the I2C communications.

                //Read the RX bytes.
                numBytes = 4;  //The number of bytes
                //aBytes[0] to aBytes[3] will contain the data
                for (int i = 0; i < numBytes; i++)
                    aBytes[i] = 0;
                LJM.eReadNameByteArray(handle, "I2C_DATA_RX", numBytes, aBytes, ref errorAddress);

                Console.Write("\nRead User Memory [0-3] = ");
                for (int i = 0; i < numBytes; i++)
                    Console.Write(aBytes[i] + " ");
                Console.WriteLine("");

                //Write EEPROM bytes 0-3 in the user memory area, using the
                //page write technique.  Note that page writes are limited to
                //16 bytes max, and must be aligned with the 16-byte page
                //intervals.  For instance, if you start writing at address 14,
                //you can only write two bytes because byte 16 is the start of
                //a new page.
                LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 5);  //Set the number of bytes to transmit
                LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 0);  //Set the number of bytes to receive

                //Set the TX bytes.
                numBytes = 5;  //The number of bytes
                aBytes[0] = 0;  //Byte 0: Memory pointer = 0
                //Create 4 new random numbers to write (aBytes[1-4]).
                for (int i = 1; i < numBytes; i++)
                    aBytes[i] = Convert.ToByte(rand.Next(255));  //0 to 255
                LJM.eWriteNameByteArray(handle, "I2C_DATA_TX", numBytes, aBytes, ref errorAddress);

                LJM.eWriteName(handle, "I2C_GO", 1);  //Do the I2C communications.

                Console.Write("Write User Memory [0-3] = ");
                for (int i = 1; i < numBytes; i++)
                    Console.Write(aBytes[i] + " ");
                Console.WriteLine("");

                //Final read of EEPROM bytes 0-3 in the user memory area. We
                //need a single I2C transmission that writes the address and
                //then reads the data.
                LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1);  //Set the number of bytes to transmit
                LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4);  //Set the number of bytes to receive

                //Set the TX bytes. We are sending 1 byte for the address.
                numBytes = 1;  //The number of bytes
                aBytes[0] = 0;  //Byte 0: Memory pointer = 0
                LJM.eWriteNameByteArray(handle, "I2C_DATA_TX", numBytes, aBytes, ref errorAddress);

                LJM.eWriteName(handle, "I2C_GO", 1);  //Do the I2C communications.

                //Read the RX bytes.
                numBytes = 4;  //The number of bytes
                //aValues[0] to aValues[3] will contain the data
                for (int i = 0; i < numBytes; i++)
                    aBytes[i] = 0;
                LJM.eReadNameByteArray(handle, "I2C_DATA_RX", numBytes, aBytes, ref errorAddress);

                Console.Write("Read User Memory [0-3] = ");
                for (int i = 0; i < numBytes; i++)
                    Console.Write(aBytes[i] + " ");
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
    }
}
