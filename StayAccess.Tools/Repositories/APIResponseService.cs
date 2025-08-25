using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class APIResponseService
    {
        public static bool IsSuccessCode(HttpStatusCode statusCode)
        {
            try
            {
                int asInt = (int)statusCode;
                return asInt >= 200 && asInt <= 299;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
