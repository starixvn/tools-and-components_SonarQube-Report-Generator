using Fanex.Bot.Client;
using SonarQube.Api;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SonarQube.ScheduledReport
{
    class Program
    {
        private static SonarQubeApiService sonarQubeApiService;

        static void Main(string[] args)
        {
            sonarQubeApiService = new SonarQubeApiServiceImpl(AppSetting.SonarQubeUrl, AppSetting.SonarQubeUserToken);
            BotClientManager.UseConfig(
                new Fanex.Bot.Client.Configuration.BotSettings(
                        AppSetting.BotServiceUrl,
                        AppSetting.BotClientId,
                        AppSetting.BotClientPassword,
                        AppSetting.BotClientTimeout));

            Task.Factory.StartNew(SendSonarQubeViolationReport)
                        .Unwrap()
                        .GetAwaiter()
                        .GetResult();
        }

        private static async Task SendSonarQubeViolationReport()
        {
            //var violatedProjects = new Dictionary<string, QualityGateStatusModel>();
            //var projects = await sonarQubeApiService.GetAllProjects();

            //foreach (var project in projects)
            //{
            //    var gateStatus = await sonarQubeApiService.GetQualityGateStatusOfProject(project.Key);

            //    if (gateStatus.ProjectStatus.Status.Equals("ERROR", System.StringComparison.OrdinalIgnoreCase))
            //    {
            //        violatedProjects.Add(project.Name, gateStatus);
            //    }
            //}

            //if (violatedProjects.Count > 0)
            //{
                var botConnector = new BotConnector();
                var conversationIds = AppSetting.GetConversationIds();
                string messageContent = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ViolationReport.md"));

                foreach (var conversationId in conversationIds)
                {
                    botConnector.Send(messageContent, conversationId);
                }

                //foreach (var pair in violatedProjects)
                //{
                //    pair.Value.ProjectStatus.
                //}

                //botConnector.Send(message: "hello", "conversationId");
            //}
        }
    }
}
