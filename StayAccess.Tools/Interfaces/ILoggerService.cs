using StayAccess.DAL.DomainEntities;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using System;
using System.Linq.Expressions;

namespace StayAccess.Tools.Interfaces
{
    public interface ILoggerService<T> where T : class
    {
        DAL.DomainEntities.Logger Add(LogType logType, string message, int? reservationId, string stackTrace = "", bool saveChanges = true);
        void DeleteLoggersWithoutSave(Expression<Func<DAL.DomainEntities.Logger, bool>> predicate);
        public DataSourceResultDto GetLogsFromRequest(FetchRequestDto request);
    }
}
