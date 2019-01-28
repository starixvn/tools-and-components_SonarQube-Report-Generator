using System.Collections.Generic;

namespace SonarQube.Api.Models
{
    public class ProjectStatus
    {
        public string Status { get; set; }

        public List<GateCondition> Conditions { get; set; }
    }
}
