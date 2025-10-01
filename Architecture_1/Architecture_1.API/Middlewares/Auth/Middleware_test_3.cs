namespace Architecture_1.API.Middlewares.Auth
{
    public class Middleware_test_3
    {
        private readonly RequestDelegate _next;

        public Middleware_test_3(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Kiểm tra đường dẫn yêu cầu
            string requestPath = context.Request.Path;
            Console.WriteLine($"Middleware 3 Request Path: {requestPath}");

            string header_content = context.Request.Headers["header-content"].FirstOrDefault();
            if (header_content != null)
            {
                await context.Response.WriteAsJsonAsync(new { HEADER_TRONG_MIDDLEWARE_3 = header_content }); // Ghi JSON vào phản hồi

                return;
            }
            else
            {
                await _next(context);
            }
        }
    }

    public static class Middleware_test_3Extensions
    {
        public static IApplicationBuilder UseMiddleware_test_3(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Middleware_test_3>();
        }
    }
}
