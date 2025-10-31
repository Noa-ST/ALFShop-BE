using System;

namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class MessageProductAttachmentDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
        public Guid ShopId { get; set; }
        public string? ShopName { get; set; }
    }
}

