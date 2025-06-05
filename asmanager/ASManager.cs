using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiLAClient.Properties;
using SiLAClient.ViewModels;
using SiLAClient.ExperimentStatusService;
using SiLAClient.ExperimentService;
using SiLAClient.RunService;
using SiLAClient.AutomationStudioRemote;
using SiLAClient.AutomationStudio;
using Tecan.Sila2.Discovery;
using Tecan.Sila2;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.CodeDom;

namespace SiLAClient
{
    public class ASManager
    {
        private ExperimentStatusServiceClient statusClient;
        private ExperimentServiceClient serviceClient;
        private RunServiceClient runClient;
        private ServerDiscovery discovery;
        private DiscoveryExecutionManager executionManager;
        private readonly int totalTries = 4;

        public ASManager()
        {
            int numTries = 0;
            bool isASRunning = false;
            while (!isASRunning && numTries < totalTries) {
                IEnumerable<ServerData> servers = GetAllServers();
                foreach (var citem in servers)
                {
                    var serverDataItem = discovery.ConnectedServers.Values.FirstOrDefault(x => x.Item4 == citem.Config.Uuid);
                    CustomServerData item = new CustomServerData(citem, serverDataItem?.Item2, serverDataItem?.Item3);

                    Console.WriteLine(String.Format("type {0} data {1} host {2} port {3}", citem.Info.Type, item.Data, item.Host, item.Port));
                    if (citem.Info.Type == "AutomationStudio")
                    {
                        isASRunning = true;
                        try
                        {
                            statusClient = new ExperimentStatusServiceClient(citem.Channel, executionManager);
                            serviceClient = new ExperimentServiceClient(citem.Channel, executionManager);
                            runClient = new RunServiceClient(citem.Channel, executionManager);
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
                    else
                    {
                        if (citem.Info.Type == "AutomationRemote")
                        {
                            try
                            {
                                Console.WriteLine("starting AS remote");
                                var client = new AutomationStudioRemoteClient(citem.Channel, executionManager);
                                var message = client.Start();
                                Console.WriteLine(message);
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
                        else
                        {
                            throw new InvalidOperationException("Automation Remote is currently not running. Manually run it first");
                        }
                    }    
                }
                numTries += 1;
            }

            if (!isASRunning)
            {
                throw new InvalidOperationException("Unable to communicate with SiLA to start Automation Studio");
            }
        }

        private IEnumerable<ServerData> GetAllServers()
        {
            Console.WriteLine("server discovery");
            var logging = new ConsoleLogging();

            executionManager = new DiscoveryExecutionManager();
            discovery = new ServerDiscovery(new ServerConnector(executionManager, logging));
            var servers = discovery.GetServers(TimeSpan.FromSeconds(Settings.Default.ScanInterval));

            Console.WriteLine("server discovery finished");
            return servers;
        }

        public string GetExperimentStatus()
        {
            string message = statusClient.GetExperimentStatus();
            return message;
        }

        public string GetStatus()
        {
            string message = statusClient.GetStatus();
            return message;
        }

        public string PollPromptRaw()
        {
            string message = statusClient.GetActivePrompt();
            return message;
        }

        public JObject PollPrompt()
        {
            string message = PollPromptRaw();
            JObject messageJson = JObject.Parse(message);
            return messageJson;
        }

        public string SetPromptInput(string response)
        {
            string message = statusClient.SetInput(response);
            return message;
        }

        public string RunDesign(int id, string prompts, string chemManager, string tipManager = null)
        {
            string message;
            message = serviceClient.SetPrompts(null);
            Console.WriteLine(message);
            message = serviceClient.SetChemicalManager(null);
            Console.WriteLine(message);
            message = serviceClient.SetTipManagement(null);
            Console.WriteLine(message);

            message = serviceClient.ChooseDesignID(id);
            Console.WriteLine(message);
            if (ParseMessageError(message).Item2 != 0)
            {
                return message;
            }

            //message = serviceClient.SetPrompts(null);
            //Console.WriteLine(message);
            message = serviceClient.SetPrompts(prompts);
            Console.WriteLine(message);
            if (ParseMessageError(message).Item2 != 0)
            {
                return message;
            }

            //message = serviceClient.SetChemicalManager(null);
            //Console.WriteLine(message);
            message = serviceClient.SetChemicalManager(chemManager);
            Console.WriteLine(message);
            if (ParseMessageError(message).Item2 != 0)
            {
                return message;
            }

            //message = serviceClient.SetTipManagement(null);
            //Console.WriteLine(message);
            if (tipManager != null)
            {
                message = serviceClient.SetTipManagement(tipManager);
                Console.WriteLine(message);
                if (ParseMessageError(message).Item2 != 0)
                {
                    return message;
                }
            }

            message = runClient.Start();
            Console.WriteLine(message);
            return message;
        }

        private Tuple<string, int> ParseMessageError(string message)
        {
            JObject messageJson = JObject.Parse(message);
            return Tuple.Create(messageJson["Error"].Value<string>(), messageJson["StatusCode"].Value<int>());
        }
    }
}
