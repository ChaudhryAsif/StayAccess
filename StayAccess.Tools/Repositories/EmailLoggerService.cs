using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Interfaces;
using System;

namespace StayAccess.Tools.Repositories
{
    public class EmailLoggerService : IEmailLoggerService
    {
        private readonly IGenericService<EmailLogger> _emailLoggerRepo;

        public EmailLoggerService(IGenericService<EmailLogger> emailLoggerRepo)
        {
            _emailLoggerRepo = emailLoggerRepo;
        }

        public void Add(string message, string recipients, EmailStatus emailStatus, int? reservationId, string error)
        {
            // add email logger info
            EmailLogger entity = new EmailLogger
                {
                    CreatedDate = DateTime.UtcNow,
                    Message = message,
                    ReservationId = reservationId,
                    Recipients = recipients,
                    Status = (int)emailStatus,
                    Error = error
                };

            _emailLoggerRepo.AddWithSave(entity);
        }
    }
}
