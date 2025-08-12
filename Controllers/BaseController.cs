using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace backend
{
    [Route("api/[Controller]")]
    [ApiController]
    [Authorize]
    public class BaseController(IHttpContextAccessor httpContextAccessor, ILogger logger, IApplicationDbContext ctx, IConfiguration configuration, ComplexReadService complexReadService) : ControllerBase
    {
        public bool IsAdmin => HasRole("دسترسی تمام صفحات کاربری") || HasRole("برنامه نویسان");

        public bool IsProgramer => HasRole("برنامه نویسان");


        public int ProjectId = configuration.GetSection("Project:ProjectId").Get<int?>() ?? throw new Exception("Set Project configs - missing ProjectId");
        public string UserName => httpContextAccessor.HttpContext?.User.Identity?.GetUserName() ?? "";
        public string FullName => httpContextAccessor.HttpContext?.User.Identity?.GetFullName() ?? "";
        public int OfficeId => httpContextAccessor.HttpContext?.User.Identity?.GetCurrentOfficeId() ?? 0;
        public int PersonId => httpContextAccessor.HttpContext?.User.Identity?.GetCurrentPersonId() ?? 0;
        public List<int> OfficeIdList => httpContextAccessor.HttpContext?.User.Identity?.GetOfficeIdList() ?? [];

        public IList<Permission> Permissions
        {
            get
            {
                //return _ctx.SpGetCurrentPermissions(ProjectId, UserName, OfficeId).Select(x => new Permission()
                //{
                //    PermissionId = x.PermissionId,
                //    Access = x.Access,
                //    PermissionName = x.PermissionName.ToLower(),
                //    PermissionType = x.PermissionType,
                //    RoleName = x.RoleName.ToLower(),
                //}).ToList();

                return (complexReadService.GetCurrentPermissionsAsync(ProjectId, UserName, OfficeId).Result).Select(x => new Permission()
                {
                    PermissionId = x.PermissionId,
                    Access = x.Access,
                    PermissionName = x.PermissionName.ToLower(),
                    PermissionType = x.PermissionType,
                    RoleName = x.RoleName.ToLower(),
                }).ToList();

            }
        }

        protected bool Can(string permission, PermissionAccess access = PermissionAccess.Any)
        {
            if (access == PermissionAccess.Any)
            {
                return Permissions.Any(x => x.PermissionName == permission.ToLower());
            }

            return Permissions.Any(x => x.PermissionName == permission.ToLower() && x.Access == access.ToString());
        }

        protected bool Cannot(string permission, PermissionAccess access = PermissionAccess.Any)
        {
            return !Can(permission, access);
        }

        protected bool HasRole(string role)
        {
            return Permissions.Any(x => x.RoleName == role.ToLower());
        }
    }
}
