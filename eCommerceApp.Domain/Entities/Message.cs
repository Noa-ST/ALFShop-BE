using eCommerceApp.Domain.Entities.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerceApp.Domain.Entities
{
    public class Message
    {
        [Key]
        public Guid MessageId { get; set; }

        public Guid ShopId { get; set; }
        public string CustomerId { get; set; } = null!; // FK -> User.Id

        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(ShopId))]
        public Shop? Shop { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public User? Customer { get; set; }
    }
}
