using Fanex.Bot.Client;
using Fanex.Bot.Client.Enums;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using SonarQube.Api;
using SonarQube.Api.Models;
using SonarQube.ScheduledReport.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            var violatedProjects = new Dictionary<ProjectModel, QualityGateStatusModel>();
            var projects = await sonarQubeApiService.GetAllProjects();

            foreach (var project in projects)
            {
                var gateStatus = await sonarQubeApiService.GetQualityGateStatusOfProject(project.Key);

                if (gateStatus.ProjectStatus.Status.Equals("ERROR", System.StringComparison.OrdinalIgnoreCase))
                {
                    violatedProjects.Add(project, gateStatus);
                }
            }

            if (violatedProjects.Count > 0)
            {
                var botConnector = new BotConnector();
                var conversationIds = AppSetting.GetConversationIds();
                string messageContent = GenerateTemplateContent(new ViolationReportModel { ViolatedProjects = violatedProjects }, @"Templates\ViolationReport.cshtml");

                foreach (var conversationId in conversationIds)
                {
                    string errorMessage = botConnector.Send(messageContent, conversationId, MessageType.XML);

                    if (errorMessage != HttpStatusCode.OK.ToString())
                    {
                        throw new SkynexBotException($"There is an error while sending message by Skynex (Status code: {errorMessage})");
                    }
                }
            }
        }

        private static string GenerateTemplateContent(object model, string relativeTemplatePath)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string templatePath = Path.Combine(appPath, relativeTemplatePath);
            string resultContent = string.Empty;

            if (File.Exists(templatePath))
            {
                var config = new TemplateServiceConfiguration
                {
                    EncodedStringFactory = new HtmlEncodedStringFactory(),
                    DisableTempFileLocking = true,
                    CachingProvider = new DefaultCachingProvider(t => { })
                };
                Engine.Razor = RazorEngineService.Create(config);

                string templateContent = File.ReadAllText(templatePath);

                resultContent = Engine.Razor.RunCompile(templateContent, relativeTemplatePath, model.GetType(), model);
            }

            return resultContent;
        }
    }
}
