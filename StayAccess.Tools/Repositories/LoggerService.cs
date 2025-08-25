using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StayAccess.DAL;
using StayAccess.DAL.DomainEntities;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using StayAccess.DTO.Enums;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace StayAccess.Tools.Repositories
{
    public class LoggerService<T> : ILoggerService<T> where T : class
    {
        private readonly ILogger<T> _logger;
        private readonly IGenericService<Logger> _loggerRepo;
        private readonly IConfiguration _configuration;

        public LoggerService(IGenericService<DAL.DomainEntities.Logger> loggerRepo, ILogger<T> logger, IConfiguration configuration)
        {
            _loggerRepo = loggerRepo;
            _logger = logger;
            _configuration = configuration;
        }

        #region public methods

        public Logger Add(LogType logType, string message, int? reservationId, string stackTrace = "", bool saveChanges = true)
        {
            try
            {
                // setting log level for azure application insights
                LogLevel logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), logType.ToString());

                // adding log on azure application insights
                if (string.IsNullOrWhiteSpace(stackTrace))
                    _logger.Log(logLevel, message);
                else
                    //_logger.Log(logLevel, message, stackTrace);
                    _logger.Log(logLevel, message + "\n StackTrace: " + stackTrace);

                // adding log on customer logger table in database
                Logger logger = GetLoggerObject(logType, message, reservationId, stackTrace);

                if (saveChanges)
                    _loggerRepo.AddWithSave(logger);
                else
                    _loggerRepo.Add(logger);

                return logger;
            }
            catch (Exception ex)
            {
                throw;
            }

            static Logger GetLoggerObject(LogType logType, string message, int? reservationId, string stackTrace)
            {
                return new Logger()
                {
                    LogTypeId = logType,
                    CreatedDate = DateTime.UtcNow,
                    Message = message,
                    ReservationId = reservationId,
                    StackTrace = stackTrace
                };
            }
        }

        public void DeleteLoggersWithoutSave(Expression<Func<DAL.DomainEntities.Logger, bool>> predicate)
        {
            try
            {
                IQueryable<DAL.DomainEntities.Logger> loggers = GetEntities(predicate);
                if (loggers != null && loggers.Any())
                {
                    foreach (var logger in loggers)
                    {
                        _loggerRepo.Delete(logger);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private class LoggerWithLogIdEnumNameDto
        {
            public string LogIdEnumName { get; set; }
            public Logger Logger { get; set; }
        }

        public DataSourceResultDto GetLogsFromRequest(FetchRequestDto request)
        {
            try
            {

                var query = _loggerRepo.List();

                //var query = _stayAccessDbContext.Logger.Select(x => new LoggerWithLogIdEnumName()
                //{
                //    Logger = x,
                //    LogIdEnumName = Enum.GetName(typeof(LogType), x.LogTypeId),
                //});


                // Enum.GetName(typeof(LogType), x.LogTypeId),



                //  var query = _loggerRepo.List(); // Enum.GetName(typeof(LogType), x.LogTypeId),

                //IEnumerable<LoggerResponseDto> query = _loggerRepo.List(x => x.ReservationId == int.Parse(request.ReservationId)).AsEnumerable().Select(x => new LoggerResponseDto
                //{
                //    LogType = Enum.GetName(typeof(LogType), x.LogTypeId),
                //    CreatedDate = x.CreatedDate,
                //    Message = x.Message
                //});


                if (request.Filters.HasAny())
                {
                    foreach (FilterDto filter in request.Filters)
                    {
                        if (!string.IsNullOrEmpty(filter.Field))
                        {
                            string field = filter.Field.ToLower();
                            string value = !string.IsNullOrEmpty(filter.Value) ? filter.Value.ToLower() : string.Empty;

                            if (field.Equals("id"))
                                query = query.Where(x => x.Id.ToString().Equals(value));

                            if (field.Equals("logtypeid"))
                            {

                                bool isEnum = Enum.TryParse<LogType>(value, out LogType logType);

                                //LogType logType = (LogType)Enum.Parse(typeof(LogType), value);
                                //query = query.Where(x => x.LogTypeId == logType);
                                query = query.Where(x => x.LogTypeId == logType);
                            }

                            if (field.Equals("createddate"))
                                query = query.Where(x => x.CreatedDate == Convert.ToDateTime(value).Date);

                            if (field.Equals("message"))
                                query = query.Where(x => x.Message == value);

                            if (field.Equals("stacktrace"))
                                query = query.Where(x => x.StackTrace == value);

                            if (field.Equals("reservationid"))
                                query = query.Where(x => x.ReservationId.ToString().Equals(value));
                        }
                    }
                }

                if (request.Sorts.HasAny())
                {
                    SortDto sort = request.Sorts.FirstOrDefault();
                    if (!string.IsNullOrEmpty(sort?.Field))
                    {
                        bool isAscending = sort.Direction.Equals("asc");
                        switch (sort?.Field.ToLower())
                        {
                            case "id":
                                query = isAscending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
                                break;
                            case "logtypeid":
                                query = isAscending ? query.OrderBy(x => x.LogTypeId) : query.OrderByDescending(x => x.LogTypeId);
                                break;
                            case "createddate":
                                query = isAscending ? query.OrderBy(x => x.CreatedDate) : query.OrderByDescending(x => x.CreatedDate);
                                break;
                            case "message":
                                query = isAscending ? query.OrderBy(x => x.Message) : query.OrderByDescending(x => x.Message);
                                break;
                            case "stacktrace":
                                query = isAscending ? query.OrderBy(x => x.StackTrace) : query.OrderByDescending(x => x.StackTrace);
                                break;
                            case "reservationid":
                                query = isAscending ? query.OrderBy(x => x.ReservationId) : query.OrderByDescending(x => x.ReservationId);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Id);
                }
                var dataSourceResult = _loggerRepo.PagedList(query, request.Page, request.PageSize);
                List<Logger> loggers = (List<Logger>)dataSourceResult.Data;
                //ListExtension<Logger> loggers = (List<Logger>)dataSourceResult.Data;

                List<LoggerWithLogIdEnumNameDto> loggerWithLogIdEnumNameDtos = new();
                if (loggers.HasAny())
                {
                    foreach (var logger in loggers)
                    {
                        loggerWithLogIdEnumNameDtos.Add(new LoggerWithLogIdEnumNameDto
                        {
                            Logger = logger,
                            LogIdEnumName = Enum.GetName(typeof(LogType), logger.LogTypeId),
                        });
                    }
                }

                dataSourceResult.Data = loggerWithLogIdEnumNameDtos;
                return dataSourceResult;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region private methods
        private IQueryable<DAL.DomainEntities.Logger> GetEntities(Expression<Func<DAL.DomainEntities.Logger, bool>> predicate)
        {
            try
            {
                return _loggerRepo.List(predicate);
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}