using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.Diagnostics;

namespace fcHMI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Start");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.WriteLine("Before run");
            MainECMaster();
            Application.Run(new Form1());
            Console.WriteLine("After run");
        }

        static async Task MainECMaster()
        {

            Console.WriteLine("ver 230902.00");
            //Console.ReadKey(true);
            var cts = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                var sleepTime = 1000;

                while (!cts.IsCancellationRequested)
                {
                    //cts.Cancel();
                    Thread.Sleep(sleepTime);
                    Console.WriteLine("master lives");
                }
            }, cts.Token);

            await task;
        }

    }       
}
