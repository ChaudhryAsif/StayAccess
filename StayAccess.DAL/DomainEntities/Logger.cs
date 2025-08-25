using StayAccess.DTO.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StayAccess.DAL.DomainEntities
{
    public class Logger
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public LogType LogTypeId { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public string Message { get; set; }

        public string StackTrace { get; set; }

        public int? ReservationId { get; set; }


    }
}