using LINQtoCSV;

namespace SonarQube.ScheduledReport.Models
{
    public class ProjectExclusionCsvModel
    {
        [CsvColumn(Name = "ProjectName", FieldIndex = 1)]
        public string SonarQubeProjectName { get; set; }

        [CsvColumn(Name = "Project", FieldIndex = 2)]
        public string ProjectName { get; set; }

        [CsvColumn(Name = "Exclusions", FieldIndex = 3)]
        public string Exclusions { get; set; }
    }
}
