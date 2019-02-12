namespace SonarQube.Api.Models
{
    public class SuppressionModel
    {
        public string ProjectKey { get; set; }

        public string ProjectName { get; set; }

        public string RuleName { get; set; }

        public string Scope { get; set; }

        public string Target { get; set; }
    }
}
