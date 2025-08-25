using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.Logger
{
    public class LoggerResponseDto
    {
        public string LogType { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Message { get; set; }
    }
}
