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
using System.Collections.Generic;
using EtherCAT.NET.Infrastructure;
using EtherCAT.NET.Extension;

using System.Configuration;
using System.Data;
using System.Net.NetworkInformation;


namespace SampleMaster
{
    class Program
    {
        static void myLog(string sMsg)
        {
            Console.WriteLine($"{DateTime.UtcNow.ToString("hh:mm:ss.fff tt")}: {sMsg.TrimEnd()}");
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
                        message.AppendLine($"{iCounter}.) '{variable.Name}', Idx: '{variable.Index:X4}h', Len: {variable.BitLength}, Offset {variable.BitOffset}");
                    //message.AppendLine($"{iCounter}.) pdoName '{pdo.Name}' variableName: '{variable.Name}', DataPtr: '{variable.DataPtr.ToInt64()}', Len: {variable.BitLength}");
                }
                iCounter++;
            }
            Console.WriteLine(message.ToString().TrimEnd());
            //logger.LogInformation(message.ToString().TrimEnd());
        }

        static async Task Main2(string[] args)
        {
            Console.WriteLine("Tester");
            ushort StatusWord = 0x8640;
            var myControlwordSpan = new ushort[1];
            var StatusBits = 0;
            var loopCounter = 0;

            Console.WriteLine("{0:X}", (0x8637 & 0x3F));
            Console.WriteLine("{0:X}", (0x8637 & 0x3F) & 0x27);
            Console.WriteLine("{0:X}",  (0x8637 & 0x27));

            var cwName = "Controlword(6040h)";
            while (false)
            {
                if (StatusBits == 0)
                {
                    myLog($"{loopCounter}: Setting {cwName} to 6h (Shutdown)");
                    StatusBits = 1;
                    myControlwordSpan[0] = 0x6;
                }
                if (((StatusWord & 0x21) == 0x21) && ((StatusBits & 2) != 2))
                {
                    myLog($"{loopCounter}: Setting {cwName} to 7h (Switch On) {(StatusWord & 0x21):X4}");
                    StatusBits = 3;
                    myControlwordSpan[0] = 0x7;
                }
                if (((StatusWord & 0x23) == 0x23) && ((StatusBits & 4) != 4))
                {
                    myLog($"{loopCounter}: Setting {cwName} to Fh (Enable Operation)");
                    StatusBits = 7;
                    myControlwordSpan[0] = 0xF;
                }
                if (((StatusWord & 0x27) == 0x27) && ((StatusBits & 8) != 8))
                {
                    myLog($"{loopCounter}: Ready to go!");
                    StatusBits = 15;                 
                }

                Thread.Sleep(500);
                loopCounter++;

                if (loopCounter == 1) StatusWord = 0x8621;
                if (loopCounter == 2) StatusWord = 0x8623;
                if (loopCounter == 3) StatusWord = 0x8627;
                
            }
        }
        static async Task Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            /* Set interface name. Edit this to suit your needs. */
            //var interfaceName = "eth0";
            //var interfaceName = "Wi-Fi";
            //var interfaceName = "Ethernet 3";
            var interfaceName = ConfigurationManager.AppSettings["interfaceName"];
            Console.WriteLine("ver 231110.00");
            Console.WriteLine("Connecting interfaceName:" + interfaceName + " (case sensitive)");

            /* Set ESI location. Make sure it contains ESI files! The default path is /home/{user}/.local/share/ESI */
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var esiDirectoryPath = Path.Combine(localAppDataPath, "ESI");
            Directory.CreateDirectory(esiDirectoryPath);

            //C:\Users\Flexicam\AppData\Local\ESI
            Console.WriteLine($"esiDirectoryPath: {esiDirectoryPath}");

            /* Copy native file. NOT required in end user scenarios, where EtherCAT.NET package is installed via NuGet! */
            var codeBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Directory.EnumerateFiles(Path.Combine(codeBase, "runtimes"), "*soem_wrapper.*", SearchOption.AllDirectories).ToList().ForEach(filePath =>
            {
                if (filePath.Contains(RuntimeEnvironment.RuntimeArchitecture))
                    File.Copy(filePath, Path.Combine(codeBase, Path.GetFileName(filePath)), true);
            });

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

            Console.WriteLine($"Slaves: {rootSlave.Descendants().Count()}");

            rootSlave.Descendants().ToList().ForEach(slave =>
            {
                // If you have special extensions for this slave, add it here:                    
                // slave.Extensions.Add(new MyFancyExtension());                

                //var dataset = new List<object>();
                //dataset.Add((uint)0x000000FF);
                
                //var requests = new List<SdoWriteRequest>()
                //{                        
                //    new SdoWriteRequest(0x6081, 0x0, dataset)
                //};

                //slave.Extensions.Add(new InitialSettingsExtension(requests));

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
                    Console.WriteLine(EcUtilities.GetSlaveStateDescription(master.Context, slaves.SelectMany(x => x.Descendants()).ToList()));
                    logger.LogError(ex.Message);
                    throw;
                }                                            

                /* start master */
                var random = new Random();
                var cts = new CancellationTokenSource();

                //byG              

                //listMappedVariables(slaves);

                var pdoAnalogIn = slaves[0].DynamicData.Pdos;
                var slaveIndex = (ushort)(Convert.ToUInt16(slaves.ToList().IndexOf(slaves[0])) + 1);
                myLog($"slaveIndex:{slaveIndex}");
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
                    sleepTime = 30;
                    //sleepTime = 300;
                    Console.WriteLine($"sleepTime: {sleepTime}");

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
                            if (((StatusBits & 0b11111111) == 0b11111111) && (PositionActual != myPosActualSpan[0])) myLog($"PosActual is: {myPosActualSpan[0]:#,##0}");                            
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

                                //231108 - it actually seems, that SDOWrite does not work. It only works, when we set it up in TwinCAT
                                ////works
                                dataset.Add((uint)0x00FFBBCC);
                                //dataset.Add((uint)0x0);
                                dataset.Add((byte)0x00);//needs to be here
                                
                                EcUtilities.SdoWrite(master.Context, slaveIndex, 0x6081, 0, dataset);
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
                                else { 
                                    myLog($"Servo enabled with Statusword: {myStatuswordSpan[0]:X4}h");
                                    NextStatusBits = 0b11111;                        
                                }
                            }

                            //if ((ModesOfOperation==1) && ((StatusBits & 16) != 16))
                            if (StatusBits == 0b11111)
                            {
                                NextStatusBits = 0b111111;
                                myLog($"Setting TargetPosition(607Ah) to 50 000 000!");
                                //myTargetPositionSpan[0] = PositionActual - 10;
                                myTargetPositionSpan[0] = -50000000;
                            }

                            if ((TargetPosition != 0) && ((StatusBits & 0b1000000) != 0b1000000))
                            {
                                NextStatusBits = 0b11111111;
                                myLog($"Setting {cwName} to 1Fh (New Set-poit)");
                                myControlwordSpan[0] = 0x1F;
                                //myControlwordSpan[0] = 0x3F;

                                var dataset1 = new byte[4];
                                //var dataset1 = new byte[2];

                                EcUtilities.SdoRead(master.Context, slaveIndex, 0x6081, 0, ref dataset1);
                                //uint uiProfileSpeed = BitConverter.ToUInt16(dataset1, 0);
                                uint uiProfileSpeed = BitConverter.ToUInt32(dataset1, 0);
                                myLog($"ProfileSpeed is: {uiProfileSpeed:X8}h");

                                

                                //ushort usStatus = (ushort) BitConverter.ToInt16(dataset1, 0);
                                //myLog($"SDOStatusword is: {usStatus:X4}h");
                            }


                            //CSP control 230709
                            //if (StatusBits == 0b1111111)
                            //{
                            //    myTargetPositionSpan[0] = PositionActual - 10;
                            //}
                                //if ((StatusWord == 0x9637) && ((StatusBits & 0b10000000) != 0b10000000))
                                //{
                                //    NextStatusBits = 0b11111111;

                                //    //ushort mask = 1 << 4;
                                //    //myControlwordSpan[0] = (ushort)(myControlwordSpan[0] & ~mask);

                                //    myLog($"We got rising edge (bit12 os Statusword is 1), setting {cwName} bit4 to 0h");
                                //    //myControlwordSpan[0] = 0x0;
                                //}

                                Thread.Sleep(sleepTime);

                            if (NextStatusBits != 0) StatusBits = NextStatusBits;
                            NextStatusBits = 0;

                            ///this does not seem to work
                            if (cts.IsCancellationRequested)
                            {
                                myControlwordSpan[0] = 0x7;
                                master.UpdateIO(DateTime.UtcNow);
                                Console.WriteLine($"Shutting down servo!");
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
                Console.ReadKey(true);

                cts.Cancel();
                await task;
            }
            //Console.WriteLine("return");
            return; /* remove this to run real world sample*/

            /* create EC Master (real world sample) */
            using (var master = new EcMaster(settings, logger))
            {
                try
                {
                    master.Configure(rootSlave);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    throw;
                }

                /*################ Sample code START ##################
                 
                // Beckhoff EL2004 (4 channel digital output)
                var eL2004 = new DigitalOut(slaves[1]);

                eL2004.SetChannel(1, false);
                eL2004.SetChannel(2, true);
                eL2004.SetChannel(3, false);
                eL2004.SetChannel(4, true);

                // Beckhoff EL1014 (4 channel digital input)
                var eL1014 = new DigitalIn(slaves[2]);

                // Beckhoff EL3021 (1 channel analog input - 16bit)
                var pdoAnalogIn = slaves[3].DynamicData.Pdos;
                var varAnalogIn = pdoAnalogIn[0].Variables.Where(x => x.Name == "Value").First();
                var varUnderrange = pdoAnalogIn[0].Variables.Where(x => x.Name == "Status__Underrange").First();
                var varOverrange = pdoAnalogIn[0].Variables.Where(x => x.Name == "Status__Overrange").First();

                // Beckhoff EL3021 SDO read (index: 0x8000 sub index: 0x6)
                var datasetFilter = new byte[2];
                EcUtilities.SdoRead(master.Context, 4, 0x8000, 6, ref datasetFilter);
                var filterOn = BitConverter.ToBoolean(datasetFilter, 0);
                logger.LogInformation($"EL3021 filter on: {filterOn}");

                ################## Sample code END #################*/

                /* start master */
                var random = new Random();
                var cts = new CancellationTokenSource();

                var task = Task.Run(() =>
                {
                    var sleepTime = 1000 / (int)settings.CycleFrequency;

                    while (!cts.IsCancellationRequested)
                    {
                        master.UpdateIO(DateTime.UtcNow);

                        /*################ Sample code START ##################

                        // Beckhoff EL2004 toggle digital output for ch1 and ch3
                        eL2004.ToggleChannel(2);
                        eL2004.ToggleChannel(4);

                        // Beckhoff EL1014 read digital input state 
                        logger.LogInformation($"EL1014 channel 1 input: {eL1014.GetChannel(1)}");
                        logger.LogInformation($"EL1014 channel 2 input: {eL1014.GetChannel(2)}");
                        logger.LogInformation($"EL1014 channel 3 input: {eL1014.GetChannel(3)}");
                        logger.LogInformation($"EL1014 channel 4 input: {eL1014.GetChannel(4)}");

                        // Beckhoff EL2004 read digital output state 
                        logger.LogInformation($"EL1014 channel 1 output: {eL2004.GetChannel(1)}");
                        logger.LogInformation($"EL1014 channel 2 output: {eL2004.GetChannel(2)}");
                        logger.LogInformation($"EL1014 channel 3 output: {eL2004.GetChannel(3)}");
                        logger.LogInformation($"EL1014 channel 4 output: {eL2004.GetChannel(4)}");
                   
                        // Beckhoff EL3021 SDO read (index: 0x6000 sub index: 0x2)
                        // overrange of 12 bit analog input.
                        var slaveIndex = (ushort)(Convert.ToUInt16(slaves.ToList().IndexOf(slaves[3])) + 1);
                        var dataset1 = new byte[2];
                        EcUtilities.SdoRead(master.Context, slaveIndex, 0x6000, 2, ref dataset1);
                        bool overrange = BitConverter.ToBoolean(dataset1, 0);
                        logger.LogInformation($"EL3021 overrange: {overrange}");

                        // Beckhoff EL3021 SDO read (index: 0x6000 sub index: 0x1)
                        // underrange of 12 bit analog input.
                        var dataset2 = new byte[2];
                        EcUtilities.SdoRead(master.Context, slaveIndex, 0x6000, 1, ref dataset2);
                        bool underrange = BitConverter.ToBoolean(dataset2, 0);
                        logger.LogInformation($"EL3021 underrange: {underrange}");
                        
                        ################## Sample code END #################*/

                        unsafe
                        {
                            if (variables.Any())
                            {
                                /*################ Sample code START ##################

                                // Read analog current from EL3021 (16 bit - PDO) 
                                void* data = varAnalogIn.DataPtr.ToPointer();
                                int bitmask = (1 << varAnalogIn.BitLength) - 1;
                                int shift = (*(int*)data >> varAnalogIn.BitOffset) & bitmask;
                                short analogIn = (short)shift; 
                                logger.LogInformation($"EL3021 analog current in: {analogIn}");

                                // Read analog current underrange status (1 bit - PDO) 
                                void* dataUnder = varUnderrange.DataPtr.ToPointer();
                                bitmask = (1 << varUnderrange.BitLength) - 1;
                                int under = (*(int*)dataUnder >> varUnderrange.BitOffset) & bitmask;
                                logger.LogInformation($"EL3021 underrange: {under}");

                                // Read analog current overrange status (1 bit - PDO) 
                                void* dataOver = varOverrange.DataPtr.ToPointer();
                                bitmask = (1 << varOverrange.BitLength) - 1;
                                int over = (*(int*)dataOver >> varOverrange.BitOffset) & bitmask;
                                logger.LogInformation($"EL3021 overrange: {over}");

                                ################## Sample code END #################*/
                            }
                        }

                        Thread.Sleep(sleepTime);
                    }
                }, cts.Token);

                /* wait for stop signal */
                Console.ReadKey(true);

                cts.Cancel();
                await task;
            }
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("\n\nApplication failed with unhandled exception:");
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("\n\nPress Enter to continue");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}
