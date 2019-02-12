using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace SonarQube.ScheduledReport
{
    public static class AppSetting
    {
        public static string SonarQubeUserToken => ConfigurationManager.AppSettings.Get("SonarQubeUserToken");

        public static string SonarQubeUrl => ConfigurationManager.AppSettings.Get("SonarQubeUrl");

        public static Uri BotServiceUrl => new Uri(ConfigurationManager.AppSettings.Get("FanexBotClient:BotServiceUrl"));

        public static string BotClientId => ConfigurationManager.AppSettings.Get("FanexBotClient:ClientId");

        public static string BotClientPassword => ConfigurationManager.AppSettings.Get("FanexBotClient:ClientPassword");

        public static int BotClientTimeout => Convert.ToInt32(ConfigurationManager.AppSettings.Get("FanexBotClient:Timeout"));

        public static List<string> GetConversationIds()
            => ConfigurationManager.AppSettings.Get("ConversationIds").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

        public static string SentryDsn => ConfigurationManager.AppSettings.Get("SentryDsn");

        public static string SentryEnvironment => ConfigurationManager.AppSettings.Get("SentryEnvironment");

        public static List<string> GetIgnoredProjectNamePatterns()
            => ConfigurationManager.AppSettings.Get("IgnoredProjectNamePatterns").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

        public static string Recipients => ConfigurationManager.AppSettings.Get("Recipients");
    }
}
