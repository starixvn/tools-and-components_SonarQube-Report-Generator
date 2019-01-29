using SonarQube.Api.Models;
using System.Collections.Generic;

namespace SonarQube.ScheduledReport.Models
{
    public class ViolationReportModel
    {
        public Dictionary<ProjectModel, QualityGateStatusModel> ViolatedProjects { get; set; }
    }
}
