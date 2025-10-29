using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Address
{
    public class UpdateAddress : CreateAddress
    {
        [Required] public Guid Id { get; set; }
    }
}