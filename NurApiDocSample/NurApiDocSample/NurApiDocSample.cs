using NurApiDotNet;
using System.Diagnostics;

namespace NurApiDocSample
{
    /// <summary>
    /// Simple console application showing some of the basic NurApi operations.
    /// Needs NordicID.NurApi.Net and NordicID.NurApi.SerialTransport assemblies.
    /// If serial port support is not used, NordicID.NurApi.SerialTransport is not needed.
    /// </summary>
    internal class App
    {
        /// <summary>
        /// NurApi object for controlling device
        /// </summary>
        private NurApi mNurApi;

        /// <summary>
        /// List of discovered devices
        /// </summary>
        private List<Uri> mDiscoveredDevices = new List<Uri>();

        /// <summary>
        /// Set in ConnectionStatusEvent when connection status is changed to connected
        /// </summary>
        private AutoResetEvent mConnectedEvent = new AutoResetEvent(false);

        private int mConnectTimeout = 10000;

        internal App()
        {
            // Create our NurApi instance
            mNurApi = new NurApi();

            // Set very verbose logging for debugging purposes
            //mNurApi.SetLogLevel(NurApi.LOG_ALL);
            //mNurApi.SetLogToStdout(true);

            // Attach event handlers
            mNurApi.ConnectionStatusEvent += Nurapi_ConnectionStatusEvent;
            mNurApi.InventoryStreamEvent += Nurapi_InventoryStreamEvent;
            mNurApi.LogEvent += NurApi_LogEvent;
        }

        /// <summary>
        /// Called for NurApi internal logging
        /// </summary>
        private void NurApi_LogEvent(object? sender, NurApi.LogEventArgs e)
        {
            Console.WriteLine(e.message);
        }

        /// <summary>
        /// Called when new inventory stream data is available
        /// </summary>
        private void Nurapi_InventoryStreamEvent(object? sender, NurApi.InventoryStreamEventArgs e)
        {
            // Inventory stream stores inventoried tags to NurApi's internal tag storage automatically
            NurApi.TagStorage tagStorage = mNurApi.GetTagStorage();
            // NOTE! When inventory stream is running, access to NurApi tag storage must be locked
            lock (tagStorage)
            {
                // Get newly added tags
                var addedTags = tagStorage.GetAddedTags();
                foreach (var pair in addedTags)
                {
                    var tag = pair.Value;
                    var epcStr = tag.GetEpcString();
                    if (tag.irData != null)
                        epcStr += $" data[{tag.GetDataString()}]";
                    Console.WriteLine($"Found new tag: {epcStr}, rssi {pair.Value.rssi}");
                }
                // Tag storage addedTags dictionary must be cleared to
                // receive new added tags next time event is called.
                addedTags.Clear();

                /*
                // NOTE: Uncomment if needed, this likely VERY verbose.
                // Usually large amount of tags gets updated during each inventory rounds, 
                // depends on inventoried tag population size.
                // Get updated tags
                var updatedTags = tagStorage.GetUpdatedTags();
                foreach (var pair in updatedTags)
                {
                    var tag = pair.Value;
                    var epcStr = tag.GetEpcString();
                    if (tag.irData != null)
                        epcStr += $" data[{tag.GetDataString()}]";
                    Console.WriteLine($"Updated new tag: {epcStr}, rssi {pair.Value.rssi}");
                }
                // Tag storage updatedTags dictionary must be cleared to
                // receive new updated tags next time event is called.
                updatedTags.Clear();*/
            }

            // Nur RFID reader stops inventory stream automatically after 20 secs
            // and application needs to restart it, if needed.
            if (e.data.stopped)
            {
                Task.Run(() => {
                    try
                    {
                        // Restart inventory stream
                        mNurApi.StartInventoryStream();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to restart inventory stream: " + ex.Message);
                    }
                });
            }
        }

        /// <summary>
        /// Called when NurApi transport status changes.
        /// </summary>
        private void Nurapi_ConnectionStatusEvent(object? sender, NurTransportStatus e)
        {
            Console.WriteLine($"Connection status changed to {e}; uri {mNurApi.LastConnectUri?.GetAddress()}");

            if (e == NurTransportStatus.Connected)
            {
                Console.WriteLine($"Connected to reader {mNurApi.Info.name}");
                // Set connected event, this is waited in this sample when connecting to reader.
                mConnectedEvent.Set();
            }
        }

        /// <summary>
        /// Helper function to read integer from console input
        /// </summary>
        private int ConsoleReadInt()
        {
            string? line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                throw new Exception("No value provided");

            int res;
            if (!int.TryParse(line, out res))
                throw new Exception("Invalid number provided");
            return res;
        }

        /// <summary>
        /// Helper function to find tag with best RSSI for this sample.
        /// Used in tag read/write examples.
        /// </summary>
        private NurApi.Tag FindNearestTag()
        {
            Console.WriteLine("Finding nearest tag..");
            // Clear tag storage
            mNurApi.ClearTagsEx();
            // Perform inventory to get tags nearby
            // Use default settings always, Rounds=auto, Q=auto, Session=0
            mNurApi.Inventory(0, 0, 0);
            // Fetch tags from Nur device
            var tagStorage = mNurApi.FetchTags();
            if (tagStorage.Count == 0)
                throw new Exception("No tag found");

            // Sort received tags to get tag with best rssi first
            var tag = tagStorage.OrderByDescending(t => t.rssi).First();
            Console.WriteLine($"Found tag {tag.GetEpcString()}, rssi {tag.rssi}");
            return tag;
        }

        /// <summary>
        /// Helper function to print content of the tag storage to console
        /// </summary>
        private void PrintTagStorage(NurApi.TagStorage tagStorage)
        {
            if (tagStorage.Count == 0)
            {
                Console.WriteLine("No tags found");
            }
            else
            {
                Console.WriteLine($"Total {tagStorage.Count} tag(s) found");
                int count = 1;
                foreach (var tag in tagStorage)
                {
                    var epcStr = tag.GetEpcString();
                    if (tag.irData != null)
                    {
                        epcStr += $" data[{tag.GetDataString()}]";
                    }
                    Console.WriteLine($"#{count++} {epcStr}; seen {tag.UpdateCount} times; last seen {(int)(DateTime.UtcNow-tag.LastSeenUtc).TotalMilliseconds} ms ago; time visible {(int)(tag.LastSeenUtc-tag.FirstSeenUtc).TotalMilliseconds} ms");
                }
            }
        }

        /// <summary>
        /// Called by NurDeviceDiscovery when device has appeared or disappeared.
        /// NOTE: Care must be takes here, each callback call is called own task. Carefully handle multithreading access here.
        /// </summary>
        void DeviceDiscoveryCallback(object sender, NurDeviceDiscoveryEventArgs args)
        {
            if (args.Visible)
            {
                // Device appeared

                // Lock access to mDiscoveredDevices, since this callback may be called simultaneously from multiple tasks
                lock (mDiscoveredDevices)
                {
                    // Add discovered device Uri to list
                    mDiscoveredDevices.Add(args.Uri);
                    Console.WriteLine($"{mDiscoveredDevices.Count}) {args.Uri.GetAddress()}");
                }
            }
            else
            {
                // Device disappeared
                Console.WriteLine($"* {args.Uri.GetAddress()} disappeared!");
            }
        }

        /// <summary>
        /// Example howto use NurDeviceDiscovery to discover devices
        /// </summary>
        private void DiscoverDevices()
        {
            int idx = -1;
            bool skipConnect = false;

            // Clear discovered devices list
            mDiscoveredDevices.Clear();
            
            // Set false to enable 'mdns://' reports instead of 'tcp://'
            // NurDeviceDiscovery_Mdns.ReportTcpUri = false;

            // Start discovering devices
            NurDeviceDiscovery.Start(DeviceDiscoveryCallback);

            Console.WriteLine("Discovering devices.");
            Console.WriteLine("Enter device number to connect or hit enter to continue without connecting");

            // Wait for console input
            try
            {
                idx = ConsoleReadInt() - 1;
            } catch {
                // If no number given, do not connect
                skipConnect = true;
            }

            // Stop discovering devices
            NurDeviceDiscovery.Stop(DeviceDiscoveryCallback);

            Console.WriteLine($"Discovered {mDiscoveredDevices.Count} devices");

            if (skipConnect)
                return;

            if (idx < 0 || idx >= mDiscoveredDevices.Count)
                throw new Exception("Device with provided number not available");

            Console.WriteLine($"Start connecting to {mDiscoveredDevices[idx].GetAddress()}");

            // Attempt to connect to discovered device
            mNurApi.Connect(mDiscoveredDevices[idx]);

            // Wait 'mConnectTimeout' for connection to establish
            if (!mConnectedEvent.WaitOne(mConnectTimeout))
            {
                mNurApi.Disconnect();
                throw new Exception($"Could not connect to device in {mConnectTimeout} ms");
            }
        }

        /// <summary>
        /// Example for direct connection to device
        /// </summary>
        private void ConnectToDevice()
        {
            Console.WriteLine("Enter Uri to connect (e.g. 'tcp://1.2.3.4', 'ser://com4')");

            // Wait for console input
            var uriString = Console.ReadLine();
            if (string.IsNullOrEmpty(uriString))
                 throw new Exception("No uri provided");
                
            // Attempt to connect provided uri
            mNurApi.Connect(uriString);

            // Wait 'mConnectTimeout' for connection to establish
            if (!mConnectedEvent.WaitOne(mConnectTimeout))
            {
                mNurApi.Disconnect();
                throw new Exception($"Could not connect to device in {mConnectTimeout} ms");
            }
        }

        /// <summary>
        /// Disconnect currently connected device
        /// </summary>
        private void DisconnectDevice()
        {
            mNurApi.Disconnect();
        }

        /// <summary>
        /// Example how to write tag's EPC. This method also changes PC word epc length if needed.
        /// </summary>
        private void WriteEPCMemory()
        {
            // Find nearest tag, we'll use that for writing
            var tag = FindNearestTag();            

            // Wait console input for new EPC
            Console.WriteLine("Enter new EPC (hex string, word boundary)");
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                return;

            // Convert from hex string to byte array
            byte[] newEpc = NurApi.HexStringToBin(line);
            
            Console.WriteLine("Writing tag..");
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Write tag EPC and change PC word epc length if needed
            // NOTE: this example works for unsecured tags only, if secured tag is used
            // change passwd parameter and set secured parameter to true.
            tag.WriteEPC(0, false, newEpc);
            
            timer.Stop();
            Console.WriteLine($"Tag written in {timer.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Example how to read 4 words from begining of the TID bank.
        /// </summary>
        private void ReadTIDMemory()
        {
            // Find nearest tag, we'll use that for reading
            var tag = FindNearestTag();

            Console.WriteLine("Reading 4 words from TID memory..");
            
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Read from TID bank address 0 bits, 8 bytes (4 words)
            // NOTE: this example works for unsecured tags only, if secured tag is used
            // change passwd parameter and set secured parameter to true.
            var data = tag.ReadTag(0, false, NurApi.BANK_TID, 0, 8);
            
            timer.Stop();
            Console.WriteLine($"TID read in {timer.ElapsedMilliseconds} ms");
            Console.WriteLine($"TID content: {NurApi.BinToHexString(data)}");
        }

        /// <summary>
        /// Example how to configure inventory read (IR).
        /// Inventory read will read specified tag content during inventory round
        /// and report data with tag buffer.
        /// This example configures IR to read 4 words from begining of the TID bank.
        /// 
        /// NOTE: Inventory read will slow down inventory a lot, use only when needed
        /// </summary>
        private void InventoryRead()
        {
            // Get current IR settings
            var irInfo = mNurApi.GetInventoryRead();
            Console.WriteLine($"Current IR info:");
            Console.WriteLine($"Active {irInfo.active}; Type {irInfo.type}; Bank {irInfo.bank}; wAddress {irInfo.wAddress}; wLength {irInfo.wLength}");

            Console.WriteLine("Enable TID bank inventory read? (y/n)");
            var line = Console.ReadLine();
            if (line == "y")
            {
                // Configure IR to return EPC+data in tag buffer and
                // read TID bank from address 0 bits, 4 words
                mNurApi.InventoryRead(true, NurApi.NUR_IR_EPCDATA, NurApi.BANK_TID, 0, 4);
                Console.WriteLine("Inventory read enabled for TID bank, address 0, words 4");
            }
            else if (line == "n")
            {
                // Disable inventory read
                mNurApi.InventoryReadCtl = false;
                Console.WriteLine("Inventory read disabled");
            }
            else
            {
                return;
            }

            // Read IR settings again and print out
            irInfo = mNurApi.GetInventoryRead();
            Console.WriteLine($"New IR info:");
            Console.WriteLine($"Active {irInfo.active}; Type {irInfo.type}; Bank {irInfo.bank}; wAddress {irInfo.wAddress}; wLength {irInfo.wLength}");
            if (irInfo.active)
            {
                Console.WriteLine("Configured IR data is now read from tag during inventory and returned in tag storage.");
            }
        }

        /// <summary>
        /// Example how to use inventory stream for inventorying tag population.
        /// 
        /// Inventory stream runs asynchronously in the RFID reader, thus no other commands should sent 
        /// to reader while stream is enabled.
        /// 
        /// Inventory stream is usually used to do mass inventory.
        /// </summary>
        private void InventoryStream()
        {
            // Clear tag storage
            mNurApi.ClearTagsEx();

            Console.WriteLine("Running inventory stream. Hit enter to stop");

            // Start inventory stream on RFID module
            mNurApi.StartInventoryStream();

            // Wait for any console input
            Console.ReadLine();

            // Stop inventory stream
            mNurApi.StopInventoryStream();
            Console.WriteLine("Inventory stream stopped");

            // Get NurApi tag storage, this is already filled during inventory stream
            var tagStorage = mNurApi.GetTagStorage();

            // Print out tag storage content
            PrintTagStorage(tagStorage);
        }

        /// <summary>
        /// Example how to run single blocking inventory.
        /// NOTE: RFID reader will run inventory for all enabled antennas, up to 4 antennas at time.
        /// </summary>
        private void SingleInventory()
        {
            Console.WriteLine("Running single inventory");

            // Clear tag storage
            mNurApi.ClearTagsEx();
            
            // Run inventory with module stored settings.
            // NOTE: this will block as long as inventory round is done.
            // Depending on settings, enabled antennas, region this might take over 10 secs to finish.
            mNurApi.Inventory();

            // Fetch tags from reader
            var tagStorage = mNurApi.FetchTags();

            // Print out tag storage content
            PrintTagStorage(tagStorage);
        }

        /// <summary>
        /// Example of settings reader settings
        /// </summary>
        private void ReaderSettings()
        {
            Console.WriteLine($"Current inventory settings:");
            Console.WriteLine($"InventoryQ: {mNurApi.InventoryQ}");
            Console.WriteLine($"InventorySession: {mNurApi.InventorySession}");
            Console.WriteLine();
            Console.WriteLine($"Set new InventoryQ (0-15) setting or hit enter to leave as is");
            try
            {
                mNurApi.InventoryQ = ConsoleReadInt();
                Console.WriteLine($"New InventoryQ: {mNurApi.InventoryQ}");
            }
            catch { }

            Console.WriteLine($"Set new InventorySession (0-3) setting or hit enter to leave as is");
            try
            {
                mNurApi.InventorySession = ConsoleReadInt();
                Console.WriteLine($"New InventorySession: {mNurApi.InventorySession}");
            }
            catch { }
        }

        /// <summary>
        /// Example od getting connected reader information
        /// </summary>
        private void ReaderInfo()
        {
            var info = mNurApi.Info;
            Console.WriteLine($"Device name: {info.name}");
            // This is serial of the NUR module
            Console.WriteLine($"Module serial number: {info.serial}");
            // If altSerial is present, it is serial of the host device where NUR module is connected
            if (!string.IsNullOrEmpty(info.altSerial))
                Console.WriteLine($"Device serial number: {info.altSerial}");
            Console.WriteLine($"FW version: {info.GetVersionString()}");
            Console.WriteLine($"GPIO count {info.numGpio}");
            Console.WriteLine($"Max antenna count {info.maxAntennas}");
        }

        /// <summary>
        /// Application main loop
        /// </summary>
        internal void Run()
        {
            bool exitApp = false;

            // List of operations
            // Tuple:
            //  - Item1 = char to start op
            //  - Item2 = string to print
            //  - Item3 = function to call
            //  - Item4 = if true, this op is allowed only when connected
            List<Tuple<char, string, Action, bool>> operations = new List<Tuple<char, string, Action, bool>>()
            {
                Tuple.Create('1', "Discover devices", DiscoverDevices, false),
                Tuple.Create('2', "Connect to device", ConnectToDevice, false),
                Tuple.Create('3', "Disconnect", DisconnectDevice, true),
                Tuple.Create('4', "Inventory read config", InventoryRead, true),
                Tuple.Create('5', "Inventory stream", InventoryStream, true),
                Tuple.Create('6', "Single Inventory", SingleInventory, true),
                Tuple.Create('7', "Read tag TID", ReadTIDMemory, true),
                Tuple.Create('8', "Write tag EPC", WriteEPCMemory, true),
                Tuple.Create('9', "Reader info", ReaderInfo, true),
                Tuple.Create('0', "Reader settings", ReaderSettings, true),
                Tuple.Create('x', "Exit", () => { exitApp = true; }, false),
            };

            while (!exitApp)
            {
                Console.WriteLine();
                Console.WriteLine("Select operation:");

                // Print ops
                foreach (var op in operations)
                {
                    // Skip if not connected and connected status is needed for op
                    if (op.Item4 && mNurApi.ConnectionStatus != NurTransportStatus.Connected)
                        continue;

                    Console.WriteLine($"{op.Item1}) {op.Item2}");
                }

                try
                {
                    // Wait for input
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        // Get first char
                        var key = line?[0];

                        // Attempt to run op
                        foreach (var op in operations)
                        {
                            if (op.Item4 && mNurApi.ConnectionStatus != NurTransportStatus.Connected)
                                continue;

                            if (op.Item1 == key)
                            {
                                op.Item3();
                                break;
                            }
                        }
                    }
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occured: {ex.Message}");
                }
            }

            // Free all resources
            mNurApi.Dispose();
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Add serial port support. 
            // NOTE: Needs NordicID.NurApi.SerialTransport reference
            // If serial port support is not needed, you can comment this line
            NurApiDotNet.SerialTransport.Support.Init();

            Console.WriteLine($"NurApi version {NurApi.FileVersion}");

            new App().Run();
        }
    }
}
