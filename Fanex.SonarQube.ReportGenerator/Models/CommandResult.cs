namespace Fanex.SonarQube.ReportGenerator.Models
{
    public class CommandResult
    {
        public string ErrorMessage { get; set; }

        public string Output { get; set; }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    }
}
