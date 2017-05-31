using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Fanex.SonarQube.ReportGenerator.Common;
using Fanex.SonarQube.ReportGenerator.Models;
using Newtonsoft.Json;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace Fanex.SonarQube.ReportGenerator
{
    class Program
    {
        private static HttpClient _httpClient = new HttpClient();
        private static int _exitCode = 0;
        private static ArgumentModel _argument;

        static int Main(string[] args)
        {
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                CreateNewAppDomainForRazorEngineCleanUp(args);
            }
            else
            {
                GenerateReport(args).Wait();
            }

            return _exitCode;
        }

        private async static Task GenerateReport(string[] args)
        {
            try
            {
                _argument = new ArgumentModel(args);

                if (_argument.IsValid)
                {
                    QualityGateStatusModel gateStatus = await GetQualityGateStatus();

                    var reportConfig = new TemplateServiceConfiguration
                    {
                        EncodedStringFactory = new HtmlEncodedStringFactory()
                    };
                    string content = GenerateTemplateContent(reportConfig, gateStatus, @"Templates\ProjectStatus.cshtml");
                    string reportPath = Path.Combine(_argument.Workspace, AppSetting.QualityStatusFileName);

                    var emailConfig = new TemplateServiceConfiguration
                    {
                        EncodedStringFactory = new RawStringFactory()
                    };
                    string emailContent = GenerateTemplateContent(emailConfig, gateStatus, @"Templates\ProjectStatusEmail.cshtml");
                    string emailFilePath = Path.Combine(_argument.Workspace, AppSetting.QualityStatusEmailFileName);

                    File.WriteAllText(reportPath, content);
                    Console.WriteLine($"The report file \"{reportPath}\" was generated.");

                    File.WriteAllText(emailFilePath, emailContent);
                    Console.WriteLine($"The email content \"{emailFilePath}\" was generated.");
                }
                else
                {
                    Console.WriteLine("ERROR: arguments are invalid.");
                    _exitCode = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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

        private static void ConfigureRazorEngine()
        {
            var config = new TemplateServiceConfiguration
            {
                EncodedStringFactory = new RawStringFactory()
            };

            Engine.Razor = RazorEngineService.Create(config);
        }

        private async static Task<QualityGateStatusModel> GetQualityGateStatus()
        {
            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{AppSetting.SonarQubeUserToken}:");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            string getGateStatusUrl = $"{AppSetting.SonarQubeUrl}/api/qualitygates/project_status?projectKey={_argument.ProjectKey}";

            HttpResponseMessage response = await _httpClient.GetAsync(getGateStatusUrl).ConfigureAwait(false);
            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            QualityGateStatusModel projectStatus = null;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                projectStatus = JsonConvert.DeserializeObject<QualityGateStatusModel>(result);
            }
            else
            {
                Console.WriteLine("ERROR:");
                Console.WriteLine(result);
                _exitCode = 1;
            }

            return projectStatus;
        }

        private static void CreateNewAppDomainForRazorEngineCleanUp(string[] args)
        {
            // RazorEngine cannot clean up from the default appdomain...
            // You only need to add strongnames when your appdomain is not a full trust environment.

            var strongNames = new StrongName[0];
            var domain = AppDomain.CreateDomain(
                "SonarQube.ReportGenerator.exe",
                null,
                AppDomain.CurrentDomain.SetupInformation,
                new PermissionSet(PermissionState.Unrestricted),
                strongNames);

            _exitCode = domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, args);

            // RazorEngine will cleanup. 
            AppDomain.Unload(domain);
        }
    }
}
