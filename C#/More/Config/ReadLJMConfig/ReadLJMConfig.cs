//-----------------------------------------------------------------------------
// ReadLJMConfig.cs
//
// Demonstrates ReadLibraryConfigS and ReadLibraryConfigStringS
//
// support@labjack.com
//-----------------------------------------------------------------------------

using System;
using LabJack;


namespace ReadLJMConfig
{
    class ReadLJMConfig
    {
        static void Main(string[] args)
        {
            ReadLJMConfig rc = new ReadLJMConfig();
            rc.performActions();
        }

        public void showErrorMessage(LJM.LJMException e)
        {
            Console.Out.WriteLine("LJMException: " + e.ToString());
            Console.Out.WriteLine(e.StackTrace);
        }

        public void performActions()
        {
            try
            {
                // Read the name of the error constants file being used by LJM
                string configString = "LJM_ERROR_CONSTANTS_FILE";
                string stringRead = "";
                LJM.ReadLibraryConfigStringS(configString, ref stringRead);
                Console.WriteLine("{0}: {1}", configString, stringRead);

                // Write the communication send/receive timeout for LJM
                configString = "LJM_SEND_RECEIVE_TIMEOUT_MS";
                LJM.WriteLibraryConfigS(configString, 5000);

                // Read the communication send/receive timeout for LJM
                double doubleRead = 0;
                LJM.ReadLibraryConfigS(configString, ref doubleRead);
                Console.WriteLine("{0}: {1}", configString, doubleRead);
            }
            catch (LJM.LJMException e)
            {
                showErrorMessage(e);
            }

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            Console.ReadLine();  // Pause for user
        }
    }
}
