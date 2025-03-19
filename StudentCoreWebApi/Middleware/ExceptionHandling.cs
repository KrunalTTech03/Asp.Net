using System.Net;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace StudentCoreWebApi.Middleware
{
    public class ExceptionHandling
    {
        private readonly RequestDelegate _next;

        public ExceptionHandling (RequestDelegate next)
        {
            _next = next;
        }

        public  async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An Unknown Error ocurred");
                await HandleExceptionAsync(context, ex);
            }
    }

        public static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var statusCode = ex switch
            {
                KeyNotFoundException => HttpStatusCode.NotFound,
                ArgumentException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            response.StatusCode = (int)statusCode;

            var errorResponse = new
            {
                Message = ex.Message,
                statusCode = statusCode,
            };
            return response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
