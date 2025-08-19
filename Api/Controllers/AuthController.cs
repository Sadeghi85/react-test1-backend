using Application;
using Application.Data;
using Application.Services;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using ILogger = Serilog.ILogger;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IConfiguration configuration, IHostEnvironment environment, ILogger logger, ComplexReadService complexReadService, TokenService tokenService, IApplicationDbContext ctx, IHttpContextAccessor httpContextAccessor) : ControllerBase
    {
        private string redirectUri = (environment.IsDevelopment()
                    ? configuration["IdentityServer:DevelopmentRedirectUri"]
                    : configuration["IdentityServer:RedirectUri"]) ?? throw new Exception("Set IdentityServer configs - missing redirect URI");
        private string clientId = configuration["IdentityServer:ClientId"] ?? throw new Exception("Set IdentityServer configs - missing ClientId");
        private string clientSecret = configuration["IdentityServer:ClientSecret"] ?? throw new Exception("Set IdentityServer configs - missing ClientSecret");
        private string authority = configuration["IdentityServer:Authority"] ?? throw new Exception("Set IdentityServer configs - missing Authority");
        private string scope = configuration["IdentityServer:Scope"] ?? throw new Exception("Set IdentityServer configs - missing Scope");


        private string developmentBasePath = configuration["Project:DevelopmentBasePath"] ?? throw new Exception("Set Project configs - missing DevelopmentBasePath");


        [HttpGet("login")]
        public IActionResult Login()
        {
            var requestUrl = new RequestUrl($"{authority}/connect/authorize");

            var authorizeUrl = requestUrl.CreateAuthorizeUrl(
                clientId: clientId,
                responseType: "code",
                redirectUri: redirectUri,
                scope: scope,
                state: Guid.NewGuid().ToString("N"),
                nonce: Guid.NewGuid().ToString("N")
            );

            //var query = new QueryString()
            //                .Add("client_id", clientId)
            //                .Add("response_type", "code")
            //                .Add("scope", "openid profile offline_access")
            //                .Add("redirect_uri", redirectUri);

            //var authorizeUrl = $"{authority}/connect/authorize" + query;


            return Ok(new { authorizeUrl = authorizeUrl });
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            //// Get userInfo from Identity Server
            ///
            var tokenClient = new HttpClient();
            var tokenResponse = await tokenClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = $"{authority}/connect/token",
                ClientId = clientId,
                ClientSecret = clientSecret,
                Code = code,
                RedirectUri = redirectUri
            });

            if (tokenResponse.IsError)
            {
                var _errorMessage = $"Token request failed: {tokenResponse.Error}";

                if (!string.IsNullOrEmpty(tokenResponse.ErrorDescription))
                    _errorMessage += $"\n\nDescription: {tokenResponse.ErrorDescription}";
                if (tokenResponse.Exception != null)
                    _errorMessage += $"\n\nException: {tokenResponse.Exception}";

                logger.Error(_errorMessage);


                await Response.WriteAsync("Identity Server error");
                return StatusCode(503, "Identity Server error");
            }

            var userInfoResponse = await tokenClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = $"{authority}/connect/userinfo",
                Token = tokenResponse.AccessToken
            });

            if (userInfoResponse.IsError)
            {
                var _errorMessage = $"UserInfo request failed: {userInfoResponse.Error}";

                if (userInfoResponse.Exception != null)
                    _errorMessage += $"\n\nException: {userInfoResponse.Exception}";

                logger.Error(_errorMessage);

                await Response.WriteAsync("Identity Server error");
                return StatusCode(503, "Identity Server error");
            }

            var preferredUsername = userInfoResponse.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

            if (preferredUsername == null)
            {
                var _errorMessage = $"preferredUsername not found in userInfoResponse";

                logger.Error(_errorMessage);

                await Response.WriteAsync("Identity Server error");
                return StatusCode(503, "Identity Server error");
            }

            //// Get additional claims from DB
            ///
            var office = await complexReadService.GetCurrentPersonOfficeAsync(preferredUsername);

            var FullName = office.First().FullName;

            var CurrentPersonId = office.Select(x => x.PersonId).First();

            var CurrentOfficeId = office.Where(x => x.MainPosition == true).OrderBy(x => x.PersonOfficeId).Select(x => x.OfficeId).FirstOrDefault();
            if (CurrentOfficeId == default)
            {
                CurrentOfficeId = office.OrderBy(x => x.PersonOfficeId).Select(x => x.OfficeId).First();
            }

            var OfficeIdList = office.Select(x => x.OfficeId).ToList();

            var allClaims = new List<Claim>();

            allClaims.AddRange(userInfoResponse.Claims);

            allClaims.RemoveAll(c => c.Type == "FullName" || c.Type == "CurrentPersonId" || c.Type == "CurrentOfficeId" || c.Type == "OfficeIdList");

            allClaims.AddRange(new List<Claim>()
            {
                new Claim("FullName", FullName),
                new Claim("CurrentPersonId", JsonConvert.SerializeObject(CurrentPersonId)),
                new Claim("CurrentOfficeId", JsonConvert.SerializeObject(CurrentOfficeId)),
                new Claim("OfficeIdList", JsonConvert.SerializeObject(OfficeIdList))
            });

            //// Create accessToken and refreshToken
            ///
            var identity = new ClaimsIdentity(allClaims, JwtBearerDefaults.AuthenticationScheme);
            var accessToken = tokenService.CreateAccessToken(identity);
            var refreshToken = tokenService.CreateRefreshToken();

            //// Save refresh token to DB mapped to user
            var refreshTokenRow = await ctx.TblRefreshTokens.FirstOrDefaultAsync(x => x.PreferredUsername == preferredUsername);
            var refreshTokenExpires = DateTime.UtcNow.AddDays(7);

            if (refreshTokenRow == null)
            {
                refreshTokenRow = new TblRefreshToken
                {
                    PreferredUsername = preferredUsername,
                    Expires = refreshTokenExpires,
                    RefreshToken = refreshToken,
                };

                ctx.TblRefreshTokens.Add(refreshTokenRow);

            }
            else
            {
                refreshTokenRow.RefreshToken = refreshToken;
                refreshTokenRow.Expires = refreshTokenExpires;
            }

            await ctx.SaveChangesAsync();


            //// Respond with accessTokens and redirects to SPA main page
            ///
            var request = httpContextAccessor.HttpContext?.Request;
            var frontendUrl = $"{request?.Scheme}://{request?.Host}{request?.PathBase}";

            if (environment.IsDevelopment())
            {
                frontendUrl = developmentBasePath;
            }


            var redirectUrl = $"{frontendUrl}/#accessToken={accessToken}";
            return Redirect(redirectUrl);


        }

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            //// Validate refreshToken from DB, get user
            ///

            var preferredUsername = httpContextAccessor.HttpContext?.User.Identity?.GetUserName();

            if (preferredUsername == null)
            {
                return Unauthorized();
            }

            var refreshTokenRow = await ctx.TblRefreshTokens.FirstOrDefaultAsync(x => x.PreferredUsername == preferredUsername);

            if (refreshTokenRow == null)
            {
                return Unauthorized();
            }

            if (refreshTokenRow.Expires < DateTime.UtcNow)
            {
                return Unauthorized();
            }

            //// Get additional claims from DB
            ///
            var office = await complexReadService.GetCurrentPersonOfficeAsync(preferredUsername);

            var FullName = office.First().FullName;

            var CurrentPersonId = office.Select(x => x.PersonId).First();

            var CurrentOfficeId = office.Where(x => x.MainPosition == true).OrderBy(x => x.PersonOfficeId).Select(x => x.OfficeId).FirstOrDefault();
            if (CurrentOfficeId == default)
            {
                CurrentOfficeId = office.OrderBy(x => x.PersonOfficeId).Select(x => x.OfficeId).First();
            }

            var OfficeIdList = office.Select(x => x.OfficeId).ToList();

            var allClaims = new List<Claim>();

            allClaims.AddRange(httpContextAccessor.HttpContext?.User.Claims!);

            allClaims.RemoveAll(c => c.Type == "FullName" || c.Type == "CurrentPersonId" || c.Type == "CurrentOfficeId" || c.Type == "OfficeIdList");

            allClaims.AddRange(new List<Claim>()
            {
                new Claim("FullName", FullName),
                new Claim("CurrentPersonId", JsonConvert.SerializeObject(CurrentPersonId)),
                new Claim("CurrentOfficeId", JsonConvert.SerializeObject(CurrentOfficeId)),
                new Claim("OfficeIdList", JsonConvert.SerializeObject(OfficeIdList))
            });

            //// Create accessToken and refreshToken
            ///
            var identity = new ClaimsIdentity(allClaims, JwtBearerDefaults.AuthenticationScheme);
            var accessToken = tokenService.CreateAccessToken(identity);
            var refreshToken = tokenService.CreateRefreshToken();

            //// Save refresh token to DB mapped to user
            var refreshTokenExpires = DateTime.UtcNow.AddDays(7);

            refreshTokenRow.RefreshToken = refreshToken;
            refreshTokenRow.Expires = refreshTokenExpires;

            await ctx.SaveChangesAsync();

            return Ok(new { accessToken = accessToken });
        }
    }
}
