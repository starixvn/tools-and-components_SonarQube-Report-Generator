using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fanex.SonarQube.ReportGenerator.Models
{
    public class ProjectStatus
    {
        public string Status { get; set; }

        public List<GateCondition> Conditions { get; set; }
    }
}
