//-----------------------------------------------------------------------------
// ESS.cs
//
// Demonstrates how to use ESS stream with multiple T8s.
//
// support@labjack.com
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using LabJack;


namespace ESS {
    class ESS {
        static void Main(string[] args) {
            ESS ess = new ESS();
            ess.run();
        }

        public void showErrorMessage(Exception e) {
            Console.WriteLine("Exception: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }

        public void run() {
            // Example Parameters
            int MAX_DEVICES = 10;
            int CONNECTION_TYPE = LJM.CONSTANTS.ctUSB;
            double MAX_VOLTAGE = 2.0;
            int RESOLUTION_INDEX = 16;
            double DESIRED_RATE = 300.0;
            int SCANS_PER_READ = 10;
            double STREAM_DURATION_SECONDS = 5.0;

            try {
                // Check for avaiable devices on USB
                int found = 0;
                int[] dev_type = new int[MAX_DEVICES];
                int[] con_type = new int[MAX_DEVICES];
                int[] sn       = new int[MAX_DEVICES];
                int[] ip       = new int[MAX_DEVICES];
                LJM.ListAll(LJM.CONSTANTS.dtT8, LJM.CONSTANTS.ctUSB, ref found, dev_type, con_type, sn, ip);
                if (found < 2 || found > MAX_DEVICES) {
                    string error = $"Could not find the correct number of T8's on USB: {found} found";
                    Console.WriteLine(error);
                    throw new Exception(error);
                } else {
                    Console.WriteLine($"Number of devices found on T8: {found}");
                }

                // Open found T8's
                int[] handles = new int[found];
                for (int h = 0; h < found; h++) {
                    LJM.Open(LJM.CONSTANTS.dtT8, CONNECTION_TYPE, sn[h].ToString(), ref handles[h]);
                    Console.WriteLine($"Opened T8 {h} {sn[h]}");
                }

                // Library Configuration
                LJM.WriteLibraryConfigS("LJM_SEND_RECEIVE_TIMEOUT_MS", 0.0);
                LJM.WriteLibraryConfigS("LJM_STREAM_RECEIVE_TIMEOUT_MODE", 2.0);
                LJM.WriteLibraryConfigS("LJM_STREAM_RECEIVE_TIMEOUT_MS", 0.0);

                // Settings to apply to all devices
                for (int h = 0; h < found; h++) {
                    LJM.eWriteName(handles[h], "AIN_ALL_RANGE", MAX_VOLTAGE);
                    LJM.eWriteName(handles[h], "STREAM_RESOLUTION_INDEX", RESOLUTION_INDEX);
                }

                // Stream Configuration
                string[] scan_list_names = {"AIN0", "AIN1", "AIN2", "AIN3", "AIN4", "AIN5", "AIN6", "AIN7", "AIN_HEALTH"};
                int total_channels = scan_list_names.Length;
                int[] scan_list = new int[total_channels];
                int[] scan_list_types = new int[total_channels];
                LJM.NamesToAddresses(total_channels, scan_list_names, scan_list, scan_list_types);

                Stopwatch stopwatch = new Stopwatch();

                // Commence streaming
                try {
                    double actual_rate;
                    // Configure all but first device as secondary device
                    for (int h = 1; h < found; h++) {
                        LJM.eWriteName(handles[h], "STREAM_CLOCK_SOURCE", 8);
                        actual_rate = DESIRED_RATE;
                        LJM.eStreamStart(handles[h], SCANS_PER_READ, total_channels, scan_list, ref actual_rate);
                        Console.WriteLine($"Device {h} has Started Stream as Secondary at {actual_rate} HZ");
                    }

                    // Configure first device as primary device
                    LJM.eWriteName(handles[0], "STREAM_CLOCK_SOURCE", 4);
                    actual_rate = DESIRED_RATE;
                    LJM.eStreamStart(handles[0], SCANS_PER_READ, total_channels, scan_list, ref actual_rate);
                    Console.WriteLine($"Device 0 has Started Stream as Primary at {actual_rate} HZ");

                    int channels_per_read = total_channels * SCANS_PER_READ;

                    // Stream for STREAM_DIRATION_SECONDS
                    stopwatch.Start();
                    while (stopwatch.Elapsed.TotalSeconds < STREAM_DURATION_SECONDS) {
                        // Read Stream for each device
                        for (int h = 0; h < found; h++) {
                            // Read Stream
                            double[] data = new double[channels_per_read];
                            int device_log = -1;
                            int driver_log = -1;
                            try {
                                LJM.eStreamRead(handles[h], data, ref device_log, ref driver_log);
                            } catch (Exception e) {
                                Console.WriteLine($"Device {h} did not stream read: {e.Message}");
                                continue;
                            }
                            Console.WriteLine($"Device {h} Stream Read:\t{device_log}\t{driver_log}");

                            // Process each scan in the read
                            for (int scan = 0; scan < SCANS_PER_READ; scan++) {
                                int index_start = scan * total_channels;

                                // Check for auto recovery
                                if (data[index_start] == -9999.0) {
                                    Console.WriteLine($"Device {h} scan {scan} was an auto recovery scan");
                                    continue;
                                }

                                // Check channel health
                                if (data[index_start + 8] != 255.0) {
                                    string bin_str = Convert.ToString((uint)data[index_start + 8], 2).PadLeft(8, '0');
                                    Console.WriteLine($"Device {h} experienced an ESD event, AIN channel might be down {bin_str}");
                                }

                                // Print channel data
                                Console.WriteLine($"Device {h}:{scan} " +
                                                  $"{data[index_start + 0]:F2}, " +
                                                  $"{data[index_start + 1]:F2}, " +
                                                  $"{data[index_start + 2]:F2}, " +
                                                  $"{data[index_start + 3]:F2}, " +
                                                  $"{data[index_start + 4]:F2}, " +
                                                  $"{data[index_start + 5]:F2}, " +
                                                  $"{data[index_start + 6]:F2}, " +
                                                  $"{data[index_start + 7]:F2}");
                            }
                        }
                    }
                } catch (Exception e) {
                    showErrorMessage(e);
                }
                stopwatch.Stop();

                // Stop Stream with primary being last
                for (int h = found - 1; h >= 0; h--) {
                    try {
                        LJM.eStreamStop(handles[h]);
                    } catch (Exception e) {
                        Console.WriteLine($"Device {h} did not stop stream: {e.Message}");
                    }
                }
                Console.WriteLine("All Devices have stopped stream");

            } catch (Exception e) {
                showErrorMessage(e);
            }

            LJM.CloseAll();  //Close all handles

            Console.WriteLine("\nDone.\nPress the enter key to exit.");
            //Console.ReadLine();  //Pause for user
        }
    }
}
