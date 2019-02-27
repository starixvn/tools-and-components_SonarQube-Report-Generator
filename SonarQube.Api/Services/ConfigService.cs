using SonarQube.Api.DataAccess;
using SonarQube.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarQube.Api.Services
{
    public interface ConfigService
    {
        void SaveGlobalSuppression(string projectKey, List<SuppressionModel> suppressions);

        void SaveProjectExclusion(string projectKey, List<ProjectExclusionModel> exclusions);

        void SaveSonarQubeAnalysisExclusion(string projectKey, List<ProjectSettingKeyValue> settings);

        List<ProjectSettingKeyValue> GetSonarQubeAnalysisExclusionSetting(string projectKey);

        List<ProjectExclusionModel> GetProjectExclusions();

        List<SuppressionModel> GetGlobalSuppressions();

        void SaveProjectBranch(ProjectBranchModel branch);
    }

    public class ConfigServiceImpl : ConfigService, IDisposable
    {
        private SonarQubeReportEntities entityManager;

        public ConfigServiceImpl()
        {
            entityManager = new SonarQubeReportEntities();
        }

        public void SaveGlobalSuppression(string projectKey, List<SuppressionModel> suppressions)
        {
            var oldConfigs = entityManager.GlobalSuppressions.Where(x => x.ProjectKey.Equals(projectKey, StringComparison.OrdinalIgnoreCase));

            entityManager.GlobalSuppressions.RemoveRange(oldConfigs);

            entityManager.GlobalSuppressions.AddRange(suppressions.Select(x => new GlobalSuppression
            {
                ProjectKey = projectKey,
                ProjectName = x.ProjectName,
                RuleName = x.RuleName,
                Scope = x.Scope,
                Target = x.Target
            }));

            entityManager.SaveChanges();
        }

        public void SaveProjectExclusion(string projectKey, List<ProjectExclusionModel> exclusions)
        {
            var oldConfigs = entityManager.ProjectExclusions.Where(x => x.ProjectKey.Equals(projectKey, StringComparison.OrdinalIgnoreCase));

            entityManager.ProjectExclusions.RemoveRange(oldConfigs);

            entityManager.ProjectExclusions.AddRange(exclusions.Select(x => new ProjectExclusion
            {
                ProjectName = x.ProjectName,
                Exclusions = x.Exclusions,
                ProjectKey = projectKey
            }));

            entityManager.SaveChanges();
        }

        public void SaveSonarQubeAnalysisExclusion(string projectKey, List<ProjectSettingKeyValue> settings)
        {
            var oldConfigs = entityManager.SonarQubeAnalysisExclusions.Where(x => x.ProjectKey.Equals(projectKey, StringComparison.OrdinalIgnoreCase));

            entityManager.SonarQubeAnalysisExclusions.RemoveRange(oldConfigs);

            entityManager.SonarQubeAnalysisExclusions.AddRange(settings.Select(x => new SonarQubeAnalysisExclusion
            {
                ProjectKey = projectKey,
                Key = x.Key,
                Values = x.Value
            }));

            entityManager.SaveChanges();
        }

        public List<ProjectSettingKeyValue> GetSonarQubeAnalysisExclusionSetting(string projectKey)
        {
            var settings = entityManager.SonarQubeAnalysisExclusions.Where(x => x.ProjectKey.Equals(projectKey, StringComparison.OrdinalIgnoreCase)).ToList();

            return settings.Select(x => new ProjectSettingKeyValue
            {
                Key = x.Key,
                Values = x.Values.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(y => y.Trim()).ToList()
            }).ToList();
        }

        public List<ProjectExclusionModel> GetProjectExclusions()
            => entityManager.ProjectExclusions.Select(x => new ProjectExclusionModel
            {
                Exclusions = x.Exclusions,
                ProjectKey = x.ProjectKey,
                ProjectName = x.ProjectName
            }).ToList();

        public List<SuppressionModel> GetGlobalSuppressions()
            => entityManager.GlobalSuppressions.Select(x => new SuppressionModel
            {
                ProjectKey = x.ProjectKey,
                ProjectName = x.ProjectName,
                RuleName = x.RuleName,
                Scope = x.Scope,
                Target = x.Target
            }).ToList();

        public void SaveProjectBranch(ProjectBranchModel branch)
        {
            entityManager.SaveProjectBranch(branch.ProjectKey, branch.Branch, branch.Commit, branch.Author);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    entityManager?.Dispose();
                }

                entityManager = null;
                disposedValue = true;
            }
        }

        ~ConfigServiceImpl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
