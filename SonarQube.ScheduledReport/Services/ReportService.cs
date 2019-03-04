using Fanex.Bot.Client;
using Fanex.Bot.Client.Enums;
using Fanex.Logging;
using Fanex.MailDeliver.MailClient.API;
using Fanex.MailDeliver.Model;
using LINQtoCSV;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using SonarQube.Api.Models;
using SonarQube.Api.Services;
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
        Task SendSonarQubeServerExclusionReport(List<ProjectModel> projects);

        Task SendSonarQubeViolationReport(List<ProjectModel> projects);

        Task<List<ProjectModel>> GetFilteredProjects();

        void SendSonarQubeProjectExclusionReportByEmail(List<ProjectModel> projects);
    }

    public class ReportServiceImpl : ReportService
    {
        private readonly SonarQubeApiService sonarQubeApiService;
        private readonly ConfigService configService;
        private static readonly string[] ExclusionSettings = new[] { "sonar.coverage.exclusions", "sonar.cpd.exclusions", "sonar.exclusions", "sonar.issue.ignore.allfile", "sonar.issue.ignore.block" };
        private readonly CsvContext csvContext;
        private readonly CsvFileDescription outputFileDescription;

        public ReportServiceImpl(SonarQubeApiService sonarQubeApiService, ConfigService configService)
        {
            this.configService = configService ?? new ConfigServiceImpl();
            this.sonarQubeApiService = sonarQubeApiService ?? new SonarQubeApiServiceImpl(AppSetting.SonarQubeUrl, AppSetting.SonarQubeUserToken);
            outputFileDescription = new CsvFileDescription
            {
                FirstLineHasColumnNames = true,
                SeparatorChar = ','
            };
            csvContext = new CsvContext();
        }

        public async Task SendSonarQubeServerExclusionReport(List<ProjectModel> projects)
        {
            try
            {
                var reportedProjects = new Dictionary<ProjectModel, List<ProjectSettingKeyValue>>();
                var limitedProjects = projects.Take(25);

                foreach (var project in limitedProjects)
                {
                    var setting = await sonarQubeApiService.GetProjectSetting(project.Key);
                    var exclusionSettings = setting.Settings.Where(x => ExclusionSettings.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToList();
                    var analysisExclusionSettings = configService.GetSonarQubeAnalysisExclusionSetting(project.Key);
                    exclusionSettings = exclusionSettings.Union(analysisExclusionSettings).ToList();

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

        public void SendSonarQubeProjectExclusionReportByEmail(List<ProjectModel> projects)
        {
            try
            {
                var projectExclusions = configService.GetProjectExclusions().Select(x => new ProjectExclusionCsvModel
                {
                    Exclusions = x.Exclusions,
                    ProjectName = x.ProjectName,
                    SonarQubeProjectName = GetSonarQubeProjectName(projects, x.ProjectKey)
                }).OrderBy(x => x.SonarQubeProjectName).ToList();

                var globalSuppressions = configService.GetGlobalSuppressions().Select(x => new GlobalSuppressionCsvModel
                {
                    ProjectName = x.ProjectName,
                    RuleName = x.RuleName,
                    Scope = x.Scope,
                    SonarQubeProjectName = GetSonarQubeProjectName(projects, x.ProjectKey),
                    Target = x.Target
                }).OrderBy(x => x.SonarQubeProjectName).ToList();

                using (var projectExclusionStream = new MemoryStream())
                using (var globalSuppressionStream = new MemoryStream())
                using (var client = new MailDeliver())
                {
                    var mailAttachments = new List<MailAttachment>();
                    MailAttachment projectExclusionCsv = GenerateProjectExclusionAttachment(projectExclusions, projectExclusionStream);
                    mailAttachments.Add(projectExclusionCsv);

                    MailAttachment globalSuppressionCsv = GenerateGlobalSuppressionAttachment(globalSuppressions, globalSuppressionStream);
                    mailAttachments.Add(globalSuppressionCsv);

                    client.SendMail(new MailRecord
                    {
                        Subject = "SonarQube Project Exclusion Report",
                        Title = "SonarQube Report",
                        Body = "Hi all,<br/><br/>Please see attachments for project exclusion reports.<br/><br/>Thanks,",
                        MailTo = AppSetting.Recipients,
                        Attachments = mailAttachments
                    });
                }
            }
            catch (Exception ex)
            {
                LogService.Error(ex.Message, ex);
            }
        }

        private MailAttachment GenerateGlobalSuppressionAttachment(List<GlobalSuppressionCsvModel> globalSuppressions, MemoryStream globalSuppressionStream)
        {
            var globalSuppressionWriter = new StreamWriter(globalSuppressionStream, System.Text.Encoding.UTF8);
            csvContext.Write(globalSuppressions, globalSuppressionWriter, outputFileDescription);
            globalSuppressionWriter.Flush();
            globalSuppressionStream.Seek(0, SeekOrigin.Begin);

            var globalSuppressionCsv = new MailAttachment(globalSuppressionStream, FileExtentions.TEXT);
            globalSuppressionCsv.FileName = "SonarQube_Global_Suppression_Report.csv";

            return globalSuppressionCsv;
        }

        private MailAttachment GenerateProjectExclusionAttachment(List<ProjectExclusionCsvModel> projectExclusions, MemoryStream projectExclusionStream)
        {
            var projectExclusionWriter = new StreamWriter(projectExclusionStream, System.Text.Encoding.UTF8);
            csvContext.Write(projectExclusions, projectExclusionWriter, outputFileDescription);
            projectExclusionWriter.Flush();
            projectExclusionStream.Seek(0, SeekOrigin.Begin);

            var projectExclusionCsv = new MailAttachment(projectExclusionStream, FileExtentions.TEXT);
            projectExclusionCsv.FileName = "SonarQube_Project_Exclusion_Report.csv";

            return projectExclusionCsv;
        }

        private static string GetSonarQubeProjectName(List<ProjectModel> projects, string projectKey)
        {
            return projects.FirstOrDefault(p => p.Key.Equals(projectKey, StringComparison.OrdinalIgnoreCase))?.Name ?? projectKey;
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
