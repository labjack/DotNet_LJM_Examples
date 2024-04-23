//-----------------------------------------------------------------------------
// LuaExecutionControl.cs
//
// Example showing how to control lua script execution with an LJM host
// application.
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
//     Single Value Functions (such as eWriteName and eReadName):
//         https://labjack.com/support/software/api/ljm/function-reference/single-value-functions
//     Multiple Value Functions (such as eWriteNameByteArray and
//     eReadNameByteArray):
//         https://labjack.com/support/software/api/ljm/function-reference/multiple-value-functions
//
// T-Series and I/O:
//     Modbus Map:
//         https://labjack.com/support/software/api/modbus/modbus-map
//     User-RAM:
//         https://labjack.com/support/datasheets/t-series/lua-scripting
//-----------------------------------------------------------------------------
using System;
using LabJack;

namespace LuaExecutionControl
{
    class LuaExecutionControl
    {
        static void Main(string[] args)
        {
            LuaExecutionControl sAIN = new LuaExecutionControl();
            sAIN.performActions();
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
            double value=0;
            const string luaScript =
                "-- Use USER_RAM0_U16 (register 46180) to determine which control loop to run\n" +
                "local ramval = 0\n" +
                "MB.W(46180, 0, ramval)\n" +
                "local loop0 = 0\n" +
                "local loop1 = 1\n" +
                "local loop2 = 2\n" +
                "-- Setup an interval to control loop execution speed. Update every second\n" +
                "LJ.IntervalConfig(0,1000)\n" +
                "while true do\n" +
                "  if LJ.CheckInterval(0) then\n" +
                "    ramval = MB.R(46180, 0)\n" +
                "    if ramval == loop0 then\n" +
                "      print(\"using loop0\")\n" +
                "    end\n" +
                "    if ramval == loop1 then\n" +
                "      print(\"using loop1\")\n" +
                "    end\n" +
                "    if ramval == loop2 then\n" +
                "      print(\"using loop2\")\n" +
                "    end\n" +
                "  end\n" +
                "end\n" +
                "\0";

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

                LJM.eReadName(handle, "FIRMWARE_VERSION", ref value);
                Console.WriteLine("FIRMWARE_VERSION: {0}", value);

                //Load and run the script
                LoadLuaScript(handle, luaScript);

                LJM.eReadName(handle, "LUA_RUN", ref value);
                Console.WriteLine("LUA_RUN: {0}", value);

                LJM.eReadName(handle, "LUA_DEBUG_NUM_BYTES", ref value);
                Console.WriteLine("LUA_DEBUG_NUM_BYTES: {0}", value);

                //Get info back from the script
                ReadLuaInfo(handle);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  //Pause for user
        }

        void LoadLuaScript(int handle, string luaScript)
        {
            byte[] scriptByteArr = System.Text.Encoding.ASCII.GetBytes(luaScript);
            int scriptLength = scriptByteArr.Length;
            int errorAddress = -1;

            Console.WriteLine("Script length: {0}", scriptLength);

            //LUA_RUN must be written to twice to disable a currently running script.
            LJM.eWriteName(handle, "LUA_RUN", 0);
            //Wait for the Lua VM to shut down. Some T7 firmware versions need
            //a longer time to shut down than others.
            System.Threading.Thread.Sleep(600);
            LJM.eWriteName(handle, "LUA_RUN", 0);

            LJM.eWriteName(handle, "LUA_SOURCE_SIZE", scriptLength);
            LJM.eWriteNameByteArray(
                handle,
                "LUA_SOURCE_WRITE",
                scriptLength,
                scriptByteArr,
                ref errorAddress
            );
            //Enable debug info for getting data back from Lua
            LJM.eWriteName(handle, "LUA_DEBUG_ENABLE", 1);
            LJM.eWriteName(handle, "LUA_DEBUG_ENABLE_DEFAULT", 1);
            //Start the Lua script
            LJM.eWriteName(handle, "LUA_RUN", 1);
        }

        void ReadLuaInfo(int handle)
        {
            int i;
            const int NUM_LOOPS = 10;
            double numBytes;
            int errorAddress = -1;
            int executionLoopNum;
            double value=0;
            for (i = 0; i < NUM_LOOPS; i++) {
                //The script sets the interval length with LJ.IntervalConfig.
                //Note that LJ.IntervalConfig has some jitter and that this program's
                //interval (set by MillisecondSleep) will have some minor drift from
                //LJ.IntervalConfig.
                System.Threading.Thread.Sleep(1000);

                LJM.eReadName(handle, "LUA_RUN", ref value);
                Console.WriteLine("LUA_RUN: {0}", value);

                //Add custom logic to control the Lua execution block
                executionLoopNum = i % 3;
                //Write which lua control block to run using the user ram register
                LJM.eWriteName(handle, "USER_RAM0_U16", executionLoopNum);

                numBytes = 0;
                LJM.eReadName(handle, "LUA_DEBUG_NUM_BYTES", ref numBytes);

                if ((int)numBytes == 0) {
                    continue;
                }

                Console.WriteLine("LUA_DEBUG_NUM_BYTES: {0}", (int)numBytes);

                byte[] aBytes = new byte[(int)numBytes];

                LJM.eReadNameByteArray(
                    handle,
                    "LUA_DEBUG_DATA",
                    (int)numBytes,
                    aBytes,
                    ref errorAddress
                );
                Console.Write("LUA_DEBUG_DATA: ");
                foreach(char val in aBytes)
                {
                    Console.Write("{0:c}", val);
                }
                Console.WriteLine();
            }
            //Stop the script
            LJM.eWriteName(handle, "LUA_RUN", 0);
        }
    }
}
