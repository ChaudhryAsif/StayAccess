using StayAccess.DTO.Enums;

namespace StayAccess.Tools.Interfaces
{
    public interface IEmailLoggerService
    {
        void Add(string message, string recipients, EmailStatus emailStatus, int? reservationId, string error);
    }
}
