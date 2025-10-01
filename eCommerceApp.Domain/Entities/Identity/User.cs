using eCommerceApp.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace eCommerceApp.Domain.Entities.Identity
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Customer; // maps to role enum column
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Navigation properties
        public ICollection<Shop>? Shops { get; set; } // if a user can have shops (seller)
        public ICollection<Cart>? Carts { get; set; }
        public ICollection<Address>? Addresses { get; set; }
        public ICollection<Order>? Orders { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Message>? Messages { get; set; }
        public ICollection<ViolationReport>? ViolationReports { get; set; }
        public ICollection<RefreshToken>? RefreshTokens { get; set; }
    }
}
