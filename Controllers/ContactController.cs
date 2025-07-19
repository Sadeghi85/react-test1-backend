

using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IContactDbContext _context;

        public ContactController(IContactDbContext context)
        {
            _context = context;
        }

        // GET: api/contacts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblContact>>> GetContacts()
        {
            return await _context.TblContacts.ToListAsync();
        }

        // POST: api/contacts
        [HttpPost]
        public async Task<ActionResult<TblContact>> PostContact(TblContact contact)
        {
            _context.TblContacts.Add(contact);
            await _context.SaveChangesAsync();

            // Returns a 201 Created status with the new contact
            return CreatedAtAction(nameof(GetContacts), new { id = contact.Id }, contact);
        }
    }
}
