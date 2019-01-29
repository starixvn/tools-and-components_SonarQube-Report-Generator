using System;
using System.Runtime.Serialization;

namespace SonarQube.ScheduledReport.Models
{
    [Serializable]
    public class SkynexBotException : Exception
    {
        public SkynexBotException(string message) : base(message) { }

        public SkynexBotException(string message, Exception innerException) : base(message, innerException) { }

        protected SkynexBotException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
