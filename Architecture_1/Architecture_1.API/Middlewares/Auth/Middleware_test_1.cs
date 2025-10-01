namespace Architecture_1.API.Middlewares.Auth
{
    public class Middleware_test_1
    {
        private readonly RequestDelegate _next;

        public Middleware_test_1(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // Kiểm tra đường dẫn yêu cầu
            string requestPath = httpContext.Request.Path;
            Console.WriteLine($"Middleware 1 Request Path: {requestPath}");
            if (1 == 1) // Điều kiện này là sai, nên không bao giờ thực hiện _next
            {

                await _next(httpContext);
            }
            else
            {
                // Thiết lập mã trạng thái HTTP phù hợp cho lỗi
                httpContext.Response.StatusCode = StatusCodes.Status200OK; // Ví dụ: 400 Bad Request
                httpContext.Response.ContentType = "application/json"; // Đặt kiểu nội dung là JSON

                // Tạo đối tượng lỗi và chuyển đổi nó thành JSON
                var errorResponse = new { error = "INVALID FROM MIDDLEWARE 1" };
                await httpContext.Response.WriteAsJsonAsync(errorResponse); // Ghi JSON vào phản hồi

                return; // Đảm bảo không tiếp tục xử lý yêu cầu
            }
        }
    }

    public static class Middleware_test_1Extensions
    {
        public static IApplicationBuilder Middleware_test_1(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Middleware_test_1>();
        }
    }
}
