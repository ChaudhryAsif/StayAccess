namespace StayAccess.DTO
{
    public class SortDto
    {
        /// <summary>
        /// It will have column name
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// It will be asc or desc 
        /// </summary>
        public string Direction { get; set; }

        /// <summary>
        /// It will be will priority of columns for sorting
        /// </summary>
        public int Priority { get; set; }
    }
}
