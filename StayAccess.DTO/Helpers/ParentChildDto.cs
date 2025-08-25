
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.DTO.Helpers
{
    public class ParentChildDto<P,C>
    {
        public P Parent { get; set; }
        public C Child { get; set; }
    }
}
