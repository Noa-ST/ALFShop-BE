using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.Category
{
    public class UpdateCategory : CategoryBase
    {
        [Required ]
        public Guid Id { get; set; }
    }
}
