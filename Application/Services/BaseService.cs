using Application.Data;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class BaseService(IConfiguration configuration, IApplicationDbContext applicationDbContext)
    {

    }
}
