using Fanex.Bot.Client;
using Fanex.Bot.Client.Enums;
using Fanex.Logging;
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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SonarQube.ScheduledReport.Services
{
    public interface ReportService
    {
        Task SendSonarQubeExclusionReport(List<ProjectModel> projects);

        Task SendSonarQubeViolationReport(List<ProjectModel> projects);

        Task<List<ProjectModel>> GetFilteredProjects();
    }

    public class ReportServiceImpl : ReportService
    {
        private readonly SonarQubeApiService sonarQubeApiService;
        private static readonly string[] ExclusionSettings = new[] { "sonar.coverage.exclusions", "sonar.cpd.exclusions", "sonar.exclusions", "sonar.issue.ignore.allfile", "sonar.issue.ignore.block" };

        public ReportServiceImpl(SonarQubeApiService sonarQubeApiService)
        {
            this.sonarQubeApiService = sonarQubeApiService ?? new SonarQubeApiServiceImpl(AppSetting.SonarQubeUrl, AppSetting.SonarQubeUserToken);
        }

        public async Task SendSonarQubeExclusionReport(List<ProjectModel> projects)
        {
            try
            {
                var reportedProjects = new Dictionary<ProjectModel, List<ProjectSettingKeyValue>>();

                foreach (var project in projects)
                {
                    var setting = await sonarQubeApiService.GetProjectSetting(project.Key);
                    var exclusionSettings = setting.Settings.Where(x => ExclusionSettings.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToList();

                    if (exclusionSettings.Count > 0)
                    {
                        reportedProjects.Add(project, exclusionSettings);
                    }
                }

                string messageContent = GenerateTemplateContent(new ExclusionReportModel { Projects = reportedProjects }, @"Templates\ExclusionReport.cshtml");

                SendBotMessage(messageContent);
            }
            catch (Exception ex)
            {
                LogService.Error(ex.Message, ex);
            }
        }

        public async Task SendSonarQubeViolationReport(List<ProjectModel> projects)
        {
            try
            {
                var violatedProjects = new Dictionary<ProjectModel, QualityGateStatusModel>();

                foreach (var project in projects)
                {
                    var gateStatus = await sonarQubeApiService.GetQualityGateStatusOfProject(project.Key);

                    if (gateStatus.ProjectStatus.Status.Equals("ERROR", System.StringComparison.OrdinalIgnoreCase))
                    {
                        violatedProjects.Add(project, gateStatus);
                    }
                }

                string messageContent = GenerateTemplateContent(new ViolationReportModel { ViolatedProjects = violatedProjects }, @"Templates\ViolationReport.cshtml");

                SendBotMessage(messageContent);
            }
            catch (Exception ex)
            {
                LogService.Error(ex.Message, ex);
            }
        }

        public async Task<List<ProjectModel>> GetFilteredProjects()
        {
            var projects = await sonarQubeApiService.GetAllProjects();
            var filteredProjects = new List<ProjectModel>();
            var patterns = AppSetting.GetIgnoredProjectNamePatterns();

            foreach (var project in projects)
            {
                bool ignored = false;
                int i = 0;

                while (!ignored && i < patterns.Count)
                {
                    var pattern = patterns[i];

                    ignored = Regex.IsMatch(project.Name, pattern);

                    i++;
                }

                if (!ignored)
                {
                    filteredProjects.Add(project);
                }
            }

            return filteredProjects;
        }

        private static void SendBotMessage(string messageContent)
        {
            var botConnector = new BotConnector();
            var conversationIds = AppSetting.GetConversationIds();

            foreach (var conversationId in conversationIds)
            {
                string statusCode = botConnector.Send(messageContent, conversationId, MessageType.XML);

                if (statusCode != HttpStatusCode.OK.ToString("D"))
                {
                    throw new SkynexBotException($"There is an error while sending message by Skynex (Status code: {statusCode})");
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
