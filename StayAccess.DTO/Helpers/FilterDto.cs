namespace StayAccess.DTO
{
    public class FilterDto
    {
        /// <summary>
        /// It will have column/parameter name
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// It will have match modes like (=, >, <, etc) 
        /// </summary>
        public string MatchMode { get; set; }

        /// <summary>
        /// It will be either "And" or "OR"
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// It will have filter/parameter value
        /// </summary>
        public string Value { get; set; }
    }
}
