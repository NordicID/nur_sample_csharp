using System;
using NurApiDotNet;
using NordicID.NurApi.Support;
using static NurApiDotNet.NurApi;
using System.Collections.Generic;

namespace ReadTID
{
    class Program
    {
        static NurTransportStatus connStatus = NurTransportStatus.Disconnected;
                       
        static void Main(string[] args)
        {
            TagStorage tags;
            NurApi nur = new NurApi();
            //nur.SetLogToStdout(true);
            int connTO = 0; // mSec connection timeout

            nur.Init();
            nur.ConnectionStatusEvent += Nur_ConnectionStatusEvent;
            nur.ConnectedEvent += Nur_ConnectedEvent;
            nur.DisconnectedEvent += Nur_DisconnectedEvent;

            try
            {
                nur.Connect(new Uri("ser://COM6"));
                //nur.Connect(new Uri("tcp://192.168.1.147"));


                //Console.WriteLine("Connecting to:" + nur.ConnectedDeviceUri.ToString());

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

                if (connStatus != NurTransportStatus.Connected)
                {
                    Console.WriteLine("Connecetion Timeout. exit..");
                    Console.ReadKey();
                    return;
                }

                //We are connected
                ReaderInfo info = nur.GetReaderInfo();
                Console.WriteLine(info.name);

                ModuleVersions ver = nur.GetVersions();
                Console.WriteLine("Versions APP=" + ver.primaryVersion + " BL=" + ver.secondaryVersion + " Mode=" + ver.mode + " IsAppMode=" + ver.isApplicationMode.ToString());
                Console.WriteLine("FWInfo=" + nur.GetFWINFO());
                Console.WriteLine("Region=" + nur.GetRegionInfo().name);

                Console.WriteLine("making simple inventory using existing settings..");

                System.Threading.Thread.Sleep(500);

                nur.ClearTagsEx();
                
                Console.WriteLine("Using TIDUtils.serialize for reading EPC + serial from TID");
                //nur.InventoryRead(true, NUR_IR_DATAONLY, BANK_TID, 0, 2);                
                nur.Inventory(); //Perform inventory
                tags = nur.FetchTags(); //Fetch tags from module                
                //Iterate results
                if (tags.Count > 0)
                {
                    for (int i = 0; i < tags.Count; i++)
                    {
                        TIDTag tid = TIDUtils.GetTIDInfo(nur, tags[i]);
                        Console.WriteLine("Full TID=" + BinToHexString(tid.FullTIDMemory));
                        Console.WriteLine("Company" + tid.Company);
                        Console.WriteLine("TagModel" + tid.TagModel);
                        Console.WriteLine("Serial: " + tid.MCS.ToString());
                        Console.WriteLine("XTID_Serial: " + BinToHexString(tid.XTID_Serial));
                        Console.WriteLine("MCS_valid: " + tid.MCS_valid.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("No tags seen..");
                }
                
                nur.ClearTagsEx();
                /*
                IrInformation ir = new IrInformation
                {
                    active = true,
                    type = NurApi.NUR_IR_EPCDATA,
                    bank = NurApi.BANK_TID,
                    wAddress = 0,
                    wLength = 4
                };

                nur.SetInventoryRead(ir);
                nur.Inventory();
                tags = nur.FetchTags();
                if (tags.Count > 0)
                {
                    foreach (Tag tag in tags)
                    {
                        //EPC + TID (tag.irData contains TID data)
                        Console.WriteLine("wl=2" + " EPC=" + tag.GetEpcString() + " TID=" + NurApi.BinToHexString(tag.irData));
                    }
                }
                else
                    Console.WriteLine("No tags seen..");

                ir.active = false;
                nur.SetInventoryRead(ir);
                */

                //nur.InventoryRead(false, NUR_IR_DATAONLY, BANK_TID, 0, 2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception:" + e.Message);                
            }

            Console.ReadKey();
            nur.Disconnect();
            Console.WriteLine("bye..");

        }

        private static void Nur_ConnectionStatusEvent(object sender, NurTransportStatus e)
        {
            Console.WriteLine(e.ToString());
            connStatus = e;
        }

        private static void Nur_DisconnectedEvent(object sender, NurEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Nur_ConnectedEvent(object sender, NurEventArgs e)
        {
            
        }
    }
}
