using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StayAccess.Api.Interfaces;
using StayAccess.DTO.Request;
using StayAccess.DTO.Responses;
using StayAccess.DTO.Responses.Arches;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IHttpClientService _httpClientService;

        public AuthController(IHttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login(LoginRequest loginRequest)
        {
            try
            {
                var response = await _httpClientService.PostAsync(loginRequest, "auth/login", false);

                // if http request fails for login
                if (!response.IsSuccessStatusCode)
                    throw new Exception(response.ReasonPhrase);

                // read response content as string and deserialize it as RolesResponseDto
                string serializedString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<APIResponse>(serializedString);

                if (apiResponse != null && apiResponse.Data != null)
                {
                    UserDto userInfo = JsonConvert.DeserializeObject<UserDto>(apiResponse.Data.ToString());
                    return Ok(new { Data = userInfo, Message = "Login Successful" });
                }
                else if (apiResponse.ErrorDetails != null && apiResponse.ErrorDetails.Any())
                {
                    var errorMessage = apiResponse.ErrorDetails.FirstOrDefault(x => !string.IsNullOrEmpty(x.ErrorMessage));
                    throw new Exception(errorMessage.ErrorMessage);
                }
                else
                {
                    throw new Exception("Login Failed");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Login Failed, {ex.Message}");
            }
        }

        [HttpPost("Logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var response = await _httpClientService.PostAsync(new { }, "auth/logout");
                return Ok(new { response.StatusCode });
            }
            catch (Exception ex)
            {
                return BadRequest($"Logout Failed, {ex.Message}");
            }
        }
    }
}
