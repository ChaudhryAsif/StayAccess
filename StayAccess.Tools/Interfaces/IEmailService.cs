using StayAccess.DTO.Email;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StayAccess.Tools.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendAsync(EmailMessage emailMessage, List<string> recipients = null, int? reservationId = null);

        string GetListAsHtmlTableString<T>(List<T> list = null, T item = default);
    }
}
