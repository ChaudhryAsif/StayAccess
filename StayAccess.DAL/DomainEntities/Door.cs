using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DAL.DomainEntities
{
    public class Doors
    {
       public List<Door> doors { get; set; } 
        
    }
    public class Door
    {
        public string uuid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string buildingUuid { get; set; }
        public string accessibilityType { get; set; }
        public bool isConnected { get; set; }
    }
}
