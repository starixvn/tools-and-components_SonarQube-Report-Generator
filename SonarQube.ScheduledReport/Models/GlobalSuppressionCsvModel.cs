using LINQtoCSV;

namespace SonarQube.ScheduledReport.Models
{
    public class GlobalSuppressionCsvModel
    {
        [CsvColumn(Name = "ProjectName", FieldIndex = 1)]
        public string SonarQubeProjectName { get; set; }

        [CsvColumn(Name = "Project", FieldIndex = 2)]
        public string ProjectName { get; set; }

        [CsvColumn(Name = "Rule", FieldIndex = 3)]
        public string RuleName { get; set; }

        [CsvColumn(Name = "Scope", FieldIndex = 4)]
        public string Scope { get; set; }

        [CsvColumn(Name = "Target", FieldIndex = 5, CanBeNull = true)]
        public string Target { get; set; }
    }
}
