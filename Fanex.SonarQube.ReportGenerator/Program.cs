using Fanex.SonarQube.ReportGenerator.Common;
using Fanex.SonarQube.ReportGenerator.Models;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
using SonarQube.Api;
using SonarQube.Api.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Fanex.SonarQube.ReportGenerator
{
    class Program
    {
        private static SonarQubeApiService sonarQubeApiService;
        private static int exitCode = 0;

        static int Main(string[] args)
        {
            sonarQubeApiService = new SonarQubeApiServiceImpl(AppSetting.SonarQubeUrl, AppSetting.SonarQubeUserToken);

            Task.Factory.StartNew(() => GenerateReport(args))
                        .Unwrap()
                        .GetAwaiter()
                        .GetResult();

            return exitCode;
        }

        private async static Task GenerateReport(string[] args)
        {
            try
            {
                var argument = new ArgumentModel(args);

                if (argument.IsValid)
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
                else
                {
                    Console.WriteLine("ERROR: arguments are invalid.");
                    exitCode = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: -> {ex}");

                exitCode = 1;
            }
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
