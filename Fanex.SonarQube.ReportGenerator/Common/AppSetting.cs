using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fanex.SonarQube.ReportGenerator.Common
{
    public static class AppSetting
    {
        public static string SonarQubeUserToken => ConfigurationManager.AppSettings.Get("SonarQubeUserToken");

        public static string SonarQubeUrl => ConfigurationManager.AppSettings.Get("SonarQubeUrl");

        public static string QualityStatusFileName => ConfigurationManager.AppSettings.Get("QualityStatusFileName");
    }
}
