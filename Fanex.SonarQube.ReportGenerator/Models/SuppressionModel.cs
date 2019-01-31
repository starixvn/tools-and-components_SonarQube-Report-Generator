namespace Fanex.SonarQube.ReportGenerator.Models
{
    public class SuppressionModel
    {
        public string ProjectName { get; set; }

        public string RuleName { get; set; }

        public string Scope { get; set; }

        public string Target { get; set; }
    }
}
