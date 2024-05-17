//-----------------------------------------------------------------------------
// ThermocoupleExample.cs
//
// Demonstrates thermocouple configuration and measurement using the
// thermocouple AIN_EF (T7/T8 only) or our LJTick-InAmp (commonly used with the
// T4).
//
// support@labjack.com
//
// Relevant Documentation:
//
// Thermocouple App-Note:
//      https://labjack.com/support/app-notes/thermocouples
//
// LJM Library:
//     LJM Library Installer:
//         https://labjack.com/support/software/installers/ljm
//     LJM Users Guide:
//         https://labjack.com/support/software/api/ljm
//     Opening and Closing:
//         https://labjack.com/support/software/api/ljm/function-reference/opening-and-closing
//     Single Value Functions (such as eWriteName):
//         https://labjack.com/support/software/api/ljm/function-reference/single-value-functions
//     Multiple Value Functions (such as eWriteNames and eReadNames):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//     TCVoltsToTemp:
//         https://labjack.com/support/software/api/ud/function-reference/tcvoltstotemp
//     Timing Functions (such as StartInterval, WaitForNextInterval and
//     CleanInterval):
//        https://labjack.com/support/software/api/ljm/function-reference/timing-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     Analog Inputs:
//         https://labjack.com/support/datasheets/t-series/ain
//     Thermocouple AIN_EF:
//         https://labjack.com/support/datasheets/t-series/ain/extended-features/thermocouple
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

        //If taking a differential reading on a T7, posChannel should be an
        //even numbered AIN connecting signal+, and signal- should be connected
        //to the positive AIN channel plus one.
        //Example: signal+=channel=0 (AIN0), signal-=negChannel=1 (AIN1)
        private int channel = 0;

        //Modbus name and address to read the CJC sensor at. The InitTCData
        //method will set the address based on the name.
        private string CJCName = "TEMPERATURE_DEVICE_K";
        private int CJCAddress = 60052;

        //Slope of CJC voltage to Kelvin conversion (K/volt).
        //TEMPERATURE_DEVICE_K and TEMPERATURE# returns temp in K, so this
        //would be set to 1 if using it for CJC. If using an LM34 on some AIN
        //for CJC, this config should be 55.56.
        private float CJCSlope = 1;

        //Offset for CJC temp (in Kelvin). This would normally be 0 if reading
        //the register TEMPERATURE_DEVICE_K or TEMPERATURE# for CJC. If using
        //an InAmp or expansion board, the CJ might be a bit cooler than the
        //internal temp sensor, so you might adjust the offset down a few
        //degrees. If using an LM34 on some AIN for CJC, this config should
        //be 255.37.
        private float CJCOffset = 0;

        public enum TempUnits {
            DEGK = 'K',
            DEGC = 'C',
            DEGF = 'F',
        }

        private TempUnits tempUnits = TempUnits.DEGC;

        public void InitTCData(int tcType,
            int channel,
            string CJCName,
            float CJCSlope,
            float CJCOffset,
            TempUnits tempUnits)
        {
            this.tcType = tcType;
            this.channel = channel;
            this.CJCName = CJCName;

            //Getting CJCAddress from CJCName.
            int type = 0;
            LJM.NameToAddress(CJCName, ref this.CJCAddress, ref type);

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
                string[] aNames = new string[NUM_FRAMES];
                double[] aValues = new double[NUM_FRAMES];
                int errorAddress = -1;
                string channelName = "AIN" + this.channel;

                //For setting up the AIN#_EF_INDEX (thermocouple type)
                aNames[0] = channelName + "_EF_INDEX";
                aValues[0] = TC_INDEX_LUT[this.tcType - 6001];

                //For setting up the AIN#_EF_CONFIG_A (temperature units)
                aNames[1] = channelName + "_EF_CONFIG_A";
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
                aNames[2] = channelName + "_EF_CONFIG_B";
                aValues[2] = this.CJCAddress;

                //For setting up the AIN#_EF_CONFIG_D (CJC slope)
                aNames[3] = channelName + "_EF_CONFIG_D";
                aValues[3] = this.CJCSlope;

                //For setting up the AIN#_EF_CONFIG_E (CJC offset)
                aNames[4] = channelName + "_EF_CONFIG_E";
                aValues[4] = this.CJCOffset;

                LJM.eWriteNames(handle, NUM_FRAMES, aNames, aValues,
                    ref errorAddress);
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
                string[] aNames = {"AIN"+this.channel, this.CJCName};
                double[] aValues = new double[2];
                int errorAddress = 0;
                double TCTemp = 0, TCVolts = 0, CJTemp = 0;

                //Read the InAmp output voltage (connected to AIN#) and
                //the CJC temperature.
                LJM.eReadNames(handle, 2, aNames, aValues, ref errorAddress);

                //Convert the InAmp voltage to the raw thermocouple voltage.
                TCVolts = (aValues[0] - inAmpOffset) / inAmpGain;

                //Apply scaling to CJC reading if necessary.
                //At this point, the reading must be in units Kelvin.
                CJTemp = aValues[1] * this.CJCSlope + this.CJCOffset;

                //Convert voltage reading to the thermocouple temperature.
                LJM.TCVoltsToTemp(this.tcType, TCVolts, CJTemp, ref TCTemp);;

                //Convert to temperature units for display.
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

                Console.WriteLine("TCTemp: {0:N3} {1},\t TCVolts: {2:N6},\tCJTemp: {3:N3} {4}\n",
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
                string channelName = "AIN" + this.channel;
                string[] aNames = { 
                    channelName + "_EF_READ_A",
                    channelName + "_EF_READ_B",
                    channelName + "_EF_READ_C"
                };
                double[] aValues = new double[3];
                int errorAddress = 0;
                double TCTemp = 0, TCVolts = 0, CJTemp = 0;

                LJM.eReadNames(handle, 3, aNames, aValues, ref errorAddress);
                TCTemp = aValues[0];   //Read value from AIN#_EF_READ_A.
                TCVolts = aValues[1];  //Read value from AIN#_EF_READ_B
                CJTemp = aValues[2];   //Read value from AIN#_EF_READ_C

                Console.WriteLine("TCTemp: {0:N3} {1},\t TCVolts: {2:N6},\tCJTemp: {3:N3} {4}\n",
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
            string cjcName = "TEMPERATURE_DEVICE_K";  //Default CJC name

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
                    //Using the TEMPERATURE# name, where #=posChannel, for
                    //CJC if using the T8.
                    cjcName = "TEMPERATURE" + posChannel;
                }

                InitTCData(
                    LJM.CONSTANTS.ttK,  //Type K thermocouple
                    posChannel,         //Measured at AIN# where #=posChannel
                    cjcName,            //Using TEMPERATURE_DEVICE_K, or TEMPERATURE#, for CJC
                    1,                  //CJCSlope=1 using internal temp sensor
                    0,                  //CJCOffset=0 using internal temp sensor
                    TempUnits.DEGK);    //Display measurements in units Kelvin

                //Set the resolution index to the default setting (value=0)
                //Default setting has different meanings depending on the device.
                //See our AIN documentation (link above) for more information.
                LJM.eWriteName(handle, "AIN" + posChannel + "_RESOLUTION_INDEX", 0);
                if (devType != LJM.CONSTANTS.dtT4)
                {
                    //T7 and T8 configuration. Setup the AIN_EF.
                    SetupAIN_EF(handle);

                    //Set up any negative channel configurations required. The
                    //T8 inputs are isolated and therefore do not require any
                    //negative channel configuration.
                    if (devType == LJM.CONSTANTS.dtT7)
                    {
                        //There are only certain valid differential channel
                        //pairs. For AIN0-13 the valid pairs are an even
                        //numbered AIN and next odd AIN. For example,
                        //AIN0-AIN1, AIN2-AIN3. To take a differential reading
                        //between AIN0 and AIN1, set AIN0_NEGATIVE_CH to 1.

                        //Set up a single ended measurement.
                        double negChannel = LJM.CONSTANTS.GND;
                        LJM.eWriteName(handle, "AIN" + posChannel +"_NEGATIVE_CH",
                            negChannel);
                    }
                }

                Console.WriteLine("\nReading thermocouple temperature in a loop.  Press a key to stop.\n");
                LJM.StartInterval(intervalHandle, 1000000);
                while (!Console.KeyAvailable)
                {
                    if (devType != LJM.CONSTANTS.dtT4)
                    {
                        //Read with AIN_EF if using a T7/T8.
                        GetReadingsAIN_EF(handle);
                    }
                    else
                    {
                        //Read with InAmp with gain 201x and offset 1.25 V settings.
                        GetReadingsInAmp(handle, 1.25, 201);
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
            try
            {
                LJM.CleanInterval(intervalHandle);
            }
            catch (LJM.LJMException)
            {
                //Ignore invalid interval handle error
            }
            LJM.CloseAll();

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }
    }
}
