using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IBackupCodesService
    {
        Task HandleArchesBackups(string loggedInUserName);
        DataSourceResultDto PagedList(FetchRequestDto request);
        Task MarkAsUsedAsync(int backupReservationId, string triggeredBy);        
    }
}
