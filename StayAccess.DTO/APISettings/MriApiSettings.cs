using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.APISettings
{
    public class MriApiSettings
    {
        public string BaseURL { get; set; }
        public string VendorId { get; set; }
        public string SecretKey { get; set; }
        public string ApplicationType { get; set; }
    }
}
