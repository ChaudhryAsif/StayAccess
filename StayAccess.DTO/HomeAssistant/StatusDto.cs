using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.HomeAssistant
{
    public class ErrorDto
    {
        public int CodeTransactionId { get; set; }
        public bool Error { get; set; }
        public string ErrorMessage { get; set; }
    }
}
