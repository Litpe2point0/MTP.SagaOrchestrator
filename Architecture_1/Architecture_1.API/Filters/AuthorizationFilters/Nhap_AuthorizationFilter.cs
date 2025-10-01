using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Architecture_1.API.Filters.AuthorizationFilters
{
    public class Nhap_AuthorizationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            Console.WriteLine("[BEGIN AUTHORIZATION] ==> <OnAuthorization>");

            // Thực hiện kiểm tra quyền (Authorization), ví dụ:
            bool isAuthorized = true;
            if (!isAuthorized)
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}


