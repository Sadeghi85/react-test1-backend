using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Principal;

namespace backend
{
    public static class IdentityExtensions
    {


        public static string GetUserName(this IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst("preferred_username");

            return claim?.Value ?? string.Empty;
        }

        public static string GetFullName(this IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst("FullName");

            return claim?.Value ?? string.Empty;
        }

        public static int GetCurrentPersonId(this IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst("CurrentPersonId");

            return JsonConvert.DeserializeObject<int>(claim?.Value ?? "0");
        }

        public static int GetCurrentOfficeId(this IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst("CurrentOfficeId");

            return JsonConvert.DeserializeObject<int>(claim?.Value ?? "0");
        }

        public static List<int> GetOfficeIdList(this IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var claim = claimsIdentity?.FindFirst("OfficeIdList");

            return JsonConvert.DeserializeObject<List<int>>(claim?.Value ?? "[]") ?? [];
        }
    }
}
