using System.ComponentModel.DataAnnotations;

namespace eCommerceApp.Domain.Entities
{
    public abstract class AuditableEntity
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
