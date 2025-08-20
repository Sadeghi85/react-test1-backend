
using Application;
using Application.Data;
using Application.Models.Contact;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Api.Controllers
{

    public class ContactController(IHttpContextAccessor httpContextAccessor, ILogger logger, IApplicationDbContext ctx, IConfiguration configuration, ComplexReadService complexReadService, ContactService contactService) : BaseController(httpContextAccessor, logger, ctx, configuration, complexReadService)
    {


        // GET: api/contacts
        [HttpPost("GetContacts")]
        public async Task<OperationResult<IEnumerable<Contact>>> GetContacts([FromBody] GetContactsInput getContactsInput)
        {

            return await contactService.GetContacts(getContactsInput);
        }

        // POST: api/contacts
        [HttpPost("CreateContact")]
        public async Task<OperationResult<Contact>> CreateContact([FromBody] Contact input)
        {
            return await contactService.CreateContact(input);
        }
    }
}
