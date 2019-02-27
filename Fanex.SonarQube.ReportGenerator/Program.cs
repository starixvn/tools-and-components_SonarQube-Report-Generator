using Fanex.SonarQube.ReportGenerator.Models;
using Fanex.SonarQube.ReportGenerator.Services;
using System;
using System.Threading.Tasks;

namespace Fanex.SonarQube.ReportGenerator
{
    class Program
    {
        private static ReportService reportService;
        private static int exitCode = 0;

        static int Main(string[] args)
        {
            reportService = new ReportServiceImpl(null, null);

            Task.Factory.StartNew(() => GenerateReportAndCollectionExclusions(args))
                        .Unwrap()
                        .GetAwaiter()
                        .GetResult();

            return exitCode;
        }

        private async static Task GenerateReportAndCollectionExclusions(string[] args)
        {
            try
            {
                var argument = new ArgumentModel(args);

                if (argument.IsValid)
                {
                    await reportService.GenerateReport(argument);

                    reportService.SaveProjectBranch(argument);

                    reportService.CollectSonarQubeProjectExclusionConfig(argument);

                    reportService.CollectSonarQubeAnalysisExclusionConfig(argument);
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
    }
}
