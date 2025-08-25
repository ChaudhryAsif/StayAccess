using System;
using System.ComponentModel.DataAnnotations;

namespace StayAccess.DAL.DomainEntities
{
    public class EmailLogger
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public string Message { get; set; }
        public string Recipients { get; set; }
        public int? ReservationId { get; set; }
        public int Status { get; set; }
        public string Error { get; set; }
    }
}
