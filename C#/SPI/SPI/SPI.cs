//-----------------------------------------------------------------------------
// SPI.cs
// 
// Demonstrates SPI communication.
//
// You can short MOSI to MISO for testing.
//
// MOSI    FIO2
// MISO    FIO3
// CLK     FIO0
// CS      FIO1
//
// If you short MISO to MOSI, then you will read back the same bytes that you
// write.  If you short MISO to GND, then you will read back zeros.  If you
// short MISO to VS or leave it unconnected, you will read back 255s.
//
// support@labjack.com
// Nov. 19, 2013
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
            Console.Out.WriteLine(e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            try
            {
                int handle = 0;
                int errAddr = 0;


                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //int devType = LJM.CONSTANTS.dtANY; //Any device type
                //int conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);

                int devType = 0;
                int conType = 0;
                int serNum = 0;
                int ipAddr = 0;
                int port = 0;
                int maxBytesPerMB = 0;
                string ipAddrStr = "";

                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);
                Console.Out.WriteLine("");

                //CS is FIO1
                LJM.eWriteName(handle, "SPI_CS_DIONUM", 1);
                
                //CLK is FIO0
                LJM.eWriteName(handle, "SPI_CLK_DIONUM", 0);

                //MISO is FIO3
                LJM.eWriteName(handle, "SPI_MISO_DIONUM", 3);

                //MOSI is FIO2
                LJM.eWriteName(handle, "SPI_MOSI_DIONUM", 2);

                //Modes:
                //0 = A: CPHA=0, CPOL=0 
                //    Data clocked on the rising edge
                //    Data changed on the falling edge
                //    Final clock state low
                //    Initial clock state low
                //1 = B: CPHA=0, CPOL=1
                //    Data clocked on the falling edge
                //    Data changed on the rising edge
                //    Final clock state low
                //    Initial clock state low
                //2 = C: CPHA=1, CPOL=0 
                //    Data clocked on the falling edge
                //    Data changed on the rising edge
                //    Final clock state high
                //    Initial clock state high
                //3 = D: CPHA=1, CPOL=1 
                //    Data clocked on the rising edge
                //    Data changed on the falling edge
                //    Final clock state high
                //    Initial clock state high

                //Selecting Mode: A - CPHA=1, CPOL=1.
                LJM.eWriteName(handle, "SPI_MODE", 0);

                //Speed:
                //Frequency = 1000000000 / (175*(65536-Speed) + 1020)
                //Valid speed values are 1 to 65536 where 0 = 65536.
                //Note: The above equation and its frequency range
                //was tested for firmware 1.0009 and may change in
                //the future.

                //Configuring Max. Speed of about 980kHz.
                LJM.eWriteName(handle, "SPI_SPEED", 0);

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
                                   "SPI_MODE", "SPI_SPEED",
                                   "SPI_OPTIONS" };
                double[] aValues = new double[aNames.Length];
                LJM.eReadNames(handle, aNames.Length, aNames, aValues, ref errAddr);

                Console.WriteLine("SPI Configuration:");
                for (int i = 0; i < aNames.Length; i++)
                {
                    Console.WriteLine("  " + aNames[i] + " = " + aValues[i]);
                }


                //Write/Read 4 bytes
                const int numBytes = 4;
                LJM.eWriteName(handle, "SPI_NUM_BYTES", numBytes);


                //Setup write bytes
                double[] dataWrite = new double[numBytes];
                Random rand = new Random();
                for (int i = 0; i < numBytes; i++)
                {
                    dataWrite[i] = Convert.ToDouble(rand.Next(255));
                }
                int[] aAddresses = new int[1];
                int[] aTypes = new int[1];
                int[] aWrites = new int[1];
                int[] aNumValues = new int[1];
    
                //Write the bytes
                aAddresses[0] = 5010; //"SPI_DATA_WRITE"
                aTypes[0] = LJM.CONSTANTS.BYTE;
                aWrites[0] = LJM.CONSTANTS.WRITE;
                aNumValues[0] = numBytes;
                LJM.eAddresses(handle, 1, aAddresses, aTypes, aWrites, aNumValues, dataWrite, ref errAddr);

                //Display the bytes written
                Console.WriteLine("");
                for (int i = 0; i < numBytes; i++)
                {
                    Console.Out.WriteLine("dataWrite[" + i + "] = " + dataWrite[i]);
                }
   

                //Read the bytes
                double[] dataRead = new double[numBytes];
                aAddresses[0] = 5050; //"SPI_DATA_READ"
                aTypes[0] = LJM.CONSTANTS.BYTE;
                aWrites[0] = LJM.CONSTANTS.READ;
                aNumValues[0] = numBytes;
                LJM.eAddresses(handle, 1, aAddresses, aTypes, aWrites, aNumValues, dataRead, ref errAddr);

                //Display the bytes read
                Console.Out.WriteLine("");
                for (int i = 0; i < numBytes; i++)
                {
                    Console.Out.WriteLine("dataRead[" + i + "] = " + dataRead[i]);
                }
            }
            catch (LJM.LJMException ljme)
            {
                showErrorMessage(ljme);
            }

            LJM.CloseAll(); //Close all LabJack handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); // Pause for user
        }
    }
}
