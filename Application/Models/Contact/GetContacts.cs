using Application.Models.Common;

namespace Application.Models.Contact
{


    public class GetContactsInput
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public Sorting[] Sorting { get; set; }
    }

    public class Contact
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

    }

}
