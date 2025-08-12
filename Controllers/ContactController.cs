
using backend.Data;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace backend.Controllers
{

    public class Sorting
    {
        public string Id { get; set; }
        public bool Desc { get; set; }
    }

    public class GetContactsInput
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public Sorting[] Sorting { get; set; }
    }

    public class GetContactsOutput
    {
        public int TotalCount { get; set; }
        public IList<TblContact> Rows { get; set; }
    }

    public class ContactController(IHttpContextAccessor httpContextAccessor, ILogger logger, IApplicationDbContext ctx, IConfiguration configuration, ComplexReadService complexReadService) : BaseController(httpContextAccessor, logger, ctx, configuration, complexReadService)
    {


        // GET: api/contacts
        [HttpPost("GetContacts")]
        public async Task<ActionResult<GetContactsOutput>> GetContacts([FromBody] GetContactsInput getContactsInput)
        {
            var totalCount = await ctx.TblContacts.CountAsync();
            var rows = await ctx.TblContacts.OrderByDescending(x => x.Id).Skip(getContactsInput.PageIndex * getContactsInput.PageSize).Take(getContactsInput.PageSize).ToListAsync();

            var getContactsOutput = new GetContactsOutput()
            {
                Rows = rows,
                TotalCount = totalCount
            };


            return getContactsOutput;
        }

        // POST: api/contacts
        [HttpPost("PostContact")]
        public async Task<ActionResult<TblContact>> PostContact(TblContact contact)
        {
            ctx.TblContacts.Add(contact);
            await ctx.SaveChangesAsync();

            // Returns a 201 Created status with the new contact
            return CreatedAtAction(nameof(GetContacts), new { id = contact.Id }, contact);
        }
    }
}
