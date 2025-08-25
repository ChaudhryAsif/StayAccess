using System.Collections.Generic;

namespace StayAccess.DTO
{
    public class FetchRequestDto
    {
        /// <summary>
        /// This will be the "UniqueKey" column value of QueryConfig
        /// </summary>
        public string UniqueKey { get; set; }

        /// <summary>
        /// This will be the "LinkId" column value of Link
        /// </summary>
        public string LinkId { get; set; }

        public string ReservationId { get; set; }

        public string BuildingId { get; set; }

        /// <summary>
        /// This will have dynamic filters information
        /// </summary>
        public List<FilterDto> Filters { get; set; }

        public List<SortDto> Sorts { get; set; }

        public int PageSize { get; set; }

        public int Page { get; set; }

        public void SetDefaultPagination()
        {
            if (Page == 0)
                Page = 1;

            if (PageSize == 0)
                PageSize = 25;
        }
    }
}