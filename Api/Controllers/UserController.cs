
using Application.Data;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Api.Controllers
{

    public class UserController(IHttpContextAccessor httpContextAccessor, ILogger logger, IApplicationDbContext ctx, IConfiguration configuration, ComplexReadService complexReadService) : BaseController(httpContextAccessor, logger, ctx, configuration, complexReadService)
    {


        [HttpGet("info")]
        public async Task<IActionResult> Info()
        {

            var userInfo = await complexReadService.GetUserInfoAsync(UserName, OfficeId, ProjectId);



            return Ok(userInfo);
        }

    }
}
