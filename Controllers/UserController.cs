
using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace backend.Controllers
{

    public class UserController(IHttpContextAccessor httpContextAccessor, ILogger logger, IApplicationDbContext ctx, IConfiguration configuration, ComplexReadService complexReadService) : BaseController(httpContextAccessor, logger, ctx, configuration, complexReadService)
    {

        [HttpGet("info")]
        public async Task<IActionResult> Info()
        {

            var userInfo = await complexReadService.GetUserInfoAsync(UserName, OfficeId);



            return Ok(userInfo);
        }

    }
}
