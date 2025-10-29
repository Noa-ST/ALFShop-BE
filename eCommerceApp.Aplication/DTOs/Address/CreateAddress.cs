using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Address
{
    public class CreateAddress
    {
        [Required] public string RecipientName { get; set; } = null!;
        [Required] public string PhoneNumber { get; set; } = null!;
        [Required] public string Street { get; set; } = null!;
        [Required] public string City { get; set; } = null!;
        [Required] public string Country { get; set; } = "Việt Nam";
        public bool IsDefault { get; set; } = false;
    }
}