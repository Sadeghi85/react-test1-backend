
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
            //var totalCount = await ctx.TblContacts.CountAsync();
            //var rows = await ctx.TblContacts.OrderByDescending(x => x.Id).Skip(getContactsInput.PageIndex * getContactsInput.PageSize).Take(getContactsInput.PageSize).ToListAsync();

            //var getContactsOutput = new GetContactsOutput()
            //{
            //    Rows = rows,
            //    TotalCount = totalCount
            //};


            //return getContactsOutput;

            return await contactService.GetContacts(getContactsInput);
        }

        // POST: api/contacts
        [HttpPost("CreateContact")]
        public async Task<ActionResult<TblContact>> CreateContact(TblContact contact)
        {
            ctx.TblContacts.Add(contact);
            await ctx.SaveChangesAsync();

            // Returns a 201 Created status with the new contact
            return CreatedAtAction(nameof(GetContacts), new { id = contact.Id }, contact);
        }
    }
}
