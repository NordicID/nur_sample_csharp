using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NurApiDotNet;

namespace ConsoleApp1
{
    class AntennaTest
    {
        NurApi api;
        public AntennaTest(NurApi api)
        {
            this.api = api;
        }

        public void EnabledAntennas()
        {
            List<string> antEnabled = api.EnabledPhysicalAntennas;

            foreach (string b in antEnabled)
                Console.WriteLine("Enabled Ant = " + b);

            Console.WriteLine("IsEnabled AUX1 and AUX16:" + api.IsPhysicalAntennaEnabled("AUX1,AUX16").ToString());
        }

        public void AntennaList()
        {            
            List<AntennaMapping> antList = api.GetAntennaList();
            foreach (AntennaMapping a in antList)
            {
                Console.WriteLine("Ant id=" + a.AntennaId.ToString() + " name=" + a.Name);
            }            
            
            List<string> antName = api.AvailablePhysicalAntennas;
            foreach (string b in antName)
            {
                Console.WriteLine("Ant =" + b);
            }
            
        }

        public void TestDisableEnable()
        {
            api.DisablePhysicalAntenna("AUX16");

            foreach (string b in api.EnabledPhysicalAntennas)
                Console.WriteLine("Enabled 2 Ant = " + b);

            Console.WriteLine("AntMask=" + api.GetPhysicalAntennaMask("AUX7,AUX8").ToString("X4"));

            api.EnablePhysicalAntenna("AUX1,AUX16", true);
            foreach (string b in api.EnabledPhysicalAntennas)
            {
                Console.WriteLine("Enabled Ant = " + b);
            }
        }
    }
}
