using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net;


namespace Architecture_1.API.Filters.ExceptionFilters
{
    public class HttpExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            Console.WriteLine(context.Exception);

            int status = (int)HttpStatusCode.InternalServerError;
            // string message = "Lỗi hệ thống!";
            string message = "Something went wrong!";
            
            if (context.Exception is HttpRequestException httpRequestException)
            {
                status = (int)HttpStatusCode.BadRequest; // Hoặc status khác tùy vào lỗi
                message = httpRequestException.Message;
            }

            context.Result = new ObjectResult(new
            {
                statusCode = status,
                message = message,
                timestamp = DateTime.UtcNow.ToString("o")
            })
            {
                StatusCode = status
            };

            context.ExceptionHandled = true;
        }
    }
}
