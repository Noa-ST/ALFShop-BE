using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Cart;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(ICartRepository cartRepository, IProductRepository productRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        // --- Hàm tiện ích: Lấy Cart hoặc tạo mới ---
        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart { CustomerId = userId, CreatedAt = DateTime.UtcNow, IsDeleted = false, Items = new List<CartItem>() };
                await _cartRepository.AddAsync(cart); // Lưu Cart mới vào DB
            }
            return cart;
        }

        // ✅ POST /api/Cart/add
        public async Task<ServiceResponse> AddItemToCartAsync(string userId, AddCartItem dto)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            // ✅ Validate quantity
            if (dto.Quantity <= 0)
            {
                return ServiceResponse.Fail("Số lượng phải lớn hơn 0.", HttpStatusCode.BadRequest);
            }

            // ✅ New: Max quantity validation (ví dụ: max 999 per item)
            const int MAX_QUANTITY_PER_ITEM = 999;
            if (dto.Quantity > MAX_QUANTITY_PER_ITEM)
            {
                return ServiceResponse.Fail($"Số lượng tối đa mỗi sản phẩm là {MAX_QUANTITY_PER_ITEM}.", HttpStatusCode.BadRequest);
            }

            // 1. Lấy Cart hoặc tạo mới
            var cart = await GetOrCreateCartAsync(userId);

            // 2. Lấy thông tin Product (cần chi tiết để kiểm tra giá và stock)
            var product = await _productRepository.GetDetailByIdAsync(dto.ProductId);

            if (product == null || product.IsDeleted)
            {
                return ServiceResponse.Fail("Sản phẩm không tồn tại.", HttpStatusCode.NotFound);
            }
            
            if (product.Status != ProductStatus.Approved)
            {
                return ServiceResponse.Fail("Sản phẩm chưa được phê duyệt hoặc không còn bán.", HttpStatusCode.BadRequest);
            }

            // 3. Kiểm tra số lượng tồn kho (Stock)
            var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == dto.ProductId);
            int totalQuantity = dto.Quantity + (existingItem?.Quantity ?? 0);

            if (totalQuantity > product.StockQuantity)
            {
                return ServiceResponse.Fail($"Số lượng tồn kho chỉ còn {product.StockQuantity}.", HttpStatusCode.BadRequest);
            }

            // ✅ New: Check max quantity after adding
            if (totalQuantity > MAX_QUANTITY_PER_ITEM)
            {
                return ServiceResponse.Fail($"Số lượng tối đa mỗi sản phẩm là {MAX_QUANTITY_PER_ITEM}. Hiện tại bạn đã có {existingItem?.Quantity ?? 0} trong giỏ.", HttpStatusCode.BadRequest);
            }

            // 4. Xử lý CartItem
            if (existingItem != null)
            {
                // Cập nhật số lượng
                existingItem.Quantity = totalQuantity;
            }
            else
            {
                // Thêm CartItem mới
                var newItem = new CartItem { CartId = cart.Id, ProductId = dto.ProductId, Quantity = dto.Quantity };
                // Do Cart là Root Aggregate, ta thêm trực tiếp vào Items
                if (cart.Items == null) cart.Items = new List<CartItem>();
                cart.Items.Add(newItem);
            }

            await _cartRepository.UpdateAsync(cart);
            return ServiceResponse.Success("Thêm sản phẩm vào giỏ hàng thành công.");
        }

        // ✅ PUT /api/Cart/update
        public async Task<ServiceResponse> UpdateCartItemQuantityAsync(string userId, UpdateCartItem dto)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                return ServiceResponse.Fail("Không tìm thấy giỏ hàng.", HttpStatusCode.NotFound);
            }

            // Lấy thông tin Product
            var product = await _productRepository.GetDetailByIdAsync(dto.ProductId);
            if (product == null || product.IsDeleted)
            {
                return ServiceResponse.Fail("Sản phẩm không tồn tại.", HttpStatusCode.NotFound);
            }

            // ✅ New: Check Product Status
            if (product.Status != ProductStatus.Approved)
            {
                return ServiceResponse.Fail("Sản phẩm chưa được phê duyệt hoặc không còn bán.", HttpStatusCode.BadRequest);
            }

            var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == dto.ProductId);
            if (existingItem == null)
            {
                return ServiceResponse.Fail("Mặt hàng không có trong giỏ hàng.", HttpStatusCode.NotFound);
            }

            // ✅ New: Max quantity validation
            const int MAX_QUANTITY_PER_ITEM = 999;
            if (dto.Quantity > MAX_QUANTITY_PER_ITEM)
            {
                return ServiceResponse.Fail($"Số lượng tối đa mỗi sản phẩm là {MAX_QUANTITY_PER_ITEM}.", HttpStatusCode.BadRequest);
            }

            // Kiểm tra Stock
            if (dto.Quantity > product.StockQuantity)
            {
                return ServiceResponse.Fail($"Số lượng tồn kho chỉ còn {product.StockQuantity}.", HttpStatusCode.BadRequest);
            }

            if (dto.Quantity <= 0)
            {
                // ✅ Xóa mặt hàng nếu số lượng <= 0 - sử dụng explicit delete
                await _cartRepository.RemoveCartItemAsync(cart.Id, dto.ProductId);
                return ServiceResponse.Success("Đã xóa mặt hàng khỏi giỏ hàng.");
            }
            else
            {
                existingItem.Quantity = dto.Quantity;
                await _cartRepository.UpdateAsync(cart);
                return ServiceResponse.Success("Cập nhật giỏ hàng thành công.");
            }
        }

        // ✅ DELETE /api/Cart/deleteItem/{productId}
        public async Task<ServiceResponse> RemoveItemFromCartAsync(string userId, Guid productId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                return ServiceResponse.Fail("Không tìm thấy giỏ hàng.", HttpStatusCode.NotFound);
            }

            var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem == null)
            {
                return ServiceResponse.Fail("Mặt hàng không có trong giỏ hàng.", HttpStatusCode.NotFound);
            }

            // ✅ Fix: Explicit delete CartItem
            int result = await _cartRepository.RemoveCartItemAsync(cart.Id, productId);
            
            if (result > 0)
            {
                return ServiceResponse.Success("Xóa mặt hàng khỏi giỏ hàng thành công.");
            }
            else
            {
                return ServiceResponse.Fail("Không thể xóa mặt hàng khỏi giỏ hàng.", HttpStatusCode.InternalServerError);
            }
        }

        // ✅ GET /api/Cart
        public async Task<ServiceResponse<GetCartDto>> GetCurrentCartAsync(string userId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse<GetCartDto>.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            // Nếu không có cart, trả về giỏ hàng rỗng
            if (cart == null)
            {
                var emptyCartDto = new GetCartDto 
                { 
                    CartId = Guid.Empty, // ✅ Fix: Set CartId
                    CustomerId = userId, 
                    SubTotal = 0,
                    Items = new List<GetCartItemDto>()
                };
                return ServiceResponse<GetCartDto>.Success(emptyCartDto);
            }

            // 1. Ánh xạ cơ bản
            var cartDto = _mapper.Map<GetCartDto>(cart);

            // 2. Tính SubTotal và ánh xạ chi tiết CartItems
            cartDto.SubTotal = 0;
            cartDto.Items = new List<GetCartItemDto>();

            // ✅ New: Filter invalid products (IsDeleted, Status != Approved)
            var validItems = cart.Items?
                .Where(item => item.Product != null 
                    && !item.Product.IsDeleted 
                    && item.Product.Status == ProductStatus.Approved)
                .ToList() ?? new List<CartItem>();

            foreach (var item in validItems)
            {
                // Product Navigation Property đã được tải từ Repository
                var product = item.Product;

                // Lấy giá hiện tại (cần check Promotion/Sale Price nếu có)
                decimal unitPrice = product?.Price ?? 0;
                decimal itemTotal = unitPrice * item.Quantity;

                // Ánh xạ CartItem chi tiết
                var itemDto = _mapper.Map<GetCartItemDto>(item);
                itemDto.ProductName = product?.Name ?? "Sản phẩm đã bị xóa";
                itemDto.ShopName = product?.Shop?.Name ?? "N/A";
                itemDto.UnitPrice = unitPrice;
                itemDto.ItemTotal = itemTotal;
                
                // ✅ Convert relative URL thành full URL cho ảnh trong cart
                var relativeImageUrl = product?.Images?.FirstOrDefault(i => !i.IsDeleted)?.Url;
                if (!string.IsNullOrEmpty(relativeImageUrl))
                {
                    if (relativeImageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                        relativeImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        itemDto.ImageUrl = relativeImageUrl;
                    }
                    else
                    {
                        // Lấy base URL từ HttpContext nếu có, nếu không dùng fallback
                        var request = _httpContextAccessor.HttpContext?.Request;
                        if (request != null)
                        {
                            var baseUrl = $"{request.Scheme}://{request.Host}";
                            itemDto.ImageUrl = $"{baseUrl}{relativeImageUrl}";
                        }
                        else
                        {
                            itemDto.ImageUrl = $"https://localhost:7109{relativeImageUrl}";
                        }
                    }
                }
                else
                {
                    itemDto.ImageUrl = null;
                }

                cartDto.Items.Add(itemDto);
                cartDto.SubTotal += itemTotal;
            }

            // ✅ New: Auto-remove invalid items from cart (cleanup)
            var invalidItems = cart.Items?
                .Where(item => item.Product == null 
                    || item.Product.IsDeleted 
                    || item.Product.Status != ProductStatus.Approved)
                .ToList();

            if (invalidItems != null && invalidItems.Any())
            {
                foreach (var invalidItem in invalidItems)
                {
                    await _cartRepository.RemoveCartItemAsync(cart.Id, invalidItem.ProductId);
                }
            }

            return ServiceResponse<GetCartDto>.Success(cartDto);
        }

        // ✅ New: Clear Cart
        public async Task<ServiceResponse> ClearCartAsync(string userId)
        {
            // ✅ Validate userId
            if (string.IsNullOrEmpty(userId))
            {
                return ServiceResponse.Fail("Không thể xác định người dùng.", HttpStatusCode.Unauthorized);
            }

            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                return ServiceResponse.Fail("Không tìm thấy giỏ hàng.", HttpStatusCode.NotFound);
            }

            int result = await _cartRepository.ClearCartItemsAsync(cart.Id);
            
            if (result >= 0) // >= 0 vì có thể cart đã rỗng
            {
                return ServiceResponse.Success("Đã xóa tất cả mặt hàng khỏi giỏ hàng.");
            }
            else
            {
                return ServiceResponse.Fail("Không thể xóa giỏ hàng.", HttpStatusCode.InternalServerError);
            }
        }
    }
}