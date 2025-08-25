using System;
using System.ComponentModel.DataAnnotations;

namespace StayAccess.DAL.DomainEntities
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
