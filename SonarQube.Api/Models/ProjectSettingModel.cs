using System.Collections.Generic;

namespace SonarQube.Api.Models
{
    public class ProjectSettingModel
    {
        public List<ProjectSettingKeyValue> Settings { get; set; }
    }

    public class ProjectSettingKeyValue
    {
        public string Key { get; set; }

        public string Name => MetricResources.ResourceManager.GetString(Key) ?? Key;

        public string Value { get; set; }

        public List<string> Values { get; set; }
    }
}
