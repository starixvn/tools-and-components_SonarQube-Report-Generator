using Fanex.Bot.Client;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using SharpRaven;
using SonarQube.Api.Models;
using SonarQube.ScheduledReport.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonarQube.ScheduledReport
{
    class Program
    {
        private static ReportService reportService;

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

            BotClientManager.UseConfig(
                new Fanex.Bot.Client.Configuration.BotSettings(
                        AppSetting.BotServiceUrl,
                        AppSetting.BotClientId,
                        AppSetting.BotClientPassword,
                        AppSetting.BotClientTimeout));

            reportService = new ReportServiceImpl(null, null);

            List<ProjectModel> projects = Task.Factory.StartNew(reportService.GetFilteredProjects)
                                                        .Unwrap()
                                                        .GetAwaiter()
                                                        .GetResult();

            Task.Factory.StartNew(() => reportService.SendSonarQubeViolationReport(projects))
                        .Unwrap()
                        .GetAwaiter()
                        .GetResult();

            Task.Factory.StartNew(() => reportService.SendSonarQubeServerExclusionReport(projects))
                        .Unwrap()
                        .GetAwaiter()
                        .GetResult();

            reportService.SendSonarQubeProjectExclusionReportByEmail(projects);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                LogService.Error(exception.Message, exception);
            }
        }
    }
}
