using Microsoft.AspNetCore.Mvc.Filters;

namespace Architecture_1.API.Filters
{
    public class LoggingFilter : IActionFilter
    {

        public LoggingFilter()
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            // Kiểm tra các phần khác của request
            var scheme = request.Scheme;  // http hoặc https
            var host = request.Host.ToString();  // localhost:5000 hoặc tên miền
            var path = request.Path.ToString();  // /api/controller/action
            var queryString = request.QueryString.ToString();  // ?key=value

            // Lấy URL đầy đủ
            var fullUrl = $"{scheme}://{host}{path}{queryString}";

            // In ra URL đầy đủ
            Console.WriteLine($"Full URL: {fullUrl}");

            // Lấy request URL
            var requestUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

            // Ghi log hoặc xử lý URL theo nhu cầu
            Console.WriteLine($"\n\n\nActionFilter PreHandle Request URL: {requestUrl}\n");
        }

        public void OnActionExecuted(ActionExecutedContext context) {
            // Lấy request URL
            var requestUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

            // Ghi log hoặc xử lý URL theo nhu cầu
            Console.WriteLine($"\nFilter PostHandle Request URL: {requestUrl}\n\n\n");

        }
    }
}
