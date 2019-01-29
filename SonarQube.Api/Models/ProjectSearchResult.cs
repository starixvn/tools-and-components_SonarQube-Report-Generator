using System;
using System.Collections.Generic;

namespace SonarQube.Api.Models
{
    public class ProjectSearchResult
    {
        public ProjectSearchPaging Paging { get; set; }

        public List<ProjectModel> Components { get; set; }
    }

    public sealed class ProjectModel : IEquatable<ProjectModel>
    {
        public string Id { get; set; }

        public string Key { get; set; }

        public string Name { get; set; }

        public bool Equals(ProjectModel other) => other != null && Key.Equals(other.Key, StringComparison.OrdinalIgnoreCase);
    }

    public class ProjectSearchPaging
    {
        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int Total { get; set; }
    }
}
