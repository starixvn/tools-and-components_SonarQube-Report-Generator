using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fanex.SonarQube.ReportGenerator.Models
{
    public class ArgumentModel
    {
        private const string ProjectKeyArgument = "--projectkey";
        private const string WorkspaceArgument = "--workspace";

        public ArgumentModel(string[] args)
        {
            GetArguments(args);

            PrintArguments();
        }

        public string ProjectKey { get; set; }

        public string Workspace { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ProjectKey) && !string.IsNullOrWhiteSpace(Workspace);

        private void PrintArguments()
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Arguments:");
            Console.WriteLine($"Project Key = {ProjectKey}");
            Console.WriteLine($"Workspace = {Workspace}");
            Console.WriteLine(string.Empty);
        }

        private void GetArguments(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                foreach (string argument in args)
                {
                    string[] parts = argument.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        string argumentId = parts[0].ToLowerInvariant();

                        GetArgumentInfo(parts, argumentId);
                    }
                }
            }
        }

        private void GetArgumentInfo(string[] parts, string argumentId)
        {
            switch (argumentId)
            {
                case ProjectKeyArgument:
                    ProjectKey = parts[1];
                    break;

                case WorkspaceArgument:
                    Workspace = parts[1];
                    break;
            }
        }
    }
}
