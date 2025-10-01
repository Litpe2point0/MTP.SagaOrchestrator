using Microsoft.AspNetCore.Mvc.Filters;

namespace Architecture_1.API.Filters.ActionFilters
{
    public class Nhap_ActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            Console.WriteLine("[BEGIN ACTION] ==> <OnActionExecuting>");
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            Console.WriteLine("[END ACTION] ==> <OnActionExecuted>");

        }
    }
}


