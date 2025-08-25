using System;
using System.ComponentModel.DataAnnotations;

namespace StayAccess.DAL.DomainEntities
{
    public class PersistentToken
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; }
        
        public string PersistentJwtToken { get; set; }
    }
}
