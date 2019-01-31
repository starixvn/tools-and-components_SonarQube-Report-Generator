using Fanex.SonarQube.ReportGenerator.Common;
using Fanex.SonarQube.ReportGenerator.Models;
using Fanex.SonarQube.ReportGenerator.Properties;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using SonarQube.Api;
using SonarQube.Api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fanex.SonarQube.ReportGenerator.Services
{
    public interface ReportService
    {
        Task GenerateReport(ArgumentModel argument);

        void CollectSonarQubeExclusionConfig(ArgumentModel argument);
    }

    public class ReportServiceImpl : ReportService
    {
        private readonly SonarQubeApiService sonarQubeApiService;
        private readonly ConfigService configService;

        public ReportServiceImpl(SonarQubeApiService sonarQubeApiService, ConfigService configService)
        {
            this.sonarQubeApiService = sonarQubeApiService ?? new SonarQubeApiServiceImpl(AppSetting.SonarQubeUrl, AppSetting.SonarQubeUserToken);
            this.configService = configService ?? new ConfigServiceImpl();
        }

        public async Task GenerateReport(ArgumentModel argument)
        {
            QualityGateStatusModel gateStatus = await sonarQubeApiService.GetQualityGateStatusOfProject(argument.ProjectKey);

            var reportConfig = new TemplateServiceConfiguration
            {
                EncodedStringFactory = new HtmlEncodedStringFactory(),
                DisableTempFileLocking = true,
                CachingProvider = new DefaultCachingProvider(t => { })
            };
            string content = GenerateTemplateContent(reportConfig, gateStatus, @"Templates\ProjectStatus.cshtml");
            string reportPath = Path.Combine(argument.Workspace, AppSetting.QualityStatusFileName);

            var emailConfig = new TemplateServiceConfiguration
            {
                EncodedStringFactory = new RawStringFactory(),
                DisableTempFileLocking = true,
                CachingProvider = new DefaultCachingProvider(t => { })
            };
            string emailContent = GenerateTemplateContent(emailConfig, gateStatus, @"Templates\ProjectStatusEmail.cshtml");
            string emailFilePath = Path.Combine(argument.Workspace, AppSetting.QualityStatusEmailFileName);

            File.WriteAllText(reportPath, content);
            Console.WriteLine($"The report file \"{reportPath}\" was generated.");

            File.WriteAllText(emailFilePath, emailContent);
            Console.WriteLine($"The email content \"{emailFilePath}\" was generated.");
        }

        public void CollectSonarQubeExclusionConfig(ArgumentModel argument)
        {
            var suppressionConfigs = new List<SuppressionModel>();
            const string defaultScope = "module";
            var projectFiles = Directory.EnumerateFiles(argument.Workspace, "*.csproj", SearchOption.AllDirectories)
                                        .Where(x => !x.EndsWith("Tests", StringComparison.OrdinalIgnoreCase))
                                        .ToList();

            foreach (var projectFile in projectFiles)
            {
                string projectFolder = Path.GetDirectoryName(projectFile);
                var globalSuppressionFile = Directory.EnumerateFiles(projectFolder, "GlobalSuppressions*.cs", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(globalSuppressionFile))
                {
                    string content = File.ReadAllText(globalSuppressionFile);
                    var matches = Regex.Matches(content, Resources.SuppressionPattern, RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        var config = new SuppressionModel
                        {
                            ProjectName = Path.GetFileName(projectFile),
                            RuleName = match.Groups[2]?.Value,
                            Scope = match.Groups[5]?.Value ?? defaultScope,
                            Target = SeparateContentBySpace(match.Groups[6]?.Value)
                        };

                        suppressionConfigs.Add(config);
                    }
                }
            }

            configService.SaveGlobalSuppression(argument.ProjectKey, suppressionConfigs);
        }

        private static string SeparateContentBySpace(string content)
        {
            string result = content;

            if (!string.IsNullOrWhiteSpace(content))
            {
                string pattern = @"([\(\)\,\{\}])";

                result = Regex.Replace(content, pattern, "$1 ");
            }

            return result;
        }

        private static string GenerateTemplateContent(TemplateServiceConfiguration config, object model, string relativeTemplatePath)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string templatePath = Path.Combine(appPath, relativeTemplatePath);
            string resultContent = string.Empty;

            if (File.Exists(templatePath))
            {
                Engine.Razor = RazorEngineService.Create(config);

                string templateContent = File.ReadAllText(templatePath);

                resultContent = Engine.Razor.RunCompile(templateContent, relativeTemplatePath, model.GetType(), model);
            }

            return resultContent;
        }
    }
}
