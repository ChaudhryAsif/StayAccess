using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Enums;
using StayAccess.DTO.Request;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersistentTokenController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IPersistentTokenService _persistentTokenService;
        private readonly ILoggerService<PersistentTokenController> _loggerService;

        public PersistentTokenController(IConfiguration configuration, IPersistentTokenService persistentTokenService, ILoggerService<PersistentTokenController> loggerService)
        {
            _configuration = configuration;
            _persistentTokenService = persistentTokenService;
            _loggerService = loggerService;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("Generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<PersistentToken> Generate()
        {
            try
            {
                string guid = Guid.NewGuid().ToString();

                // generic user claims
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, guid),
                    new Claim(JwtRegisteredClaimNames.Jti, guid),
                    new Claim(JwtRegisteredClaimNames.GivenName, guid),
                    new Claim(JwtRegisteredClaimNames.FamilyName, guid),
                    new Claim(JwtRegisteredClaimNames.Email, guid)
                };

                // making jwt key and signing credentials
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // jwt token configurations
                var jwt = new JwtSecurityToken
                (
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddYears(10),
                    signingCredentials: signingCredentials
                );

                // generate token that is valid for 10 years
                string token = new JwtSecurityTokenHandler().WriteToken(jwt);

                // saving persistent token in databse
                PersistentToken generatedToken = _persistentTokenService.Add(new PersistentToken()
                {
                    PersistentJwtToken = token
                });

                return Ok(generatedToken);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in creating new persistent token. Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest($"Token Creation Failed, {ex.Message}");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PersistentToken>> VerifyAsync(TokenRequest tokenRequest)
        {
            try
            {
                if (tokenRequest is null)
                    throw new Exception("Token is missing");

                if (string.IsNullOrEmpty(tokenRequest.Token))
                    throw new Exception("Token is missing");

                PersistentToken token = await _persistentTokenService.GetByTokenAsync(tokenRequest.Token);
                if (token is null)
                    return NotFound("Token not found.");

                return Ok(token);
            }
            catch (Exception ex)
            {
                _loggerService.Add(LogType.Error, $"Error occured in token verification. Token: {tokenRequest.ToJsonString()} Error: {ex.Message}.", null, ex.StackTrace);
                return BadRequest($"Token Verification Failed, {ex.Message}");
            }
        }
    }
}