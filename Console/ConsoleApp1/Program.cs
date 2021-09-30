using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NurApiDotNet;
using NurApiDotNet.DotnetFramework;
using static NurApiDotNet.NurApi;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
       static NurTransportStatus connStatus = NurTransportStatus.Disconnected;

        static void Main(string[] args)
        {
            NurApi api = new NurApi();
            api.SetLogToStdout(true);

            AntennaTest antTest = new AntennaTest(api);
            GpioTest gpioTest = new GpioTest(api);
            
            int connTO = 0; // mSec connection timeout
            
            api.Init();
            api.ConnectionStatusEvent += Api_ConnectionStatusEvent;
            api.BootEvent += Api_BootEvent;
            api.DisconnectedEvent += Api_DisconnectedEvent;
            api.ConnectedEvent += Api_ConnectedEvent;

            //api.DisconnectedEvent += Api_DisconnectedEvent;
           

            api.LogEvent += Api_LogEvent;

            
            
            api.SetLogLevel(LOG_VERBOSE);
            //api.SetLogToFile(true); //Log to default file
            //api.SetLogFilePath("C:\\Temp\\NurCCLog.txt", true);
           // api.SetLogToStdout(true);
                        
            try
            {
                api.Connect(new Uri("ser://COM6"));
                //api.Connect(new Uri("tcp://192.168.3.106"));


                Console.WriteLine("Connecting to:" + api.ConnectedDeviceUri);

                //Wait until connected or timeout
                while (connTO < 5000)
                {
                    if (connStatus == NurTransportStatus.Connected)
                    {
                        //Finally we are connected
                        Console.WriteLine("CONNECTED! in " + connTO.ToString() + " ms");
                        break;
                    }
                   

                    System.Threading.Thread.Sleep(5);
                    connTO += 5;
                }

                if(connStatus != NurTransportStatus.Connected)
                {
                    Console.WriteLine("Connecetion Timeout. exit..");
                    Console.ReadKey();
                    return;
                }

                api.CommTimeoutMilliSec = 3000;

                ReaderInfo info = api.GetReaderInfo();
                Console.WriteLine(info.name);

                ModuleVersions ver = api.GetVersions();
                Console.WriteLine("Versions APP=" + ver.primaryVersion+" BL=" + ver.secondaryVersion + " Mode=" + ver.mode + " IsAppMode=" + ver.isApplicationMode.ToString());
                Console.WriteLine("FWInfo=" + api.GetFWINFO());
                Console.WriteLine("Region=" + api.GetRegionInfo().name);
                Console.WriteLine("Region india=" + api.GetRegionInfo(NurApi.REGIONID_INDIA).name);
                
                SystemInfo si = api.GetSystemInfo();
                Console.WriteLine("AppAddr=" + si.appAddr.ToString());

                Console.WriteLine("LinkFreq current = " + api.LinkFrequency.ToString());
                api.LinkFrequency = 256000;
                Console.WriteLine("LinFreq now = " + api.LinkFrequency.ToString());

                ModuleSetup mySetup = new ModuleSetup();
                mySetup.inventoryQ = 6;
                mySetup.inventoryRounds = 2;

                api.SetModuleSetup(NurApi.SETUP_INVQ | NurApi.SETUP_INVROUNDS, ref mySetup);
                //api.SetModuleSetup(SetupFlags.InventoryQ |SetupFlags.InventoryRounds, mySetup);
                Console.WriteLine("Q = " + api.InventoryQ.ToString());
                Console.WriteLine("Rounds = " + api.InventoryRounds.ToString());

                /*
                antTest.AntennaList();
                antTest.EnabledAntennas();
                antTest.TestDisableEnable();
                  */

               gpioTest.ShowGpioList();
               gpioTest.LedShow();

                List<AccessorySensorConfig> mySensors = api.AccSensorEnumerate();

                for(int x=0;x<mySensors.Count;x++)
                    Console.WriteLine("Sensor: " + x.ToString() +"=" + mySensors[x].source.ToString());

                /*
                InventoryResponse resp = api.Inventory();
                Console.WriteLine("found=" + resp.numTagsFound.ToString());
                Console.WriteLine("mem=" + resp.numTagsMem.ToString());
                

                //Console.WriteLine("First tag found = " + myTag.GetEpcString());
                api.FetchTags();
                TagStorage ts = api.GetTagStorage();
                List<Tag> added = ts.GetAddedTags();
                List<Tag> updated = ts.GetUpdatedTags();
                                
                if (ts.Count > 0)
                {
                    Tag myTag = ts.Get(0);
                    Console.WriteLine("First tag found = " + myTag.GetEpcString());
                    //Write new epc by revering current
                    byte[] newEpc = new byte[myTag.epc.Length];
                    Array.Copy(myTag.epc, 0, newEpc, 0, myTag.epc.Length);
                    Array.Reverse(newEpc, 0,newEpc.Length);
                    myTag.WriteEPC(0, false,newEpc);
                    //myTag.WriteEPC(0, false, "AABBCCDD");
                    api.ClearTagsEx();
                    api.Inventory();
                    api.FetchTags();
                    TagStorage tsi = api.GetTagStorage();
                    foreach (NurApi.Tag tg in tsi)
                    {
                        Console.WriteLine("EPC=" + tg.GetEpcString());
                    }
                    

                    Console.WriteLine("Tag[0] = " + ts.Get(0).EpcString + " Api=" + ts.Get(0).hApi.ToString());
                }
                */


                Console.ReadKey();               
                api.Disconnect();
                Console.WriteLine("bye..");


            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("CurLinkFreq = " + api.LinkFrequency.ToString());
                api.Disconnect();
                Console.ReadKey();
            }

            //dd.DeviceDiscoveredEvent += Dd_DeviceDiscoveredEvent;

            //dd.StartDeviceDiscovery();
            
        }

        private static void Api_ConnectionStatusEvent(object sender, NurTransportStatus e)
        {
            Console.WriteLine(e.ToString());
            connStatus = e;
        }

        private static void Api_ConnectedEvent(object sender, NurEventArgs e)
        {
            Console.WriteLine("CONNECTED");
        }

        private static void Api_DisconnectedEvent(object sender, NurEventArgs e)
        {
            Console.WriteLine("DISCONNECTED");
        }

        private static void Api_BootEvent(object sender, BootEventArgs e)
        {
            Console.WriteLine("BOOT:" + e.message);
        }
                          

        private static void Api_LogEvent(object sender, LogEventArgs e)
        {
            // Console.WriteLine("LOG: " + e.timestamp.ToString() + ":" + e.message);
        }
                        
    }
}
