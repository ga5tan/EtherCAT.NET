
using EtherCAT.NET;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using EtherCAT.NET.Infrastructure;
using EtherCAT.NET.Extension;

using System.Configuration;
using System.Runtime.ExceptionServices;
using System.Security;

namespace fcHMI
{
    static class Program
    {
        public static string sMessage;
        public static Form1 mainForm;
        private static AutoResetEvent event_1 = new AutoResetEvent(false);
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]        
        static void Main()
        {
            try
            {

           
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mainForm = new Form1();
            //MainECMaster();
            myLog("Form1 created, starting Controller thread");
            MainECMasterTester();
            myLog("Controller thread started, running window");
            Application.Run(mainForm);         
        }

        static void myLog(string sMsg, bool bAddStamp = true)
        {
            var sOutput = "";
            if (!bAddStamp)
                sOutput = sMsg.TrimEnd();
            else
                sOutput = $"{DateTime.UtcNow.ToString("hh:mm:ss.fff tt")}: {sMsg.TrimEnd()}";

            Console.WriteLine(sOutput);
            sMessage += sOutput + Environment.NewLine;
            if (mainForm != null)
            mainForm.SetLog(sMessage);
        }        
        static async Task MainECMaster()
        {
            try
            {

            

            var interfaceName = ConfigurationManager.AppSettings["interfaceName"];
            myLog("ver 231102.02", false);
            myLog("Connecting interfaceName:" + interfaceName + " (case sensitive)", false);

            /* Set ESI location. Make sure it contains ESI files! The default path is /home/{user}/.local/share/ESI */
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var esiDirectoryPath = Path.Combine(localAppDataPath, "ESI");
            Directory.CreateDirectory(esiDirectoryPath);

            //C:\Users\Flexicam\AppData\Local\ESI
            myLog($"esiDirectoryPath: {esiDirectoryPath}", false);

            /* Copy native file. NOT required in end user scenarios, where EtherCAT.NET package is installed via NuGet! */
            var codeBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //Directory.EnumerateFiles(Path.Combine(codeBase, "runtimes"), "*soem_wrapper.*", SearchOption.AllDirectories).ToList().ForEach(filePath =>
            //{
            //    if (filePath.Contains(RuntimeEnvironment.RuntimeArchitecture))
            //        File.Copy(filePath, Path.Combine(codeBase, Path.GetFileName(filePath)), true);
            //});

            /* create logger */
            var loggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Debug);
                loggingBuilder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger("EtherCAT Master");

            /* create EtherCAT master settings (with 10 Hz cycle frequency) */
            var settings = new EcSettings(cycleFrequency: 10U, esiDirectoryPath, interfaceName);

            /* scan available slaves */
            var rootSlave = EcUtilities.ScanDevices(settings.InterfaceName);

            myLog($"Slaves: {rootSlave.Descendants().Count()}", false);

            rootSlave.Descendants().ToList().ForEach(slave =>
            {
                // If you have special extensions for this slave, add it here:                    
                // slave.Extensions.Add(new MyFancyExtension());                

                EcUtilities.CreateDynamicData(settings.EsiDirectoryPath, slave);
            });

            /* print list of slaves */
            var message = new StringBuilder();
            var slaves = rootSlave.Descendants().ToList();

            message.AppendLine($"Found {slaves.Count()} slaves:");

            foreach (var slave in slaves)
            {
                message.AppendLine($"{slave.DynamicData.Name} (PDOs: {slave.DynamicData.Pdos.Count} - CSA: {slave.Csa})");
            }

            logger.LogInformation(message.ToString().TrimEnd());

            /* create variable references for later use */
            var variables = slaves.SelectMany(child => child.GetVariables()).ToList();

            /* create EC Master (short sample) */
            using (var master = new EcMaster(settings, logger))
            {
                try
                {
                    //Console.WriteLine("Pre-Configure");
                    master.Configure(rootSlave);
                    //Console.WriteLine("Master Configured");
                }
                catch (Exception ex)
                {
                    myLog(EcUtilities.GetSlaveStateDescription(master.Context, slaves.SelectMany(x => x.Descendants()).ToList()), false);
                    logger.LogError(ex.Message);
                    throw;
                }

                /* start master */
                var random = new Random();
                var cts = new CancellationTokenSource();

                //byG              

                listMappedVariables(slaves);

                var pdoAnalogIn = slaves[0].DynamicData.Pdos;
                var varErrorCode = pdoAnalogIn[0].Variables.Where(x => x.Name == "Error code").First();
                var varStatusWord = pdoAnalogIn[0].Variables.Where(x => x.Name == "Statusword").First();
                var varPosActual = pdoAnalogIn[0].Variables.Where(x => x.Name == "Position actual value").First();
                var varModesOfOperationDisplay = pdoAnalogIn[0].Variables.Where(x => x.Name == "Modes of operation display").First();

                var varControlWord = pdoAnalogIn[4].Variables.Where(x => x.Name == "Controlword").First();
                var varTargetPosition = pdoAnalogIn[4].Variables.Where(x => x.Name == "Target position").First();
                var varModesOfOperation = pdoAnalogIn[4].Variables.Where(x => x.Name == "Modes of operation").First();


                var task = Task.Run(() =>
                {
                    var sleepTime = 1000 / (int)settings.CycleFrequency;
                    sleepTime = 1;
                    //sleepTime = 300;
                    myLog($"sleepTime: {sleepTime}", false);

                    //603fh
                    ushort ErrorCode = 0;
                    //6040h
                    ushort ControlWord = 0;
                    //6041h
                    ushort StatusWord = 0;
                    Int32 Status6bits = 0;
                    //6060h
                    sbyte ModesOfOperation = 0;
                    //6061h
                    sbyte ModesOfOperationDisplay = 0;
                    Int32 PositionActual = 0;
                    //607A
                    Int32 TargetPosition = 0;

                    var loopCounter = 0;
                    var StatusBits = 0;
                    var NextStatusBits = 0;

                    var cwName = "Controlword(6040h)";

                    //skips the initialisation
                    //StatusBits = 16;

                    while (!cts.IsCancellationRequested)
                    {
                        master.UpdateIO(DateTime.UtcNow);

                        unsafe
                        {
                            Span<ushort> myErrorCodeSpan = new Span<ushort>(varErrorCode.DataPtr.ToPointer(), 1);
                            if (ErrorCode != myErrorCodeSpan[0]) myLog($"ErrorCode is: {myErrorCodeSpan[0]:X4}");
                            ErrorCode = myErrorCodeSpan[0];

                            Span<ushort> myStatuswordSpan = new Span<ushort>(varStatusWord.DataPtr.ToPointer(), 1);
                            if (StatusWord != myStatuswordSpan[0]) myLog($"Statusword is: {myStatuswordSpan[0]:X4}h");
                            StatusWord = myStatuswordSpan[0];

                            Span<ushort> myControlwordSpan = new Span<ushort>(varControlWord.DataPtr.ToPointer(), 1);
                            //if (ControlWord != myControlwordSpan[0]) myLog($"Controlword is: {myControlwordSpan[0]:X4}h");
                            ControlWord = myControlwordSpan[0];

                            Span<int> myPosActualSpan = new Span<int>(varPosActual.DataPtr.ToPointer(), 1);
                            if (((StatusBits & 0b11111111) == 0b11111111) && (PositionActual != myPosActualSpan[0])) myLog($"PosActual is: {myPosActualSpan[0]}");
                            //if (PositionActual != myPosActualSpan[0]) myLog($"PosActual is: {myPosActualSpan[0]}");
                            PositionActual = myPosActualSpan[0];

                            Span<int> myTargetPositionSpan = new Span<int>(varTargetPosition.DataPtr.ToPointer(), 1);
                            if (TargetPosition != myTargetPositionSpan[0]) myLog($"Target Pos is: {myTargetPositionSpan[0]}");
                            TargetPosition = myTargetPositionSpan[0];

                            Span<sbyte> myModesOfOperation = new Span<sbyte>(varModesOfOperation.DataPtr.ToPointer(), 1);
                            if (ModesOfOperation != myModesOfOperation[0]) myLog($"ModesOfOperation is: {myModesOfOperation[0]:X4}h");
                            ModesOfOperation = myModesOfOperation[0];

                            Span<sbyte> myModesOfOperationDisplay = new Span<sbyte>(varModesOfOperationDisplay.DataPtr.ToPointer(), 1);
                            //if (ModesOfOperationDisplay != myModesOfOperationDisplay[0]) myLog($"ModesOfOperationDisplay is: {myModesOfOperationDisplay[0]:X4}h");
                            ModesOfOperationDisplay = myModesOfOperationDisplay[0];

                            //enable servo

                            //reset seems to work sometimes
                            if ((StatusWord & 0xF) == 0x08)
                            {
                                myLog($"Setting 80h (Fault Reset)");
                                StatusBits = 0;
                                myControlwordSpan[0] = 0x80;
                                //230806
                                myTargetPositionSpan[0] = 0; //Cancel enabling servo
                            }

                            //are we good to start - Status ending with 40h?
                            if (((StatusWord & 0x7F) == 0x40) && ((StatusBits & 0b1) == 0))
                            {
                                myLog($"setting ModesOfOperation(6060h) to 1");
                                myModesOfOperation[0] = 1;
                                //myModesOfOperation[0] = 8;
                                NextStatusBits = 1;

                            }

                            if (((StatusWord & 0x7F) == 0x40) && (StatusBits == 1))
                            {
                                myLog($"Setting {cwName} to 6h (Shutdown)");
                                StatusBits = 0b11;
                                myControlwordSpan[0] = 0x6;

                                myLog($"Setting 0x6081 Profile velocity to 0x900");
                                var dataset = new List<object>();
                                dataset.Add((ushort)0x900);
                                EcUtilities.SdoWrite(master.Context, 0, 0x6081, 0, dataset);
                            }

                            Status6bits = (StatusWord & 0x3F);

                            if ((Status6bits == 0x21) && ((StatusBits & 0b100) != 0b100))
                            {
                                myLog($"Setting {cwName} to 7h (Switch On)");
                                StatusBits = 0b111;
                                myControlwordSpan[0] = 0x7;
                            }
                            //should be 23
                            if (((Status6bits == 0x23) || (Status6bits == 0x33)) && ((StatusBits & 0b1000) != 0b1000))
                            {
                                myLog($"Setting {cwName} to Fh (Enable Operation)");
                                StatusBits = 0b1111;
                                myControlwordSpan[0] = 0xF;
                            }
                            //should be 27
                            if (((Status6bits == 0x27) || (Status6bits == 0x37)) && ((StatusBits & 0b10000) != 0b10000))
                            {
                                if ((StatusBits & 7) != 7)
                                {
                                    myLog("It seems the app was started with servo on!\nRestarting");
                                    //servo restarts anyway, probably Control is 7 from shutdown
                                    myControlwordSpan[0] = 0x7;
                                    StatusBits = 0b11110;
                                }
                                else
                                {
                                    myLog($"Servo enabled with Statusword: {myStatuswordSpan[0]:X4}h");
                                    NextStatusBits = 0b11111;
                                }
                            }

                            //if ((ModesOfOperation==1) && ((StatusBits & 16) != 16))
                            if (StatusBits == 0b11111)
                            {
                                NextStatusBits = 0b111111;
                                myLog($"Setting TargetPosition(607Ah) to -5 000 000!");
                                //myTargetPositionSpan[0] = PositionActual - 10;
                                myTargetPositionSpan[0] = -5000000;
                            }

                            if ((TargetPosition != 0) && ((StatusBits & 0b1000000) != 0b1000000))
                            {
                                NextStatusBits = 0b11111111;
                                myLog($"Setting {cwName} to 1Fh (New Set-poit)");
                                myControlwordSpan[0] = 0x1F;
                                //myControlwordSpan[0] = 0x3F;

                                //var dataset1 = new byte[2];
                                //EcUtilities.SdoRead(master.Context, 0, 0x6041, 1, ref dataset1);
                                //ushort usStatus = (ushort) BitConverter.ToInt16(dataset1, 0);
                                //myLog($"SDOStatusword is: {usStatus:X4}h");
                            }


                            Thread.Sleep(sleepTime);

                            if (NextStatusBits != 0) StatusBits = NextStatusBits;
                            NextStatusBits = 0;

                            ///this does not seem to work
                            if (cts.IsCancellationRequested)
                            {
                                myControlwordSpan[0] = 0x7;
                                master.UpdateIO(DateTime.UtcNow);
                                myLog($"Shutting down servo!");
                                Thread.Sleep(sleepTime);
                            }
                            loopCounter++;

                        }


                    }

                    //unsafe
                    //{
                    //    Console.WriteLine($"Loop ended!");
                    //    Span<ushort> myControlwordSpan = new Span<ushort>(varControlWord.DataPtr.ToPointer(), 1);
                    //    myControlwordSpan[0] = 0x7;
                    //    master.UpdateIO(DateTime.UtcNow);
                    //}
                }, cts.Token);

                    //Console.WriteLine("waiting for the key...");
                    /* wait for stop signal */
                    //Console.ReadKey(true);
                    //event_1.WaitOne();

                    //cts.Cancel();
                    await task;
                }

            }
            catch (Exception e)
            {
                myLog("\n\nMainEC Exception:", false);
                myLog("----------------------------------------------", false);
                myLog(e.Message, false);
            }
            return; /* remove this to run real world sample*/            
        }
        static void listMappedVariables(List<SlaveInfo> slaves)
        {
            var message = new StringBuilder();
            int iCounter = 0;
            foreach (var pdo in slaves[0].DynamicData.Pdos)
            {

                foreach (var variable in pdo.Variables)
                {
                    if (variable.DataPtr.ToInt64() != 0)

                        message.AppendLine($"{iCounter}.) '{variable.Name}', Idx: '{variable.Index:X4}h', DataPtr: '{variable.DataPtr.ToInt64()}'");
                    //message.AppendLine($"{iCounter}.) '{variable.Name}', Idx: '{variable.Index:X4}h', Len: {variable.BitLength}, Offset {variable.BitOffset}");
                    //message.AppendLine($"{iCounter}.) pdoName '{pdo.Name}' variableName: '{variable.Name}', DataPtr: '{variable.DataPtr.ToInt64()}', Len: {variable.BitLength}");
                }
                iCounter++;
            }
            Console.WriteLine(message.ToString().TrimEnd());
            //logger.LogInformation(message.ToString().TrimEnd());
        }
        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            myLog("\n\nApplication failed with unhandled exception:", false);
            myLog("----------------------------------------------", false);
            myLog(e.ExceptionObject.ToString(), false);
            myLog("\n\nPress Enter to continue", false);
            //Console.ReadLine();
            //Environment.Exit(1);
        }
        static async Task MainECMasterTester()
        {

            myLog("ver 231121.00");
            
            var cts = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                var sleepTime = 1000;
                
                while (!cts.IsCancellationRequested)
                {
                    //cts.Cancel();
                    Thread.Sleep(sleepTime);
                    myLog("master lives");
                    //Console.WriteLine("master lives");
                    if (mainForm != null && mainForm.bStop)
                    {
                        //event_1.Set();
                        cts.Cancel();
                    }
                }
            }, cts.Token);

            myLog("task.Id:" + task.ToString());
            //Console.ReadKey(true);
            //event_1.WaitOne();
            myLog("Waiting for the task");
            //cts.Cancel();
            await task;
            myLog("Task is done");
        }
    }       
}
