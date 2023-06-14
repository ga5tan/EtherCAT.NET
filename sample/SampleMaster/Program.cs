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


namespace SampleMaster
{
    class Program
    {
        static void listMappedVariables(List<SlaveInfo> slaves)
        {
            var message = new StringBuilder();
            int iCounter = 0;
            foreach (var pdo in slaves[0].DynamicData.Pdos)
            {

                foreach (var variable in pdo.Variables)
                {
                    if (variable.DataPtr.ToInt64() != 0)
                        message.AppendLine($"{iCounter}.) '{variable.Name}', Idx: '{variable.Index:X4}', Len: {variable.BitLength}, Offset {variable.BitOffset}");
                    //message.AppendLine($"{iCounter}.) pdoName '{pdo.Name}' variableName: '{variable.Name}', DataPtr: '{variable.DataPtr.ToInt64()}', Len: {variable.BitLength}");
                }
                iCounter++;
            }
            Console.WriteLine(message.ToString().TrimEnd());
            //logger.LogInformation(message.ToString().TrimEnd());
        }
        static async Task Main(string[] args)
        {
            System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            /* Set interface name. Edit this to suit your needs. */
            //var interfaceName = "eth0";
            //var interfaceName = "Wi-Fi";
            //var interfaceName = "Ethernet 3";
            var interfaceName = ConfigurationManager.AppSettings["interfaceName"];
            Console.WriteLine("ver 230614.01");
            Console.WriteLine("Connecting interfaceName:" + interfaceName + " (case sensitive)");

            /* Set ESI location. Make sure it contains ESI files! The default path is /home/{user}/.local/share/ESI */
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var esiDirectoryPath = Path.Combine(localAppDataPath, "ESI");
            Directory.CreateDirectory(esiDirectoryPath);

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

                /*################ Sample code START ##################

                // Example code to add SDO write request during initialization
                // to Beckhoff "EL3021"
                if (slave.ProductCode == 0xBCD3052)
                {
                    var dataset = new List<object>();
                    dataset.Add((byte)0x01);

                    var requests = new List<SdoWriteRequest>()
                    {
                        // Index 0x8000 sub index 6: Filter on
                        new SdoWriteRequest(0x8000, 0x6, dataset)   
                    };

                    slave.Extensions.Add(new InitialSettingsExtension(requests));
                }

                ################## Sample code END #################*/

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

                listMappedVariables(slaves);

                var pdoAnalogIn = slaves[0].DynamicData.Pdos;
                var varStatusword = pdoAnalogIn[0].Variables.Where(x => x.Name == "Statusword").First();
                var varPosActual = pdoAnalogIn[0].Variables.Where(x => x.Name == "Position actual value").First();
                var varControlword = pdoAnalogIn[4].Variables.Where(x => x.Name == "Controlword").First();
                var varTargetPosition = pdoAnalogIn[4].Variables.Where(x => x.Name == "Target position").First();
                var varModesOfOperation = pdoAnalogIn[4].Variables.Where(x => x.Name == "Modes of operation").First();


                var task = Task.Run(() =>
                {
                    var sleepTime = 1000 / (int)settings.CycleFrequency;
                    sleepTime = 2000;
                    Console.WriteLine($"sleepTime: {sleepTime}");

                    ushort Statusword = 0;
                    ushort Controlword = 0;
                    Int32 PositionActual = 0;
                    Int32 TargetPositionSpan = 0;
                    sbyte ModesOfOperation = 0;
                    var loopCounter = 0;     

                    while (!cts.IsCancellationRequested)                   
                    {                        
                        master.UpdateIO(DateTime.UtcNow);                        

                        unsafe
                        {
                            Span<ushort> myStatuswordSpan = new Span<ushort>(varStatusword.DataPtr.ToPointer(), 1);                            
                            if (Statusword != myStatuswordSpan[0])
                                Console.WriteLine($"Statusword is: {myStatuswordSpan[0]:X4}h/{myStatuswordSpan[0]}");
                            Statusword = myStatuswordSpan[0];

                            Span<ushort> myControlwordSpan = new Span<ushort>(varControlword.DataPtr.ToPointer(), 1);                            
                            if (Controlword != myControlwordSpan[0])
                                Console.WriteLine($"Controlword is: {myControlwordSpan[0]:X4}h");
                            Controlword = myControlwordSpan[0];

                            Span<int> myPosActualSpan = new Span<int>(varPosActual.DataPtr.ToPointer(), 1);                            
                            if (PositionActual != myPosActualSpan[0])
                                Console.WriteLine($"PosActual is: {myPosActualSpan[0]}");
                            PositionActual = myPosActualSpan[0];

                            Span<int> myTargetPositionSpan = new Span<int>(varTargetPosition.DataPtr.ToPointer(), 1);                            
                            if (TargetPositionSpan != myTargetPositionSpan[0])
                                Console.WriteLine($"Target Pos is: {myTargetPositionSpan[0]}");
                            TargetPositionSpan = myTargetPositionSpan[0];

                            Span<sbyte> myModesOfOperation = new Span<sbyte>(varModesOfOperation.DataPtr.ToPointer(), 1);
                            if (ModesOfOperation != myModesOfOperation[0])
                                Console.WriteLine($"ModesOfOperation is: {myModesOfOperation[0]:X4}h/{myStatuswordSpan[0]}");
                            ModesOfOperation = myModesOfOperation[0];

                            //enable servo
                            //if (loopCounter == 1)
                            //{
                            //    Console.WriteLine($"Enabling 6");
                            //    myControlwordSpan[0] = 0x6;
                            //}
                            //if (loopCounter == 2)
                            //{
                            //    Console.WriteLine($"Enabling 7");
                            //    myControlwordSpan[0] = 0x7;
                            //}
                            //if (loopCounter == 3)
                            //{
                            //    Console.WriteLine($"Enabling servo");
                            //    myControlwordSpan[0] = 0xF;
                            //}
                            //if (loopCounter == 9)
                            //{
                            //    Console.WriteLine($"Target Pos GO!");
                            //    myControlwordSpan[0] = 0x1F;
                            //    myModesOfOperation[0] = 1;
                            //    myTargetPositionSpan[0] = 5000000;
                            //}

                            loopCounter++;
                            
                        }
                        

                        //unsafe
                        //{
                        //    void* data = varStatusword.DataPtr.ToPointer();
                        //    int bitmask = (1 << varStatusword.BitLength) - 1;
                        //    int shift = (*(int*)data >> varStatusword.BitOffset) & bitmask;
                        //    ushort myStatusword = (ushort)shift;
                        //    //if (Statusword != myStatusword) 
                        //        Console.WriteLine($"Statusword: {myStatusword}");
                        //    Statusword = myStatusword;
                        //}


                        //Console.WriteLine("sleeping");
                        Thread.Sleep(sleepTime);
                    }
                }, cts.Token);

                Console.WriteLine("waiting for the key...");
                /* wait for stop signal */
                Console.ReadKey(true);

                cts.Cancel();
                await task;
            }
            Console.WriteLine("return");
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
