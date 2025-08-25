using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace StayAccess.Api.Controllers
{
    public class BaseController : Controller
    {
        public string GetLoggedInUserName
        {
            get { return User.Claims.FirstOrDefault()?.Value ?? string.Empty; }
        }
    }
}
