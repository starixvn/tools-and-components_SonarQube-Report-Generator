using Fanex.Bot.Client;
using Fanex.Bot.Client.Enums;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using SharpRaven;
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            LogManager
                .SetDefaultLogCategory(AppSetting.SentryEnvironment)
                .Use(new List<ILogEngine>
                {
                     new SentryLogging(new SentryEngineOptions {
                        Dsn = new Dsn(AppSetting.SentryDsn),
                        Environment = AppSetting.SentryEnvironment
                     })
                });

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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                LogService.Error(exception.Message, exception);
            }
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
                    string statusCode = botConnector.Send(messageContent, conversationId, MessageType.XML);

                    if (statusCode != HttpStatusCode.OK.ToString())
                    {
                        throw new SkynexBotException($"There is an error while sending message by Skynex (Status code: {statusCode})");
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
