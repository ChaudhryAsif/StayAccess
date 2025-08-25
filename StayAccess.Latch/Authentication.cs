using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.Doors.LatchDoor;
using StayAccess.Tools.Interfaces;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using static StayAccess.DTO.Doors.LatchDoor.LatchApi;

namespace StayAccess.Latch
{
    public  class Authentication
    {
        public static async Task SignRequestAsync(HttpRequestMessage request, LatchApi latchApi, ILogService logRepo, StayAccessDbContext dbContext, IGenericService<LatchAccessToken> tokenRepo)
        {
            string token = await GetAccessToken(latchApi, logRepo, dbContext, tokenRepo);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",token);
           // client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");CANT PASS IN CLIENT CUZ IF FAILS IT RETRYS, AND CANT ADD AUTHORIZATION TWICE TO CLIENT
        }
        public static async Task<string> GetAccessToken(LatchApi latchApi, ILogService logRepo, StayAccessDbContext dbContext, IGenericService<LatchAccessToken> tokenRepo)
        {
            
            LatchAccessToken latchAccessToken = dbContext.LatchAccessToken?.ToList().OrderByDescending(x => x.DateAdded).FirstOrDefault();
            //var expire = latchAccessToken.Expires;
            if(latchAccessToken == default || latchAccessToken?.DateAdded.AddHours(24)  < DateTime.UtcNow)
            {
                string endpoint = latchApi.LatchEndpoints.Auth;
                var authRequest = latchApi.LatchJwt;
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsJsonAsync(endpoint, authRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync();
                        LatchToken accessToken = JsonSerializer.Deserialize<LatchToken>(message);
                        LatchAccessToken newToken = new LatchAccessToken { AccessToken = accessToken.access_token, DateAdded = DateTime.UtcNow, Expires = accessToken.expires_in };
                        //SAVE ACCESS TOKEN, DELETE THE FIRST ONE?
                        tokenRepo.AddWithSave(newToken);
                        return accessToken.access_token;
                    }
                    else
                    {
                        switch ((int)response.StatusCode)
                        {
                            case 400:
                                throw new Exception("Error occured when trying to generate an access token for latch request. Check that request includes the `grant_type` parameter.");
                            case 401:
                                throw new Exception("Error occured when trying to generate an access token for latch request. Check the request is using the correct value for `client_id` and `client_secret`.");
                            case 403:
                                throw new Exception("Error occured when trying to generate an access token for latch request. Check the API request to make sure the `audience` field has the right value.");
                            case 500:
                                throw new Exception("Error occured when trying to generate an access token for latch request. Contact Latch Support to help debug this issue.");
                        }
                        return null;
                    }
                }
            }
            else
            {
                return latchAccessToken.AccessToken;
            }
        }
    }
}
