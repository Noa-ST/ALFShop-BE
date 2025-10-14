﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerceApp.Domain.Enums;

namespace eCommerceApp.Domain.Entities
{
    public class Payment
    {
        // Guid là Primary Key của Payment
        [Key]
        public Guid PaymentId { get; set; }

        // OrderId vừa là Foreign Key, vừa là Unique Key để đảm bảo 1-1
        public Guid OrderId { get; set; }

        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? TransactionId { get; set; } // ID giao dịch từ cổng thanh toán (VNPAY, Momo...)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Cần cập nhật khi trạng thái thay đổi

        // Navigation
        [ForeignKey(nameof(OrderId))]
        public Order? Order { get; set; }
    }
}