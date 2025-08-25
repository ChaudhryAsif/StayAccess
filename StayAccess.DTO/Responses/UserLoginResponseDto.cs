using System;
using System.Collections.Generic;

namespace StayAccess.DTO.Responses
{
    public enum APIErrors
    {
        ValidationError = 1,
        InvalidCredentials = 2,
        BadRequest = 3,
        MissingLocationInfo = 4,
        GeneralError = 5,
        NotFound = 6,
        IntervalServerError = 7
    }

    public class APIResponse
    {
        public APIResponse(object data, string token)
        {
            Data = data;
            Token = token;
        }

        public APIResponse(object data, string token, bool hasError, string message, int errorCode, List<ErrorDetail> errorDetail = null)
        {
            Data = data;
            Token = token;
            ErrorCode = errorCode;
            Message = message;
            ErrorDetails = errorDetail;
        }

        public int ErrorCode { get; set; }
        public string Message { get; set; }
        public List<ErrorDetail> ErrorDetails { get; set; }
        public object Data { get; set; }
        public string Token { get; set; }
        public string ErrorType { get; set; }

        public APIResponse()
        {
            ErrorType = "";
            ErrorCode = 0;
            Message = "";
            Data = null;
            ErrorDetails = new List<ErrorDetail>();
        }
    }

    public class ErrorDetail
    {
        public string DataField { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Code { get; set; }

        public int? PersonId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool IsADUser { get; set; }
        public string Token { get; set; }
        public DateTime ExpireTime { get; set; }
        public string Identifier { get; set; }
        public int? SalesRepId { get; set; }
        public string RoleNames { get; set; }

        public string Old_IdUser { get; set; }
        public List<string> Activities { get; set; }
    }
}
