using System.Collections;

namespace StayAccess.DTO
{
    public class DataSourceResultDto
    {
        public IEnumerable Data { get; set; }
        public int Total { get; set; }
    }
}