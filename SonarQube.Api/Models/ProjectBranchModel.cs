using System;

namespace SonarQube.Api.Models
{
    public class ProjectBranchModel
    {
        public string ProjectKey { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }

        public string Author { get; set; }

        public DateTime ScanTime { get; set; }
    }
}
