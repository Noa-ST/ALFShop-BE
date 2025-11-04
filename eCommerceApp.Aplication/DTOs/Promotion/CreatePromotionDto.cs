using System;
using System.ComponentModel.DataAnnotations;
using eCommerceApp.Domain.Enums;

// HÃY ĐẢM BẢO NAMESPACE NÀY LÀ CHÍNH XÁC
namespace eCommerceApp.Aplication.DTOs.Promotion
{
	public class CreatePromotionDto
	{
		public Guid? ShopId { get; set; }
		public Guid? ProductId { get; set; }

		[Required]
		[StringLength(50)]
		public string Code { get; set; } = string.Empty;

		[Required]
		public PromotionType PromotionType { get; set; }

		[Range(0.01, double.MaxValue)]
		public decimal DiscountValue { get; set; }

		[Required]
		public DateTime StartDate { get; set; }

		[Required]
		public DateTime EndDate { get; set; }

		[Range(1, int.MaxValue)]
		public int MaxUsageCount { get; set; }

		[Range(0, double.MaxValue)]
		public decimal MinOrderAmount { get; set; } = 0;
	}
}