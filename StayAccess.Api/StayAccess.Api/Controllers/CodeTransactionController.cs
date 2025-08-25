using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.BLL.Interfaces;
using StayAccess.Tools.Interfaces;
using System;

namespace StayAccess.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CodeTransactionController : BaseController
    {
        private readonly ICodeTransactionService _codeTransactionService;
        private readonly ILoggerService<CodeTransactionController> _loggerService;

        public CodeTransactionController(ICodeTransactionService codeTransactionService, ILoggerService<CodeTransactionController> loggerService)
        {
            _codeTransactionService = codeTransactionService;
            _loggerService = loggerService;
        }

        [HttpGet]
        [Route("ExecuteTransactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult ExecuteTransactions()
        {
            try
            {
                _codeTransactionService.ExecuteTransactions();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
