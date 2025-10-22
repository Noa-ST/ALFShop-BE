using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Aplication.DTOs.GlobalCategory
{
    public class UpdateGlobalCategory : GlobalCategoryBase
    {
        [Required ]
        public Guid Id { get; set; }
    }
}
