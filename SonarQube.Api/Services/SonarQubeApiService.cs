using Newtonsoft.Json;
using SonarQube.Api.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SonarQube.Api.Services
{
    public interface SonarQubeApiService
    {
        Task<QualityGateStatusModel> GetQualityGateStatusOfProject(string projectKey);

        Task<List<ProjectModel>> GetAllProjects();

        Task<ProjectSettingModel> GetProjectSetting(string projectKey);
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

        public async Task<List<ProjectModel>> GetAllProjects()
        {
            var result = new List<ProjectModel>();
            int pageIndex = 1;
            var pagingProjects = await GetPagingProjects(pageIndex);
            int pageCount = (pagingProjects.Paging.Total + pagingProjects.Paging.PageSize - 1) / pagingProjects.Paging.PageSize;

            result.AddRange(pagingProjects.Components);

            for (int i = 2; i <= pageCount; i++)
            {
                pageIndex = i;

                pagingProjects = await GetPagingProjects(pageIndex);

                result.AddRange(pagingProjects.Components);
            }

            return result;
        }

        public async Task<QualityGateStatusModel> GetQualityGateStatusOfProject(string projectKey)
        {
            var getGateStatusUrl = new Uri($"{sonarQubeUrl}/api/qualitygates/project_status?projectKey={projectKey}");
            QualityGateStatusModel projectStatus = await GetAsync<QualityGateStatusModel>(getGateStatusUrl);

            return projectStatus;
        }

        public async Task<ProjectSettingModel> GetProjectSetting(string projectKey)
        {
            var getProjectSettingUrl = new Uri($"{sonarQubeUrl}/api/settings/values?component={projectKey}");
            var setting = await GetAsync<ProjectSettingModel>(getProjectSettingUrl);

            return setting;
        }

        private async Task<ProjectSearchResult> GetPagingProjects(int pageIndex)
        {
            var getPagingProjectUrl = new Uri($"{sonarQubeUrl}/api/projects/search?p={pageIndex}");
            var projects = await GetAsync<ProjectSearchResult>(getPagingProjectUrl);

            return projects;
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
