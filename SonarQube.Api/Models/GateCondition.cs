using System;

namespace SonarQube.Api.Models
{
    public class GateCondition
    {
        public string Status { get; set; }

        public string MetricKey { get; set; }

        public string MetricName => MetricResources.ResourceManager.GetString(MetricKey);

        public string Comparator { get; set; }

        public string ErrorThreshold { get; set; }

        public string ActualValue { get; set; }

        //ERROR: Reliability Rating: 3 > 1
        //ERROR: Security Rating: 5 > 1
        //ERROR: Code Smells: 248 > 0
        public string Result
        {
            get
            {
                string result = ActualValue;

                if (IsFailed)
                {
                    result = $"{ActualValue} {ConvertedComparator} {ErrorThreshold}";
                }

                return result;
            }
        }

        public bool IsFailed => Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase);

        private string ConvertedComparator
        {
            get
            {
                string convertedComparator;

                if (Comparator.Equals("GT", StringComparison.OrdinalIgnoreCase))
                {
                    convertedComparator = ">";
                }
                else if (Comparator.Equals("LT", StringComparison.OrdinalIgnoreCase))
                {
                    convertedComparator = "<";
                }
                else
                {
                    convertedComparator = string.Empty;
                }

                return convertedComparator;
            }
        }
    }
}
