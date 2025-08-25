using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StayAccess.Api.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Api.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;

        public HttpClientService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
        }

        #region public methods

        public async Task<HttpResponseMessage> PostAsync<T>(T model, string path, bool requireToken = true, string baseUrl = "") where T : class
        {
            try
            {
                HttpContent stringContent = SetHttpContent(model);
                var httpClient = GetHttpClient(requireToken, baseUrl);
                return await httpClient.PostAsync(path, stringContent);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public T Deserialize<T>(string serializedString)
        {
            return JsonConvert.DeserializeObject<T>(serializedString);
        }

        public string Serialize<T>(T model)
        {
            return JsonConvert.SerializeObject(model);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Gets http client to make http requests.
        /// </summary>
        /// <returns><seealso cref="HttpClient"/></returns>
        private HttpClient GetHttpClient(bool requireToken = true, string baseUrl = "")
        {
            try
            {
                if (string.IsNullOrEmpty(baseUrl))
                    baseUrl = configuration["AuthApiPath"];

                // make http request to get all roles
                HttpClient httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(baseUrl)
                };

                if (requireToken)
                {
                    string token = GetJwtToken();

                    if (string.IsNullOrEmpty(token))
                        throw new UnauthorizedAccessException("access token not found");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GetJwtToken());
                }

                return httpClient;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets <seealso cref="HttpContent"/> for http request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns><seealso cref="HttpContent"/></returns>
        private HttpContent SetHttpContent<T>(T model)
        {
            string serializedString = Serialize(model);
            HttpContent stringContent = new StringContent(serializedString, Encoding.UTF8, MediaTypeNames.Application.Json);
            return stringContent;
        }

        /// <summary>
        /// Gets jwt token from request
        /// </summary>
        /// <returns><seealso cref="string"/> jwt token</returns>
        private string GetJwtToken()
        {
            // get token value from request 'Authorization' header
            string jwtToken = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(jwtToken) || string.IsNullOrWhiteSpace(jwtToken)) return null;

            // remove 'bearer' from start of the token to avoid format exception while decoding
            if (jwtToken.Trim().StartsWith("bearer", true, null)) return jwtToken.Trim().Remove(0, 6).Trim();

            return jwtToken.Trim();
        }

        #endregion
    }
}
