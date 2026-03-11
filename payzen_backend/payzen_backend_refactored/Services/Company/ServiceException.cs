using System;

namespace payzen_backend.Services.Company
{
    public class ServiceException : Exception
    {
        public int StatusCode { get; }

        public ServiceException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
