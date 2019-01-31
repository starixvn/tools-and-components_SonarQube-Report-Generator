using Fanex.SonarQube.ReportGenerator.DataAccess;
using Fanex.SonarQube.ReportGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fanex.SonarQube.ReportGenerator.Services
{
    public interface ConfigService
    {
        void SaveGlobalSuppression(string projectKey, List<SuppressionModel> suppressions);
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
