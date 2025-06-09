using SiLAClient.Properties;
using SiLAClient.ViewModels;
using SiLAClient.ExperimentStatusService;
using SiLAClient.ExperimentService;
using SiLAClient.RunService;
using SiLAClient.AutomationStudioRemote;
using SiLAClient.AutomationStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Tecan.Sila2.Discovery;
using Caliburn.Micro;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace SiLAClient
{
    class TestProgram
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("server discovery");
            var logging = new ConsoleLogging();

            var executionManager = new DiscoveryExecutionManager();
            var discovery = new ServerDiscovery(new ServerConnector(executionManager, logging));
            var servers = discovery.GetServers(TimeSpan.FromSeconds(Settings.Default.ScanInterval));

            Console.WriteLine("server discovery finished");

            foreach (var citem in servers)
            {
                var serverDataItem = discovery.ConnectedServers.Values.FirstOrDefault(x => x.Item4 == citem.Config.Uuid);
                CustomServerData item = new CustomServerData(citem, serverDataItem?.Item2, serverDataItem?.Item3);

                Console.WriteLine(String.Format("type {0} data {1} host {2} port {3}", citem.Info.Type, item.Data, item.Host, item.Port));

                //Console.WriteLine("starting AS remote");
                //if (citem.Info.Type == "AutomationRemote")
                //{
                //    try
                //    {
                //        var client = new AutomationStudioRemoteClient(citem.Channel, executionManager);
                //        var message = client.Start();
                //    }
                //    catch (Exception ex)
                //    {
                //        dynamic response = new ExpandoObject();
                //        response.Status = "Failure";
                //        response.Content = "";
                //        response.Error = ex.Message;
                //        response.StatusCode = -99;
                //        Console.WriteLine(response);
                //    }
                //}
                if (citem.Info.Type == "AutomationStudio")
                {
                    try
                    {
                        ExperimentStatusServiceClient statusClient;
                        ExperimentServiceClient experimentClient;
                        RunServiceClient runClient;
                        JObject response_json;
                        DateTime localDate;

                        Console.WriteLine("getexperimentstatus");
                        statusClient = new ExperimentStatusServiceClient(citem.Channel, executionManager);
                        var message = statusClient.GetExperimentStatus();
                        Console.WriteLine(message);

                        Console.WriteLine("getstatus");
                        message = statusClient.GetStatus();
                        Console.WriteLine(message);

                        Console.WriteLine("getinformation");
                        experimentClient = new ExperimentServiceClient(citem.Channel, executionManager);
                        message = experimentClient.GetInformation();
                        Console.WriteLine(message);

                        Console.WriteLine("choosedesignid");
                        message = experimentClient.ChooseDesignID(290);
                        Console.WriteLine(message);

                        Console.WriteLine("setprompts");
                        message = experimentClient.SetPrompts(@"C:\Users\Unchained Labs\Documents\DCIEDD\AS.prompts.WithDesignCreator.xml");
                        Console.WriteLine(message);

                        //Console.WriteLine("setchemicalmanager");
                        //message = experimentClient.SetChemicalManager(@"C:\Users\Unchained Labs\Documents\DCIEDD\chemical_mgr_water_example.xml");
                        //Console.WriteLine(message);

                        Console.WriteLine("start");
                        runClient = new RunServiceClient(citem.Channel, executionManager);
                        message = runClient.Start();
                        Console.WriteLine(message);

                        //Console.WriteLine("abort");
                        //message = runClient.Abort();
                        //Console.WriteLine(message);

                        while (true)
                        {
                            Console.WriteLine("getactiveprompt");
                            message = statusClient.GetActivePrompt();
                            Console.WriteLine(message);
                            response_json = JObject.Parse(message);
                            //if (response_json["StatusCode"].Value<int>() == 0)
                            //{
                            //    Console.WriteLine(JObject.Parse(response_json["Content"].Value<string>())["Option"].Value<string>());
                            //    Console.WriteLine("setinput");
                            //    message = statusClient.SetInput("Abort");
                            //    Console.WriteLine(message);
                            //}

                            //Console.WriteLine("getstatus");
                            //message = statusClient.GetStatus();
                            //Console.WriteLine(message);

                            Console.WriteLine("getexperimentstatus");
                            message = statusClient.GetExperimentStatus();
                            //Console.WriteLine(message);

                            localDate = DateTime.Now;
                            var culture = new CultureInfo("en-US");
                            Console.WriteLine("timestamp: {0}", localDate.ToString(culture));

                            response_json = JObject.Parse(message);

                            Console.WriteLine(String.Format("status: {0}", response_json["Content"]["Status"].Value<string>()));
                            Console.WriteLine(String.Format("current action: {0}", response_json["Content"]["CurrentAction"].Value<string>()));
                            Console.WriteLine(String.Format("current map: {0}", response_json["Content"]["CurrentMap"].Value<string>()));

                            Thread.Sleep(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        dynamic response = new ExpandoObject();
                        response.Status = "Failure";
                        response.Content = "";
                        response.Error = ex.Message;
                        response.StatusCode = -99;
                        Console.WriteLine(response);
                    }
                }
            }
        }
    }
}
