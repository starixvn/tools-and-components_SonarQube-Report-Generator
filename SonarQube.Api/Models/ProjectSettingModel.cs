using System;
using System.Collections.Generic;

namespace SonarQube.Api.Models
{
    public class ProjectSettingModel
    {
        public List<ProjectSettingKeyValue> Settings { get; set; }
    }

    public sealed class ProjectSettingKeyValue : IEquatable<ProjectSettingKeyValue>
    {
        public string Key { get; set; }

        public string Name => MetricResources.ResourceManager.GetString(Key) ?? Key;

        public string Value { get; set; }

        public List<string> Values { get; set; }

        public bool Equals(ProjectSettingKeyValue other)
            => other != null && Key.Equals(other.Key, StringComparison.OrdinalIgnoreCase);
    }
}
