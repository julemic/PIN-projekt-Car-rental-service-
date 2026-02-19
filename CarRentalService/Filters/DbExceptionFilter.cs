using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Filters
{
    public class DbExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<DbExceptionFilter> _logger;

        public DbExceptionFilter(ILogger<DbExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed");
                context.Result = new RedirectToActionResult("Error", "Home", null);
                context.ExceptionHandled = true;
            }
        }
    }
}
