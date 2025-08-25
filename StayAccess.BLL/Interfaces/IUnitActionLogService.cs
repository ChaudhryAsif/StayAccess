using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.BLL.Interfaces
{
    public interface IUnitActionLogService
    {
        Task<UnitActionLog> AddAsync(UnitActionLogRequestDto requestDto);
        DataSourceResultDto GetLogsFromRequest(FetchRequestDto request);
    }
}
