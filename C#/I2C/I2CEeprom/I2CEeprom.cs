//-----------------------------------------------------------------------------
// I2CEeprom.cs
//
// Demonstrates I2C communication using the LJM driver. The demonstration uses
// a LJTick-DAC connected to FIO0/FIO1, configures I2C settings, and reads,
// writes and reads bytes from/to the EEPROM.
//
// support@labjack.com
// Nov. 26, 2013
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

            try
            {
                //Open first found LabJack
                LJM.OpenS("ANY", "ANY", "ANY", ref handle);
                //devType = LJM.CONSTANTS.dtANY; //Any device type
                //conType = LJM.CONSTANTS.ctANY; //Any connection type
                //LJM.Open(devType, conType, "ANY", ref handle);
                
                LJM.GetHandleInfo(handle, ref devType, ref conType, ref serNum, ref ipAddr, ref port, ref maxBytesPerMB);
                LJM.NumberToIP(ipAddr, ref ipAddrStr);
                Console.WriteLine("Opened a LabJack with Device type: " + devType + ", Connection type: " + conType + ",");
                Console.WriteLine("Serial number: " + serNum + ", IP address: " + ipAddrStr + ", Port: " + port + ",");
                Console.WriteLine("Max bytes per MB: " + maxBytesPerMB);


	            //Configure the I2C communication.
	            LJM.eWriteName(handle, "I2C_SDA_DIONUM", 1); //SDA pin number = 1 (FIO1)
                
                LJM.eWriteName(handle, "I2C_SCL_DIONUM", 0); //SCA pin number = 0 (FIO0)
                
                //Speed throttle is inversely proportional to clock
	            //frequency. 0 = max.
                LJM.eWriteName(handle, "I2C_SPEED_THROTTLE", 0); //Speed throttle = 0
				
                //Options bits:
				//  bit0: Reset the I2C bus.
				//  bit1: Restart w/o stop
				//  bit2: Disable clock stretching.
                LJM.eWriteName(handle, "I2C_OPTIONS", 0); //Options = 0
	            
                LJM.eWriteName(handle, "I2C_SLAVE_ADDRESS", 80); //Slave Address of the I2C chip = 80 (0x50)

	
	            //Initial read of EEPROM bytes 0-3 in the user memory area. We
	            //need a single I2C transmission that writes the chip's memory
                //pointer and then reads the data.
	            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1); //Set the number of bytes to transmit
	            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4); //Set the number of bytes to receive

	            string[] aNames = new string[1];
	            int[] aWrites = new int[1];
	            int[] aNumValues = new int[1];
	            double[] aValues = new double[5]; //TX/RX bytes will go here
                int errorAddress = -1;

	            //Set the TX bytes. We are sending 1 byte for the address.
                aNames[0] = "I2C_WRITE_DATA";
                aWrites[0] = LJM.CONSTANTS.WRITE; //Indicates we are writing the values.
	            aNumValues[0] = 1; //The number of bytes
	            aValues[0] = 0; //Byte 0: Memory pointer = 0
	            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, ref errorAddress);

	            LJM.eWriteName(handle, "I2C_GO", 1); //Do the I2C communications.

	            //Read the RX bytes.
                aNames[0] = "I2C_READ_DATA";
	            aWrites[0] = LJM.CONSTANTS.READ; //Indicates we are reading the values.
	            aNumValues[0] = 4; //The number of bytes
	            //aValues[0] to aValues[3] will contain the data
	            for(int i = 0; i < 4; i++)
		            aValues[i] = 0;
	            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, ref errorAddress);
	            
	            Console.Write("\nRead User Memory [0-3] = ");
	            for(int i = 0; i < 4; i++)
		            Console.Write(aValues[i] + " ");
	            Console.WriteLine("");

	
	            //Write EEPROM bytes 0-3 in the user memory area, using the
                //page write technique.  Note that page writes are limited to
                //16 bytes max, and must be aligned with the 16-byte page
                //intervals.  For instance, if you start writing at address 14,
                //you can only write two bytes because byte 16 is the start of
                //a new page.
	            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 5); //Set the number of bytes to transmit
	            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 0); //Set the number of bytes to receive

	            //Set the TX bytes.
                aNames[0] = "I2C_WRITE_DATA";
	            aWrites[0] = LJM.CONSTANTS.WRITE; //Indicates we are writing the values.
	            aNumValues[0] = 5; //The number of bytes
	            aValues[0] = 0; //Byte 0: Memory pointer = 0
	            //Create 4 new random numbers to write (aValues[1-4]).
                Random rand = new Random();
	            for(int i = 1; i < 5; i++)
		            aValues[i] = Convert.ToDouble(rand.Next(255)); //0 to 255
	            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, ref errorAddress);
    	        
	            LJM.eWriteName(handle, "I2C_GO", 1); //Do the I2C communications.

                Console.Write("Write User Memory [0-3] = ");
	            for(int i = 1; i < 5; i++)
	            {
		            Console.Write(aValues[i] + " ");
	            }
	            Console.WriteLine("");


	            //Final read of EEPROM bytes 0-3 in the user memory area. We
	            //need a single I2C transmission that writes the address and
                //then reads the data.
	            LJM.eWriteName(handle, "I2C_NUM_BYTES_TX", 1); //Set the number of bytes to transmit
	            LJM.eWriteName(handle, "I2C_NUM_BYTES_RX", 4); //Set the number of bytes to receive

	            //Set the TX bytes. We are sending 1 byte for the address.
                aNames[0] = "I2C_WRITE_DATA";
                aWrites[0] = LJM.CONSTANTS.WRITE; //Indicates we are writing the values.
	            aNumValues[0] = 1; //The number of bytes
	            aValues[0] = 0; //Byte 0: Memory pointer = 0
	            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, ref errorAddress);
    	        
	            LJM.eWriteName(handle, "I2C_GO", 1); //Do the I2C communications.
    	        
	            //Read the RX bytes.
                aNames[0] = "I2C_READ_DATA";
	            aWrites[0] = LJM.CONSTANTS.READ; //Indicates we are reading the values.
	            aNumValues[0] = 4; //The number of bytes
	            //aValues[0] to aValues[3] will contain the data
	            for(int i = 0; i < 4; i++)
		            aValues[i] = 0;
	            LJM.eNames(handle, 1, aNames, aWrites, aNumValues, aValues, ref errorAddress);

	            Console.Write("Read User Memory [0-3] = ");
	            for(int i = 0; i < 4; i++)
	                Console.Write(aValues[i] + " ");
	            Console.WriteLine("");
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll(); //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine(); //Pause for user
        }
    }
}
