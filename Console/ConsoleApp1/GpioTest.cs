using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NurApiDotNet;


namespace ConsoleApp1
{
    class GpioTest
    {
        NurApi api;
        public GpioTest(NurApi api)
        {
            this.api = api;
            api.IOChangeEvent += Api_IOChangeEvent; ;
        }

        private void Api_IOChangeEvent(object sender, NurApi.IOChangeEventArgs e)
        {
            Console.WriteLine("SRC=" + e.data.source.ToString() + " DIR=" + e.data.dir.ToString());
        }

        public void ShowGpioList()
        {
            GpioEntry[] gpio = api.GetGPIOConfig();

            for(int x=0;x<gpio.Length;x++)
            {
                GPIOType type = (GPIOType)gpio[x].type;
                GPIOEdge edge = (GPIOEdge)gpio[x].edge;
                GPIOAction action = (GPIOAction)gpio[x].action;
                GpioStatus status = api.GetGPIOStatus(x);
                Console.WriteLine("GPIO:" + (x + 1).ToString() + " Available=" + gpio[x].available.ToString() + " Enabled=" + gpio[x].enabled.ToString() + " Type=" + type.ToString() + " Edge=" + edge.ToString() + " Action=" + action.ToString()+ " State=" + status.state);
            }
        }

        public void LedShow()
        {
            //Tesed with SmartSampo where 4 inputs (buttons) and 4 output (led's)
            GpioEntry[] gpio = api.GetGPIOConfig();
            //make sure all shutdown
            api.SetGPIOStatus(4, true);
            api.SetGPIOStatus(5, true);
            api.SetGPIOStatus(6, true);
            api.SetGPIOStatus(7, true);
            Thread.Sleep(500);
            api.SetGPIOStatus(4, false);
            Thread.Sleep(500);
            api.SetGPIOStatus(5, false);
            Thread.Sleep(500);
            api.SetGPIOStatus(6, false);
            Thread.Sleep(500);
            api.SetGPIOStatus(7, false);
            Thread.Sleep(1500);
            //Mask test
            api.SetGPIOStatusMask(0xF0, true); //All off
            Thread.Sleep(500);
            api.SetGPIOStatusMask(0x50, false);
            Thread.Sleep(500);
            api.SetGPIOStatusMask(0x50, true);
            Thread.Sleep(500);
            api.SetGPIOStatusMask(0xF0, false);//All on

        }
               
    }
}
