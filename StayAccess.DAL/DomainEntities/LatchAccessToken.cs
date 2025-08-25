using System;


namespace StayAccess.DAL.DomainEntities
{
    public class LatchAccessToken
    {
        public int Id { get; set; }
        public string AccessToken { get;set; }
        public int Expires { get; set; }
        public DateTime DateAdded {  get; set; }
    }
}
