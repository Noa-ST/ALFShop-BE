using eCommerceApp.Domain.Enums;
using System;

namespace eCommerceApp.Aplication.DTOs.Promotion
{
	public class PromotionDto
	{
		public Guid Id { get; set; }
		public Guid? ShopId { get; set; }
		public Guid? ProductId { get; set; }
		public string Code { get; set; } = string.Empty;
		public string PromotionType { get; set; } 
		public decimal DiscountValue { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public bool IsActive { get; set; }
		public int MaxUsageCount { get; set; }
		public int CurrentUsageCount { get; set; }
		public decimal MinOrderAmount { get; set; }
	}
}