namespace SonarQube.Api.Models
{
    public class ProjectExclusionModel
    {
        public string ProjectKey { get; set; }

        public string ProjectName { get; set; }

        public string Exclusions { get; set; }
    }
}
