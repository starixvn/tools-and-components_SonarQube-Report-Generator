using SonarQube.Api.Models;
using System.Collections.Generic;

namespace SonarQube.ScheduledReport.Models
{
    public class ExclusionReportModel
    {
        public Dictionary<ProjectModel, List<ProjectSettingKeyValue>> Projects { get; set; }
    }
}
