//-----------------------------------------------------------------------------
// ThermocoupleExample.cs
//
// Demonstrates thermocouple configuration and measurement.
// This example demonstrates usage of the thermocouple AIN_EF (T7/T8 only)
// and a solution using our LJTick-InAmp (commonly used with the T4).
//
// Thermocouple App-Note:
//      https://labjack.com/support/app-notes/thermocouples
//
// LJM Library:
//  LJM Library Installer:
//      https://labjack.com/support/software/installers/ljm
//  LJM Users Guide:
//      https://labjack.com/support/software/api/ljm
//  Opening and Closing:
//      https://labjack.com/support/software/api/ljm/function-reference/opening-and-closing
//  Single Value Functions(such as eReadName):
//      https://labjack.com/support/software/api/ljm/function-reference/single-value-functions
//  TCVoltsToTemp:
//      https://labjack.com/support/software/api/ud/function-reference/tcvoltstotemp
//
// T-Series and I/O:
//  Modbus Map:
//      https://labjack.com/support/software/api/modbus/modbus-map
//  Analog Inputs:
//      https://labjack.com/support/datasheets/t-series/ain
//  Thermocouple AIN_EF:
//      https://labjack.com/support/datasheets/t-series/ain/extended-features/thermocouple
//
// support@labjack.com
//-----------------------------------------------------------------------------

using System;
using LabJack;

namespace ThermocoupleExample
{
    class ThermocoupleExample
    {
        //Supported TC types are:
        //    LJM_ttB (val=6001)
        //    LJM_ttE (val=6002)
        //    LJM_ttJ (val=6003)
        //    LJM_ttK (val=6004)
        //    LJM_ttN (val=6005)
        //    LJM_ttR (val=6006)
        //    LJM_ttS (val=6007)
        //    LJM_ttT (val=6008)
        //    LJM_ttC (val=6009)
        //Note that the values above do not align with the AIN_EF index values
        //or order. In this example, we demonstrate a lookup table to convert
        //our thermocouple constant to the correct index when using the AIN_EF.
        private int tcType = LJM.CONSTANTS.ttK;

        //If taking a differential reading on a T7, posChannel should be an even
        //numbered AIN connecting signal+, and signal- should be connected to
        //the positive AIN channel plus one.
        //Example: signal+=channel=0 (AIN0), signal-=negChannel=1 (AIN1)
        private int channel = 0;

        //Modbus address to read the CJC sensor at.
        private int CJCAddress = 60052;

        //Slope of CJC voltage to Kelvin conversion (K/volt). TEMPERATURE_DEVICE_K
        //returns temp in K, so this would be set to 1 if using it for CJC. If
        //using an LM34 on some AIN for CJC, this config should be 55.56.
        private float CJCSlope = 1;

        //Offset for CJC temp (in Kelvin).This would normally be 0 if reading the
        //register TEMPERATURE_DEVICE_K for CJC. If using an InAmp or expansion
        //board, the CJ might be a bit cooler than the internal temp sensor, so
        //you might adjust the offset down a few degrees. If using an LM34 on some
        //AIN for CJC, this config should be 255.37.
        private float CJCOffset = 0;

        public enum TempUnits {
            DEGK = 'K',
            DEGC = 'C',
            DEGF = 'F',
        }

        private TempUnits tempUnits = TempUnits.DEGC;

        public void InitTCData(int tcType,
            int channel,
            int CJCAddress,
            float CJCSlope,
            float CJCOffset,
            TempUnits tempUnits)
        {
            this.tcType = tcType;
            this.channel = channel;
            this.CJCAddress = CJCAddress;
            this.CJCSlope = CJCSlope;
            this.CJCOffset = CJCOffset;
            this.tempUnits = tempUnits;
        }

        public void SetupAIN_EF(int handle)
        {
            try
            {
                //For converting LJM TC type constant to TC AIN_EF index.
                //Thermocouple type:    B  E  J  K  N  R  S  T  C
                int[] TC_INDEX_LUT = {28, 20, 21, 22, 27, 23, 25, 24, 30};
                const int NUM_FRAMES = 5;
                int[] aAddresses = new int[NUM_FRAMES];
                int[] aTypes = new int[NUM_FRAMES];
                double[] aValues = new double[NUM_FRAMES];
                int errorAddress = -1;

                //For setting up the AIN#_EF_INDEX (thermocouple type)
                aAddresses[0] = 9000+2*this.channel;
                aTypes[0] = LJM.CONSTANTS.UINT32;
                aValues[0] = TC_INDEX_LUT[this.tcType - 6001];

                //For setting up the AIN#_EF_CONFIG_A (temperature units)
                aAddresses[1] = 9300+2*this.channel;
                aTypes[1] = LJM.CONSTANTS.UINT32;
                switch (this.tempUnits) {
                    case TempUnits.DEGK:
                        aValues[1] = 0;
                        break;
                    case TempUnits.DEGC:
                        aValues[1] = 1;
                        break;
                    case TempUnits.DEGF:
                        aValues[1] = 2;
                        break;
                    default:
                        aValues[1] = 0;
                        break;
                }

                //For setting up the AIN#_EF_CONFIG_B (CJC address)
                aAddresses[2] = 9600+2*this.channel;
                aTypes[2] = LJM.CONSTANTS.UINT32;
                aValues[2] = this.CJCAddress;

                //For setting up the AIN#_EF_CONFIG_D (CJC slope)
                aAddresses[3] = 10200+2*this.channel;
                aTypes[3] = LJM.CONSTANTS.FLOAT32;
                aValues[3] = this.CJCSlope;

                //For setting up the AIN#_EF_CONFIG_E (CJC offset)
                aAddresses[4] = 10500+2*this.channel;
                aTypes[4] = LJM.CONSTANTS.FLOAT32;
                aValues[4] = this.CJCOffset;

                LJM.eWriteAddresses(handle, NUM_FRAMES, aAddresses, aTypes,
                    aValues, ref errorAddress);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Exception in ThermocoupleExample.SetupAIN_EF");
                throw e;
            }
        }

        public void GetReadingsInAmp(int handle, double inAmpOffset, int inAmpGain)
        {
            try
            {
                double TCTemp = 0, TCVolts = 0, CJTemp = 0;
                LJM.eReadAddress(handle, 2*this.channel, LJM.CONSTANTS.FLOAT32, ref TCVolts);

                //Account for LJTick-InAmp scaling
                TCVolts = (TCVolts - inAmpOffset) / inAmpGain;

                LJM.eReadAddress(handle, this.CJCAddress, LJM.CONSTANTS.FLOAT32, ref CJTemp);

                //Apply scaling to CJC reading if necessary.
                //At this point, the reading must be in units Kelvin.
                CJTemp = CJTemp * this.CJCSlope + this.CJCOffset;

                //Convert voltage reading to the thermocouple temperature.
                LJM.TCVoltsToTemp(this.tcType, TCVolts, CJTemp, ref TCTemp);;

                //Convert to temp units for display
                switch (this.tempUnits){
                    case TempUnits.DEGK:
                        //Nothing to do
                        break;
                    case TempUnits.DEGC:
                        TCTemp = TCTemp-273.15;
                        CJTemp = CJTemp-273.15;
                        break;

                    case TempUnits.DEGF:
                        TCTemp = (1.8*TCTemp)-459.67;
                        CJTemp = (1.8*CJTemp)-459.67;
                        break;
                    default:
                        //Assume DEGK. Nothing to do.
                        break;
                }
                Console.WriteLine("TCTemp: {0:N4} {1},\t TCVolts: {2:N4},\tCJTemp: {3:N4} {4}",
                    TCTemp, (char)this.tempUnits, TCVolts, CJTemp, (char)this.tempUnits);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Exception in ThermocoupleExample.GetReadingsInAmp");
                throw e;
            }
        }

        public void GetReadingsAIN_EF(int handle)
        {
            try
            {
                double TCTemp = 0, TCVolts = 0, CJTemp = 0;

                LJM.eReadAddress(handle, 7300+2*this.channel, LJM.CONSTANTS.FLOAT32, ref TCVolts);

                LJM.eReadAddress(handle, 7600+2*this.channel, LJM.CONSTANTS.FLOAT32, ref CJTemp);

                LJM.eReadAddress(handle, 7000+2*this.channel, LJM.CONSTANTS.FLOAT32, ref TCTemp);

                Console.WriteLine("TCTemp: {0:N4} {1},\t TCVolts: {2:N4},\tCJTemp: {3:N4} {4}",
                    TCTemp, (char)this.tempUnits, TCVolts, CJTemp, (char)this.tempUnits);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Exception in ThermocoupleExample.GetReadingsAIN_EF");
                throw e;
            }
        }

        static void Main(string[] args)
        {
            ThermocoupleExample tce = new ThermocoupleExample();
            tce.performActions();
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
            int intervalHandle = 1;
            int skippedIntervals = 0;
            int cjcAddr = 60052;  //Default CJC address (TEMPERATURE_DEVICE_K)

            try
            {
                int posChannel = 0;
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

                if (devType == LJM.CONSTANTS.dtT8)
                {
                    //Using the TEMPERATURE# address (based on AIN#) for CJC
                    //if using the T8.
                    cjcAddr = 600 + 2*posChannel;
                }


                InitTCData(
                    LJM.CONSTANTS.ttK,  //Type K thermocouple
                    posChannel,         //Measured at AIN# where #=posChannel
                    cjcAddr,            //Using TEMPERATURE_DEVICE_K for CJC
                    1,                  //CJCSlope=1 using internal temp sensor
                    0,                  //CJCOffset=0 using internal temp sensor
                    TempUnits.DEGC);    //Display measurements in units Celcius

                //Set the resolution index to the default setting (value=0)
                //Default setting has different meanings depending on the device.
                //See our AIN documentation (link above) for more information.
                LJM.eWriteAddress(handle, 41500+posChannel, LJM.CONSTANTS.UINT16, 0);

                if (devType != LJM.CONSTANTS.dtT4)
                {
                    //T7 and T8 configuration. Setup the AIN_EF.
                    SetupAIN_EF(handle);

                    //Only set up the negative channel config if using a T7.
                    //Set up a single ended measurement (AIN#_NEGATIVE_CH = GND).
                    //If taking a differential reading on a T7, channel should be an even
                    //numbered AIN connecting signal+, and signal- should be connected to
                    //the positive AIN channel plus one.
                    //Example: signal+=channel=0 (AIN0), signal-=negChannel=1 (AIN1)
                    if (devType == LJM.CONSTANTS.dtT7)
                    {
                        double negChannel = LJM.CONSTANTS.GND;
                        LJM.eWriteAddress(handle, 41000+posChannel,
                            LJM.CONSTANTS.UINT16, negChannel);
                    }
                }

                Console.WriteLine("\nStarting read loop.  Press a key to stop.");
                LJM.StartInterval(intervalHandle, 1000000);
                while (!Console.KeyAvailable)
                {
                    if (devType != LJM.CONSTANTS.dtT4)
                    {
                        //Read with AIN_EF if T7/T8
                        GetReadingsAIN_EF(handle);
                    }
                    else
                    {
                        //Assume usage of an InAmp if using a T4
                        GetReadingsInAmp(handle, 0.4, 51);
                    }
                    LJM.WaitForNextInterval(intervalHandle, ref skippedIntervals);  //Wait 1 second
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

            //Close interval and all device handles
            LJM.CleanInterval(intervalHandle);
            LJM.CloseAll();

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
