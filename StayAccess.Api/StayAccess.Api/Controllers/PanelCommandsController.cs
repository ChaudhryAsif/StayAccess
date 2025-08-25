using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Responses.Arches;
using StayAccess.Tools.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PanelCommandsController : BaseController
    {

        private readonly ILoggerService<PanelCommandsController> _loggerService;

        public PanelCommandsController(ILoggerService<PanelCommandsController> loggerService)
        {
            _loggerService = loggerService;
        }

        [HttpPost]
        [Route("PulseDoor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PulseDoorResponseDto>> PulseDoorAsync(PulseDoorRequestDto pulseDoorRequestDto)
        {
            string responseString = string.Empty;

            try
            {
                _loggerService.Add(LogType.Information, $"Setting Panel Command -> Pulse Door: {JsonSerializer.Serialize(pulseDoorRequestDto)}.", null);

                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                };

                using (HttpClient client = new HttpClient(clientHandler))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var json = JsonSerializer.Serialize(pulseDoorRequestDto);
                    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://100.12.159.106:11001/api/PanelCommands/PulseDoor?HCAuthToken=687616817qkaPfGRcF9hC-l1947xqd_ED1h2Huwi-ZzekTC9UixM=412992017687", httpContent);
                    responseString = await response.Content.ReadAsStringAsync();

                    // if http request fails
                    if (!response.IsSuccessStatusCode)
                        throw new Exception(response.ReasonPhrase);
                }

                // read response content as string and deserialize it
                _loggerService.Add(LogType.Information, $"Panel Command -> Pulse Door: {JsonSerializer.Serialize(pulseDoorRequestDto)} set successfully. Response -> {responseString}", null);

                PulseDoorResponseDto apiResponse = JsonSerializer.Deserialize<PulseDoorResponseDto>(responseString);
                return Ok(apiResponse);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error occured in Panel Command -> Pulse Door: {JsonSerializer.Serialize(pulseDoorRequestDto)}. Error: {ex.Message}. Response -> {responseString}";
                _loggerService.Add(LogType.Error, errorMessage, null, ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }
    }
}
