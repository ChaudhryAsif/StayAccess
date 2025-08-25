using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Responses.Latch
{
    public class LatchResponse<TMessage>
    {
        //public Header header { get; set; }
        public Payload<TMessage> payload { get; set; }
        //public Trailer trailer { get; set; }
        //public string message { get;set; }
        //public DateTime timestamp { get;set; }
        //public int status { get; set; }
        //public string error { get; set; }
        //public string path { get; set; }
    }

    public class Header
    {
        public string messageType { get; set; }
        public string apiVersion { get; set; }
        public long sendingTime { get; set; }
    }

    public class Payload<TMessage>
    {
        public TMessage message { get; set; }
    }

    public class Trailer
    {
        public long checkSum { get; set; }
    }

    public interface ILatchMessage
    {
       // string UserUUid { get; set; }
    }

    public interface ILatchResponse
    {
    }
}
