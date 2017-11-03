//-----------------------------------------------------------------------------
// SPI.cs
// 
// Demonstrates SPI communication.
//
// You can short MOSI to MISO for testing.
//
// T7:
//     MOSI    FIO2
//     MISO    FIO3
//     CLK     FIO0
//     CS      FIO1
//
// T4:
//     MOSI    FIO6
//     MISO    FIO7
//     CLK     FIO4
//     CS      FIO5
//
// If you short MISO to MOSI, then you will read back the same bytes that you
// write.  If you short MISO to GND, then you will read back zeros.  If you
// short MISO to VS or leave it unconnected, you will read back 255s.
//
// support@labjack.com
//-----------------------------------------------------------------------------
using System;
using LabJack;


namespace SPI
{
    class SPI
    {
        static void Main(string[] args)
        {
            SPI spi = new SPI();
            spi.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            int handle = 0;
            int errAddr = -1;
            int devType = 0;
            int conType = 0;
            int serNum = 0;
            int ipAddr = 0;
            int port = 0;
            int maxBytesPerMB = 0;
            string ipAddrStr = "";
            int numBytes = 4;
            byte []aBytes = new byte[4];
            Random rand = new Random();

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
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);
                Console.Out.WriteLine("");

                if (devType == LJM.CONSTANTS.dtT4)
                {
                    //Configure FIO4 to FIO7 as digital I/O.
                    LJM.eWriteName(handle, "DIO_INHIBIT", 0xFFF0F);
                    LJM.eWriteName(handle, "DIO_ANALOG_ENABLE", 0x00000);

                    //Setting CS, CLK, MISO, and MOSI lines for the T4. FIO0
                    //to FIO3 are reserved for analog inputs, and SPI requires
                    //digital lines.
                    LJM.eWriteName(handle, "SPI_CS_DIONUM", 5);  //CS is FIO5
                    LJM.eWriteName(handle, "SPI_CLK_DIONUM", 4);  //CLK is FIO4
                    LJM.eWriteName(handle, "SPI_MISO_DIONUM", 7);  //MISO is FIO7
                    LJM.eWriteName(handle, "SPI_MOSI_DIONUM", 6);  //MOSI is FIO6
                }
                else
                {
                    //Setting CS, CLK, MISO, and MOSI lines for the T7 and
                    //other devices.
                    LJM.eWriteName(handle, "SPI_CS_DIONUM", 1);  //CS is FIO1
                    LJM.eWriteName(handle, "SPI_CLK_DIONUM", 0);  //CLK is FIO0
                    LJM.eWriteName(handle, "SPI_MISO_DIONUM", 3);  //MISO is FIO3
                    LJM.eWriteName(handle, "SPI_MOSI_DIONUM", 2);  //MOSI is FIO2
                }

                //Selecting Mode CPHA=1 (bit 0), CPOL=1 (bit 1)
                LJM.eWriteName(handle, "SPI_MODE", 3);

                //Speed Throttle:
                //Valid speed throttle values are 1 to 65536 where 0 = 65536.
                //Configuring Max. Speed (~800 kHz) = 0
                LJM.eWriteName(handle, "SPI_SPEED_THROTTLE", 0);

                //Options
                //bit 0:
                //    0 = Active low clock select enabled
                //    1 = Active low clock select disabled.
                //bit 1:
                //    0 = DIO directions are automatically changed
                //    1 = DIO directions are not automatically changed.
                //bits 2-3: Reserved
                //bits 4-7: Number of bits in the last byte. 0 = 8.
                //bits 8-15: Reserved

                //Enabling active low clock select pin
                LJM.eWriteName(handle, "SPI_OPTIONS", 0);

                //Read back and display the SPI settings
                string[] aNames = {"SPI_CS_DIONUM", "SPI_CLK_DIONUM",
                                   "SPI_MISO_DIONUM", "SPI_MOSI_DIONUM",
                                   "SPI_MODE", "SPI_SPEED_THROTTLE",
                                   "SPI_OPTIONS" };
                double[] aValues = new double[aNames.Length];
                LJM.eReadNames(handle, aNames.Length, aNames, aValues, ref errAddr);

                Console.WriteLine("SPI Configuration:");
                for (int i = 0; i < aNames.Length; i++)
                {
                    Console.WriteLine("  " + aNames[i] + " = " + aValues[i]);
                }

                //Write(TX)/Read(RX) 4 bytes
                numBytes = 4;  //Redundant but to reiterate 4 TX/RX bytes
                LJM.eWriteName(handle, "SPI_NUM_BYTES", numBytes);

                //Write the bytes
                for (int i = 0; i < numBytes; i++)
                {
                    aBytes[i] = Convert.ToByte(rand.Next(255));
                }
                LJM.eWriteNameByteArray(handle, "SPI_DATA_TX", numBytes, aBytes, ref errAddr);
                LJM.eWriteName(handle, "SPI_GO", 1);  //Do the SPI communications

                //Display the bytes written
                Console.WriteLine("");
                for (int i = 0; i < numBytes; i++)
                {
                    Console.Out.WriteLine("dataWrite[" + i + "] = " + aBytes[i]);
                }

                //Read the bytes
                //Initialize byte array values to zero
                for (int i = 0; i < numBytes; i++)
                {
                    aBytes[i] = 0;
                }
                LJM.eReadNameByteArray(handle, "SPI_DATA_RX", numBytes, aBytes, ref errAddr);
                LJM.eWriteName(handle, "SPI_GO", 1);  //Do the SPI communications

                //Display the bytes read
                Console.Out.WriteLine("");
                for (int i = 0; i < numBytes; i++)
                {
                    Console.Out.WriteLine("dataRead[" + i + "] = " + aBytes[i]);
                }
            }
            catch (LJM.LJMException ljme)
            {
                showErrorMessage(ljme);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
