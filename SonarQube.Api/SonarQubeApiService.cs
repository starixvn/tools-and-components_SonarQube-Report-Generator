using Newtonsoft.Json;
using SonarQube.Api.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SonarQube.Api
{
    public interface SonarQubeApiService
    {
        Task<QualityGateStatusModel> GetQualityGateStatusOfProject(string projectKey);
    }

    public class SonarQubeApiServiceImpl : SonarQubeApiService
    {
        private HttpClient httpClient;
        private readonly string sonarQubeUrl;

        public SonarQubeApiServiceImpl(string sonarQubeUrl, string userToken)
        {
            this.sonarQubeUrl = sonarQubeUrl;

            InitializeHttpClient(userToken);
        }

        public async Task<QualityGateStatusModel> GetQualityGateStatusOfProject(string projectKey)
        {
            var getGateStatusUrl = new Uri($"{sonarQubeUrl}/api/qualitygates/project_status?projectKey={projectKey}");
            QualityGateStatusModel projectStatus = await GetAsync<QualityGateStatusModel>(getGateStatusUrl);

            return projectStatus;
        }

        private void InitializeHttpClient(string userToken)
        {
            var byteArray = System.Text.Encoding.ASCII.GetBytes($"{userToken}:");
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        private async Task<T> PostAsync<T>(Uri apiUri, object requestData)
        {
            string paramData = JsonConvert.SerializeObject(requestData);
            var requestContent = new StringContent(paramData, Encoding.UTF8, "application/json");

            using (HttpResponseMessage response = await httpClient.PostAsync(apiUri, requestContent))
            {
                string content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
                    var exception = JsonConvert.DeserializeObject<HttpError>(content);

                    throw new HttpRequestException(exception.ExceptionMessage);
                }
            }
        }

        private async Task<T> GetAsync<T>(Uri apiUri)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(apiUri))
            {
                string content = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
                    var exception = JsonConvert.DeserializeObject<HttpError>(content);

                    throw new HttpRequestException(exception.ExceptionMessage);
                }
            }
        }
    }

    public class HttpError
    {
        public string ExceptionMessage { get; set; }
    }
}
