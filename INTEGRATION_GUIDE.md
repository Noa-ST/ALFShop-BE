# Integration Guide - Product Service Integration

## ✅ Đã tích hợp

### 1. Stock Management trong OrderService

**OrderService.cs** đã được tích hợp:
- ✅ **CreateOrderAsync()**: Tự động giảm stock sau khi tạo order thành công
- ✅ **UpdateStatusAsync()**: Tự động restore stock khi order bị cancel

**Cách hoạt động:**
```csharp
// Khi tạo order thành công
foreach (var item in orderItems)
{
    await _productService.ReduceStockAsync(item.ProductId, item.Quantity);
}

// Khi order bị cancel
if (newStatus == OrderStatus.Canceled)
{
    foreach (var item in order.Items)
    {
        await _productService.RestoreStockAsync(item.ProductId, item.Quantity);
    }
}
```

## 📝 Cần tích hợp khi có ReviewService

### Rating Management Integration

Khi có **ReviewService** hoặc **ReviewController**, cần tích hợp `RecalculateRatingAsync()` vào các điểm sau:

#### 1. Khi Review được Approve
```csharp
// Trong ReviewService hoặc ReviewController
public async Task<ServiceResponse> ApproveReviewAsync(Guid reviewId)
{
    // ... existing code to approve review ...
    
    // ✅ Tính lại rating cho product
    await _productService.RecalculateRatingAsync(review.ProductId);
    
    return ServiceResponse.Success("Review approved successfully.");
}
```

#### 2. Khi Review bị Xóa
```csharp
public async Task<ServiceResponse> DeleteReviewAsync(Guid reviewId)
{
    var review = await _reviewRepo.GetByIdAsync(reviewId);
    if (review == null) return ServiceResponse.Fail("Review not found.");
    
    Guid productId = review.ProductId;
    
    // ... existing code to delete review ...
    
    // ✅ Tính lại rating cho product
    await _productService.RecalculateRatingAsync(productId);
    
    return ServiceResponse.Success("Review deleted successfully.");
}
```

#### 3. Khi Review được Update
```csharp
public async Task<ServiceResponse> UpdateReviewAsync(Guid reviewId, UpdateReviewDto dto)
{
    var review = await _reviewRepo.GetByIdAsync(reviewId);
    if (review == null) return ServiceResponse.Fail("Review not found.");
    
    Guid productId = review.ProductId;
    
    // ... existing code to update review ...
    
    // ✅ Tính lại rating cho product (chỉ khi review đã approved)
    if (review.Status == ReviewStatus.Approved)
    {
        await _productService.RecalculateRatingAsync(productId);
    }
    
    return ServiceResponse.Success("Review updated successfully.");
}
```

## 🔧 Setup Required

### Dependency Injection

Đảm bảo `IProductService` đã được đăng ký trong DI container:

```csharp
// ServiceContainer.cs (đã có sẵn)
services.AddScoped<IProductService, ProductService>();
```

### Inject vào ReviewService

```csharp
public class ReviewService : IReviewService
{
    private readonly IProductService _productService;
    
    public ReviewService(
        // ... other dependencies ...
        IProductService productService)
    {
        _productService = productService;
    }
}
```

## 📌 Lưu ý

1. **Stock Management**: Đã được tích hợp tự động trong OrderService
2. **Rating Management**: Cần tích hợp thủ công khi có ReviewService
3. **Error Handling**: Các methods đã có try-catch để không làm gián đoạn flow chính
4. **Transaction**: Xem xét sử dụng transaction nếu cần đảm bảo consistency nghiêm ngặt

## 🎯 Tóm tắt

- ✅ **Stock Management**: Đã tích hợp vào OrderService
- ⏳ **Rating Management**: Chờ ReviewService để tích hợp
- ✅ **Admin Features**: Đã có sẵn endpoints
- ✅ **Search & Filter**: Đã implement đầy đủ

