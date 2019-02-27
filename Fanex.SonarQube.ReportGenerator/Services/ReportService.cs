using Fanex.SonarQube.ReportGenerator.Common;
using Fanex.SonarQube.ReportGenerator.Models;
using Fanex.SonarQube.ReportGenerator.Properties;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using SonarQube.Api.Models;
using SonarQube.Api.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fanex.SonarQube.ReportGenerator.Services
{
    public interface ReportService
    {
        Task GenerateReport(ArgumentModel argument);

        void CollectSonarQubeProjectExclusionConfig(ArgumentModel argument);

        void CollectSonarQubeAnalysisExclusionConfig(ArgumentModel argument);

        void SaveProjectBranch(ArgumentModel argument);
    }

    public class ReportServiceImpl : ReportService
    {
        private readonly SonarQubeApiService sonarQubeApiService;
        private readonly ConfigService configService;
        private const string SonarQubeExclude = "SonarQubeExclude";
        private const string ProjectNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private const string SonarQubeConfigNamespace = "http://www.sonarsource.com/msbuild/integration/2015/1";
        private static readonly string[] ExclusionSettings = new[] { "sonar.coverage.exclusions", "sonar.cpd.exclusions", "sonar.exclusions", "sonar.issue.ignore.allfile", "sonar.issue.ignore.block" };
        private const string NickNamePattern = @"(\b[A-Z]+\b)";

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

        public void CollectSonarQubeProjectExclusionConfig(ArgumentModel argument)
        {
            var suppressionConfigs = new List<SuppressionModel>();
            var exclusionConfigs = new List<ProjectExclusionModel>();
            var projectFiles = Directory.EnumerateFiles(argument.Workspace, "*.csproj", SearchOption.AllDirectories)
                                        .Where(x => !x.EndsWith("Tests.csproj", StringComparison.OrdinalIgnoreCase))
                                        .ToList();

            foreach (var projectFile in projectFiles)
            {
                CollectGlobalSuppression(suppressionConfigs, projectFile);

                CollectExclusionConfig(exclusionConfigs, projectFile);
            }

            configService.SaveGlobalSuppression(argument.ProjectKey, suppressionConfigs);

            configService.SaveProjectExclusion(argument.ProjectKey, exclusionConfigs);
        }

        public void CollectSonarQubeAnalysisExclusionConfig(ArgumentModel argument)
        {
            string sonarQubeConfigName = "SonarQube.Analysis.xml";
            string sonarQubeConfigFile = Directory.EnumerateFiles(argument.Workspace, sonarQubeConfigName, SearchOption.AllDirectories).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(sonarQubeConfigFile))
            {
                var settings = new List<ProjectSettingKeyValue>();
                var doc = XDocument.Load(sonarQubeConfigFile);
                var properties = doc.Descendants(XName.Get("Property", SonarQubeConfigNamespace)).ToList();

                foreach (XElement property in properties)
                {
                    var key = property.Attribute("Name")?.Value;
                    var value = property.Value.Trim();

                    if (!string.IsNullOrWhiteSpace(key)
                        && ExclusionSettings.Contains(key, StringComparer.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(value))
                    {
                        settings.Add(new ProjectSettingKeyValue
                        {
                            Key = key,
                            Value = value
                        });
                    }
                }

                configService.SaveSonarQubeAnalysisExclusion(argument.ProjectKey, settings);
            }
        }

        public void SaveProjectBranch(ArgumentModel argument)
        {
            var branch = new ProjectBranchModel
            {
                Author = GetLatestCommitAuthor(argument.Workspace),
                ProjectKey = argument.ProjectKey,
                Branch = Environment.GetEnvironmentVariable("GIT_BRANCH")?.Replace("origin/", string.Empty),
                Commit = Environment.GetEnvironmentVariable("GIT_COMMIT")
            };

            configService.SaveProjectBranch(branch);
        }

        private string GetLatestCommitAuthor(string workspace)
        {
            string nickName = string.Empty;
            string command = "git";
            string arguments = "show -s --pretty=%an";

            var result = RunCommandLine(command, arguments, workspace, timeout: 60000);

            if (!result.HasError)
            {
                var match = Regex.Match(result.Output, NickNamePattern, RegexOptions.IgnoreCase);
                nickName = match.Success ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(match.Groups[1]?.Value) : string.Empty;
            }

            return nickName;
        }

        private static void CollectGlobalSuppression(List<SuppressionModel> suppressionConfigs, string projectFile)
        {
            const string sonarQubeRulePattern = @"^S\d+";
            const string defaultScope = "module";
            string projectFolder = Path.GetDirectoryName(projectFile);
            string projectName = Path.GetFileName(projectFile);
            var globalSuppressionFile = Directory.EnumerateFiles(projectFolder, "GlobalSuppressions*.cs", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(globalSuppressionFile))
            {
                string content = File.ReadAllText(globalSuppressionFile);
                var matches = Regex.Matches(content, Resources.SuppressionPattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    string ruleName = match.Groups[2]?.Value;

                    if (!string.IsNullOrWhiteSpace(ruleName) && Regex.IsMatch(ruleName, sonarQubeRulePattern, RegexOptions.IgnoreCase))
                    {
                        string scope = match.Groups[5]?.Value;

                        suppressionConfigs.Add(new SuppressionModel
                        {
                            ProjectName = projectName,
                            RuleName = match.Groups[2]?.Value,
                            Scope = !string.IsNullOrWhiteSpace(scope) ? scope : defaultScope,
                            Target = SeparateContentBySpace(match.Groups[6]?.Value)
                        });
                    }
                }
            }
        }

        private static void CollectExclusionConfig(List<ProjectExclusionModel> exclusionConfigs, string projectFile)
        {
            string projectName = Path.GetFileName(projectFile);
            var document = XDocument.Load(projectFile);
            var projectExclude = document.Descendants(XName.Get("PropertyGroup", ProjectNamespace))
                                            .Elements(XName.Get(SonarQubeExclude, ProjectNamespace))
                                            .FirstOrDefault();
            bool isExcludedProject = bool.TrueString.Equals(projectExclude?.Value, StringComparison.OrdinalIgnoreCase);

            if (isExcludedProject)
            {
                exclusionConfigs.Add(new ProjectExclusionModel
                {
                    ProjectName = projectName,
                    Exclusions = projectName
                });
            }
            else
            {
                var compileElements = document.Descendants(XName.Get("Compile", ProjectNamespace));
                var excludedFiles = new List<string>();

                foreach (var element in compileElements)
                {
                    string fileName = element.Attribute("Include")?.Value;
                    bool isExcludedFile = bool.TrueString.Equals(element.Element(XName.Get(SonarQubeExclude, ProjectNamespace))?.Value, StringComparison.OrdinalIgnoreCase);

                    if (isExcludedFile && !string.IsNullOrWhiteSpace(fileName))
                    {
                        excludedFiles.Add(fileName);
                    }
                }

                if (excludedFiles.Count > 0)
                {
                    exclusionConfigs.Add(new ProjectExclusionModel
                    {
                        ProjectName = projectName,
                        Exclusions = string.Join(", ", excludedFiles)
                    });
                }
            }
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

        private static CommandResult RunCommandLine(string processFilePath, string arguments, string workingDirectory, int timeout = 300000, bool checkExitCode = true)
        {
            var commandResult = new CommandResult();
            string outputMessage = string.Empty;

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = processFilePath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.WorkingDirectory = workingDirectory;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    var output = new StringBuilder();
                    var error = new StringBuilder();
                    int exitCode;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed.
                        output.AppendLine(error.ToString());
                        exitCode = process.HasExited ? process.ExitCode : 0;
                    }
                    else
                    {
                        // Timed out.
                        output.AppendLine(error.ToString());
                        output.AppendLine("Error timeout is reached!");
                        exitCode = 1;
                    }

                    if (output.Length > 0)
                    {
                        outputMessage = output.ToString().Trim();
                        commandResult.Output = outputMessage;
                    }

                    if (checkExitCode && exitCode != 0)
                    {
                        commandResult.ErrorMessage = outputMessage;
                    }
                }
            }

            return commandResult;
        }
    }
}
