using System.Collections.Generic;
using System.Linq;

namespace SonarQube.Api.Models
{
    public class ProjectStatus
    {
        public ProjectStatus()
        {
            Conditions = new List<GateCondition>();
        }

        public string Status { get; set; }

        public List<GateCondition> Conditions { get; set; }

        public List<GateCondition> GetFailedConditions() => Conditions.Where(x => x.IsFailed).ToList();
    }
}
