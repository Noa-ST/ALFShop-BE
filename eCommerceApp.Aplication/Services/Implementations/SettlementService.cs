using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Settlement;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Aplication.Services.Interfaces.Logging;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class SettlementService : ISettlementService
    {
        private readonly ISettlementRepository _settlementRepository;
        private readonly ISellerBalanceRepository _sellerBalanceRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IAppLogger<SettlementService> _logger;

        // Configuration constants
        private readonly decimal _platformCommissionPercent;
        private readonly decimal _minSettlementAmount;
        private readonly int _holdPeriodDays;

        public SettlementService(
            ISettlementRepository settlementRepository,
            ISellerBalanceRepository sellerBalanceRepository,
            IOrderRepository orderRepository,
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IAppLogger<SettlementService> logger)
        {
            _settlementRepository = settlementRepository;
            _sellerBalanceRepository = sellerBalanceRepository;
            _orderRepository = orderRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;

            // Load configuration from appsettings.json
            _platformCommissionPercent = _configuration.GetValue<decimal>("Settlement:PlatformCommissionPercent", 5.0m);
            _minSettlementAmount = _configuration.GetValue<decimal>("Settlement:MinSettlementAmount", 100000m);
            _holdPeriodDays = _configuration.GetValue<int>("Settlement:HoldPeriodDays", 3);
        }

        private bool IsAdmin(string? userId = null)
        {
            return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
        }

        public async Task<ServiceResponse<SellerBalanceDto>> GetSellerBalanceAsync(string sellerId)
        {
            try
            {
                if (string.IsNullOrEmpty(sellerId))
                {
                    return ServiceResponse<SellerBalanceDto>.Fail(
                        "SellerId không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                // Lấy shop của seller
                var shops = await _shopRepository.GetBySellerIdAsync(sellerId);
                var shop = shops.FirstOrDefault();
                if (shop == null)
                {
                    return ServiceResponse<SellerBalanceDto>.Fail(
                        "Không tìm thấy shop của seller này.",
                        HttpStatusCode.NotFound);
                }

                // Lấy hoặc tạo balance
                var balance = await _sellerBalanceRepository.GetOrCreateByShopIdAsync(shop.Id, sellerId);
                
                var balanceDto = _mapper.Map<SellerBalanceDto>(balance);
                balanceDto.ShopName = shop.Name;
                balanceDto.SellerName = shop.Seller?.FullName;

                _logger.LogInformation($"Retrieved seller balance: SellerId={sellerId}, ShopId={shop.Id}, AvailableBalance={balance.AvailableBalance}");

                return ServiceResponse<SellerBalanceDto>.Success(balanceDto, "Lấy số dư thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get seller balance: SellerId={sellerId}, Error={ex.Message}");
                return ServiceResponse<SellerBalanceDto>.Fail(
                    $"Lỗi khi lấy số dư: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<SettlementDto>> CreateSettlementRequestAsync(string sellerId, CreateSettlementRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(sellerId))
                {
                    return ServiceResponse<SettlementDto>.Fail(
                        "SellerId không được để trống.",
                        HttpStatusCode.BadRequest);
                }

                // Validate amount
                if (request.Amount < _minSettlementAmount)
                {
                    return ServiceResponse<SettlementDto>.Fail(
                        $"Số tiền giải ngân tối thiểu là {_minSettlementAmount:N0} VND.",
                        HttpStatusCode.BadRequest);
                }

                // Parse settlement method
                if (!Enum.TryParse<SettlementMethod>(request.Method, true, out var settlementMethod))
                {
                    return ServiceResponse<SettlementDto>.Fail(
                        $"Phương thức giải ngân không hợp lệ: {request.Method}",
                        HttpStatusCode.BadRequest);
                }

                // Validate bank info nếu là BankTransfer
                if (settlementMethod == SettlementMethod.BankTransfer)
                {
                    if (string.IsNullOrWhiteSpace(request.BankAccount) ||
                        string.IsNullOrWhiteSpace(request.BankName) ||
                        string.IsNullOrWhiteSpace(request.AccountHolderName))
                    {
                        return ServiceResponse<SettlementDto>.Fail(
                            "Thông tin ngân hàng không đầy đủ. Vui lòng cung cấp số tài khoản, tên ngân hàng và tên chủ tài khoản.",
                            HttpStatusCode.BadRequest);
                    }
                }

                // Lấy shop và balance
                var shops = await _shopRepository.GetBySellerIdAsync(sellerId);
                var shop = shops.FirstOrDefault();
                if (shop == null)
                {
                    return ServiceResponse<SettlementDto>.Fail(
                        "Không tìm thấy shop của seller này.",
                        HttpStatusCode.NotFound);
                }

                var balance = await _sellerBalanceRepository.GetByShopIdAsync(shop.Id);
                if (balance == null)
                {
                    balance = await _sellerBalanceRepository.GetOrCreateByShopIdAsync(shop.Id, sellerId);
                }

                // Validate available balance
                if (balance.AvailableBalance < request.Amount)
                {
                    return ServiceResponse<SettlementDto>.Fail(
                        $"Số dư khả dụng không đủ. Số dư hiện tại: {balance.AvailableBalance:N0} VND.",
                        HttpStatusCode.BadRequest);
                }

                // Lấy các orders chưa được settle (eligible for settlement)
                var eligibleOrders = await _settlementRepository.GetEligibleOrdersForSettlementAsync(shop.Id, _holdPeriodDays);
                var eligibleOrdersList = eligibleOrders.ToList();

                // Tính tổng amount từ các orders (để validate với request.Amount)
                decimal totalEligibleAmount = 0;
                decimal totalCommission = 0;
                var orderSettlements = new List<OrderSettlement>();

                foreach (var eligibleOrder in eligibleOrdersList)
                {
                    // Tính commission cho order này
                    decimal orderCommission = eligibleOrder.TotalAmount * (_platformCommissionPercent / 100);
                    decimal orderSettlementAmount = eligibleOrder.TotalAmount - orderCommission;

                    totalEligibleAmount += orderSettlementAmount;
                    totalCommission += orderCommission;

                    // Tạo OrderSettlement record
                    orderSettlements.Add(new OrderSettlement
                    {
                        Id = Guid.NewGuid(),
                        OrderId = eligibleOrder.Id,
                        SettlementId = Guid.Empty, // Sẽ được set sau
                        OrderAmount = eligibleOrder.TotalAmount,
                        Commission = orderCommission,
                        SettlementAmount = orderSettlementAmount,
                        OrderDeliveredAt = eligibleOrder.UpdatedAt ?? eligibleOrder.CreatedAt,
                        EligibleAt = (eligibleOrder.UpdatedAt ?? eligibleOrder.CreatedAt).AddDays(_holdPeriodDays),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Validate: request.Amount phải <= totalEligibleAmount (seller có thể rút ít hơn)
                if (request.Amount > totalEligibleAmount)
                {
                    return ServiceResponse<SettlementDto>.Fail(
                        $"Số tiền yêu cầu ({request.Amount:N0} VND) vượt quá số tiền có thể giải ngân ({totalEligibleAmount:N0} VND).",
                        HttpStatusCode.BadRequest);
                }

                // Tính platform fee trên tổng amount yêu cầu
                decimal platformFee = request.Amount * (_platformCommissionPercent / 100);
                decimal netAmount = request.Amount - platformFee;

                // Tạo settlement request
                var settlement = new Settlement
                {
                    Id = Guid.NewGuid(),
                    SellerId = sellerId,
                    ShopId = shop.Id,
                    Amount = request.Amount,
                    PlatformFee = platformFee,
                    NetAmount = netAmount,
                    Status = SettlementStatus.Pending,
                    Method = settlementMethod,
                    BankAccount = request.BankAccount,
                    BankName = request.BankName,
                    AccountHolderName = request.AccountHolderName,
                    Notes = request.Notes,
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _settlementRepository.AddAsync(settlement);

                // Set SettlementId cho OrderSettlements và thêm vào database
                // Chọn các orders phù hợp với request.Amount (có thể cần logic phức tạp hơn nếu partial settlement)
                decimal remainingAmount = request.Amount;
                var selectedOrderSettlements = new List<OrderSettlement>();
                
                foreach (var os in orderSettlements.OrderBy(os => os.OrderDeliveredAt)) // Ưu tiên orders cũ hơn
                {
                    if (remainingAmount <= 0) break;
                    
                    os.SettlementId = settlement.Id;
                    selectedOrderSettlements.Add(os);
                    remainingAmount -= os.SettlementAmount;
                }

                await _settlementRepository.AddOrderSettlementsAsync(selectedOrderSettlements);

                // Cập nhật balance: giảm AvailableBalance, tăng TotalPendingWithdrawal
                await _sellerBalanceRepository.UpdateBalanceAsync(
                    shop.Id,
                    availableBalanceChange: -request.Amount,
                    totalPendingWithdrawalChange: request.Amount);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Settlement request created: SettlementId={settlement.Id}, SellerId={sellerId}, Amount={request.Amount}, NetAmount={netAmount}");

                var settlementDto = _mapper.Map<SettlementDto>(settlement);
                settlementDto.ShopName = shop.Name;
                settlementDto.SellerName = shop.Seller?.FullName;

                return ServiceResponse<SettlementDto>.Success(settlementDto, "Tạo yêu cầu giải ngân thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Failed to create settlement request: SellerId={sellerId}, Error={ex.Message}");
                return ServiceResponse<SettlementDto>.Fail(
                    $"Lỗi khi tạo yêu cầu giải ngân: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<PagedResult<SettlementDto>>> GetMySettlementsAsync(string sellerId, SettlementFilterDto? filter = null)
        {
            try
            {
                filter ??= new SettlementFilterDto { Page = 1, PageSize = 20 };
                filter.Validate();
                filter.SellerId = sellerId; // Force filter by seller

                var (settlements, totalCount) = await _settlementRepository.GetSettlementsAsync(
                    sellerId: filter.SellerId,
                    shopId: filter.ShopId,
                    status: string.IsNullOrEmpty(filter.Status) ? null : Enum.Parse<SettlementStatus>(filter.Status, true),
                    startDate: filter.StartDate,
                    endDate: filter.EndDate,
                    page: filter.Page,
                    pageSize: filter.PageSize);

                var settlementDtos = _mapper.Map<List<SettlementDto>>(settlements);
                
                // Map shop and seller names
                foreach (var dto in settlementDtos)
                {
                    var settlement = settlements.FirstOrDefault(s => s.Id == dto.Id);
                    if (settlement != null)
                    {
                        dto.ShopName = settlement.Shop?.Name;
                        dto.SellerName = settlement.Seller?.FullName;
                    }
                }

                var result = new PagedResult<SettlementDto>
                {
                    Data = settlementDtos,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

                return ServiceResponse<PagedResult<SettlementDto>>.Success(result, "Lấy danh sách giải ngân thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get seller settlements: SellerId={sellerId}, Error={ex.Message}");
                return ServiceResponse<PagedResult<SettlementDto>>.Fail(
                    $"Lỗi khi lấy danh sách giải ngân: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<List<SettlementDto>>> GetPendingSettlementsAsync()
        {
            try
            {
                var settlements = await _settlementRepository.GetPendingSettlementsAsync();
                var settlementDtos = _mapper.Map<List<SettlementDto>>(settlements);

                // Map shop and seller names
                foreach (var dto in settlementDtos)
                {
                    var settlement = settlements.FirstOrDefault(s => s.Id == dto.Id);
                    if (settlement != null)
                    {
                        dto.ShopName = settlement.Shop?.Name;
                        dto.SellerName = settlement.Seller?.FullName;
                    }
                }

                return ServiceResponse<List<SettlementDto>>.Success(settlementDtos, "Lấy danh sách pending settlements thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get pending settlements: Error={ex.Message}");
                return ServiceResponse<List<SettlementDto>>.Fail(
                    $"Lỗi khi lấy danh sách pending settlements: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> ApproveSettlementAsync(Guid settlementId, string adminId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!IsAdmin(adminId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ Admin mới có thể approve settlement.",
                        HttpStatusCode.Forbidden);
                }

                var settlement = await _settlementRepository.GetByIdAsync(settlementId);
                if (settlement == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy settlement.",
                        HttpStatusCode.NotFound);
                }

                if (settlement.Status != SettlementStatus.Pending)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể approve settlement ở trạng thái {settlement.Status}. Chỉ có thể approve settlements ở trạng thái Pending.",
                        HttpStatusCode.BadRequest);
                }

                // Update status
                settlement.Status = SettlementStatus.Approved;
                settlement.ProcessedBy = adminId;
                settlement.ApprovedAt = DateTime.UtcNow;
                settlement.UpdatedAt = DateTime.UtcNow;

                await _settlementRepository.UpdateAsync(settlement);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Settlement approved: SettlementId={settlementId}, AdminId={adminId}, Amount={settlement.NetAmount}");

                return ServiceResponse<bool>.Success(true, "Approve settlement thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Failed to approve settlement: SettlementId={settlementId}, AdminId={adminId}, Error={ex.Message}");
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi approve settlement: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> ProcessPayoutAsync(Guid settlementId, string adminId, ProcessSettlementRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!IsAdmin(adminId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ Admin mới có thể process payout.",
                        HttpStatusCode.Forbidden);
                }

                var settlement = await _settlementRepository.GetByIdAsync(settlementId);
                if (settlement == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy settlement.",
                        HttpStatusCode.NotFound);
                }

                if (settlement.Status != SettlementStatus.Approved)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể process payout cho settlement ở trạng thái {settlement.Status}. Settlement phải ở trạng thái Approved.",
                        HttpStatusCode.BadRequest);
                }

                // TODO: Gọi PayOS API để thực hiện transfer (nếu có API)
                // Hiện tại chỉ update status và transaction reference
                // Trong tương lai cần tích hợp với PayOS Transfer API

                settlement.Status = SettlementStatus.Processing;
                settlement.TransactionReference = request.TransactionReference;
                settlement.Notes = request.Notes;
                settlement.ProcessedBy = adminId;
                settlement.ProcessedAt = DateTime.UtcNow;
                settlement.UpdatedAt = DateTime.UtcNow;

                await _settlementRepository.UpdateAsync(settlement);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Payout processing started: SettlementId={settlementId}, AdminId={adminId}, TransactionReference={request.TransactionReference}");

                // TODO: Sau khi PayOS confirm, update status = Completed qua webhook hoặc manual

                return ServiceResponse<bool>.Success(true, "Bắt đầu xử lý giải ngân. Vui lòng cập nhật trạng thái sau khi PayOS confirm.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Failed to process payout: SettlementId={settlementId}, AdminId={adminId}, Error={ex.Message}");
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi xử lý giải ngân: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> RejectSettlementAsync(Guid settlementId, string adminId, string reason)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!IsAdmin(adminId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ Admin mới có thể reject settlement.",
                        HttpStatusCode.Forbidden);
                }

                var settlement = await _settlementRepository.GetByIdAsync(settlementId);
                if (settlement == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy settlement.",
                        HttpStatusCode.NotFound);
                }

                if (settlement.Status != SettlementStatus.Pending && settlement.Status != SettlementStatus.Approved)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể reject settlement ở trạng thái {settlement.Status}.",
                        HttpStatusCode.BadRequest);
                }

                // Update status
                settlement.Status = SettlementStatus.Cancelled;
                settlement.ProcessedBy = adminId;
                settlement.FailureReason = reason;
                settlement.UpdatedAt = DateTime.UtcNow;

                await _settlementRepository.UpdateAsync(settlement);

                // Hoàn lại AvailableBalance và giảm TotalPendingWithdrawal
                await _sellerBalanceRepository.UpdateBalanceAsync(
                    settlement.ShopId,
                    availableBalanceChange: settlement.Amount,
                    totalPendingWithdrawalChange: -settlement.Amount);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Settlement rejected: SettlementId={settlementId}, AdminId={adminId}, Reason={reason}");

                return ServiceResponse<bool>.Success(true, "Reject settlement thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Failed to reject settlement: SettlementId={settlementId}, AdminId={adminId}, Error={ex.Message}");
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi reject settlement: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<PagedResult<SettlementDto>>> GetAllSettlementsAsync(SettlementFilterDto? filter = null)
        {
            try
            {
                filter ??= new SettlementFilterDto { Page = 1, PageSize = 20 };
                filter.Validate();

                var (settlements, totalCount) = await _settlementRepository.GetSettlementsAsync(
                    sellerId: filter.SellerId,
                    shopId: filter.ShopId,
                    status: string.IsNullOrEmpty(filter.Status) ? null : Enum.Parse<SettlementStatus>(filter.Status, true),
                    startDate: filter.StartDate,
                    endDate: filter.EndDate,
                    page: filter.Page,
                    pageSize: filter.PageSize);

                var settlementDtos = _mapper.Map<List<SettlementDto>>(settlements);
                
                // Map shop and seller names
                foreach (var dto in settlementDtos)
                {
                    var settlement = settlements.FirstOrDefault(s => s.Id == dto.Id);
                    if (settlement != null)
                    {
                        dto.ShopName = settlement.Shop?.Name;
                        dto.SellerName = settlement.Seller?.FullName;
                    }
                }

                var result = new PagedResult<SettlementDto>
                {
                    Data = settlementDtos,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount
                };

                return ServiceResponse<PagedResult<SettlementDto>>.Success(result, "Lấy danh sách settlements thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get all settlements: Error={ex.Message}");
                return ServiceResponse<PagedResult<SettlementDto>>.Fail(
                    $"Lỗi khi lấy danh sách settlements: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse> CalculateSettlementForOrderAsync(Guid orderId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return ServiceResponse.Fail(
                        "Không tìm thấy order.",
                        HttpStatusCode.NotFound);
                }

                // Chỉ tính settlement cho orders đã Delivered và Paid
                if (order.Status != OrderStatus.Delivered || order.PaymentStatus != PaymentStatus.Paid)
                {
                    return ServiceResponse.Fail(
                        "Chỉ có thể tính settlement cho orders đã Delivered và Paid.",
                        HttpStatusCode.BadRequest);
                }

                // Kiểm tra order đã được settle chưa
                var existingOrderSettlement = await _settlementRepository.GetByOrderIdAsync(orderId);
                if (existingOrderSettlement != null)
                {
                    return ServiceResponse.Fail(
                        "Order này đã được settle.",
                        HttpStatusCode.BadRequest);
                }

                // Lấy shop
                var shop = await _shopRepository.GetByIdAsync(order.ShopId);
                if (shop == null)
                {
                    return ServiceResponse.Fail(
                        "Không tìm thấy shop.",
                        HttpStatusCode.NotFound);
                }

                // Lấy hoặc tạo balance
                var balance = await _sellerBalanceRepository.GetOrCreateByShopIdAsync(shop.Id, shop.SellerId);

                // Tính commission và settlement amount
                decimal commission = order.TotalAmount * (_platformCommissionPercent / 100);
                decimal settlementAmount = order.TotalAmount - commission;

                // Note: OrderSettlement sẽ được tạo khi seller request withdrawal
                // Hiện tại chỉ cập nhật balance

                // Cập nhật balance
                // Chuyển từ PendingBalance sang AvailableBalance sau hold period
                var orderDeliveredAt = order.UpdatedAt ?? order.CreatedAt; // Use UpdatedAt hoặc CreatedAt
                var holdPeriodEnd = orderDeliveredAt.AddDays(_holdPeriodDays);

                if (DateTime.UtcNow >= holdPeriodEnd)
                {
                    // Đã đủ hold period -> AvailableBalance
                    await _sellerBalanceRepository.UpdateBalanceAsync(
                        shop.Id,
                        availableBalanceChange: settlementAmount,
                        totalEarnedChange: settlementAmount);
                }
                else
                {
                    // Chưa đủ hold period -> PendingBalance
                    await _sellerBalanceRepository.UpdateBalanceAsync(
                        shop.Id,
                        pendingBalanceChange: settlementAmount,
                        totalEarnedChange: settlementAmount);
                }

                _logger.LogInformation($"Settlement calculated for order: OrderId={orderId}, Commission={commission}, SettlementAmount={settlementAmount}");

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResponse.Success("Tính settlement cho order thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Failed to calculate settlement for order: OrderId={orderId}, Error={ex.Message}");
                return ServiceResponse.Fail(
                    $"Lỗi khi tính settlement: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<object>> GetSettlementStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var settlementsByStatus = await _settlementRepository.GetSettlementsByStatusAsync();
                var totalSettledAmount = await _settlementRepository.GetTotalSettledAmountAsync(startDate, endDate);

                var statistics = new
                {
                    SettlementsByStatus = settlementsByStatus,
                    TotalSettledAmount = totalSettledAmount,
                    DateRange = new
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    }
                };

                return ServiceResponse<object>.Success(statistics, "Lấy thống kê settlement thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get settlement statistics: Error={ex.Message}");
                return ServiceResponse<object>.Fail(
                    $"Lỗi khi lấy thống kê: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResponse<bool>> CompleteSettlementAsync(Guid settlementId, string adminId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (!IsAdmin(adminId))
                {
                    return ServiceResponse<bool>.Fail(
                        "Chỉ Admin mới có thể complete settlement.",
                        HttpStatusCode.Forbidden);
                }

                var settlement = await _settlementRepository.GetByIdAsync(settlementId);
                if (settlement == null)
                {
                    return ServiceResponse<bool>.Fail(
                        "Không tìm thấy settlement.",
                        HttpStatusCode.NotFound);
                }

                if (settlement.Status != SettlementStatus.Processing)
                {
                    return ServiceResponse<bool>.Fail(
                        $"Không thể complete settlement ở trạng thái {settlement.Status}. Settlement phải ở trạng thái Processing.",
                        HttpStatusCode.BadRequest);
                }

                // Update status to Completed
                settlement.Status = SettlementStatus.Completed;
                settlement.ProcessedBy = adminId;
                settlement.CompletedAt = DateTime.UtcNow;
                settlement.UpdatedAt = DateTime.UtcNow;

                await _settlementRepository.UpdateAsync(settlement);

                // Cập nhật balance: giảm TotalPendingWithdrawal, tăng TotalWithdrawn
                await _sellerBalanceRepository.UpdateBalanceAsync(
                    settlement.ShopId,
                    totalPendingWithdrawalChange: -settlement.Amount,
                    totalWithdrawnChange: settlement.Amount);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation($"Settlement completed: SettlementId={settlementId}, AdminId={adminId}, Amount={settlement.NetAmount}");

                return ServiceResponse<bool>.Success(true, "Complete settlement thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, $"Failed to complete settlement: SettlementId={settlementId}, AdminId={adminId}, Error={ex.Message}");
                return ServiceResponse<bool>.Fail(
                    $"Lỗi khi complete settlement: {ex.Message}",
                    HttpStatusCode.InternalServerError);
            }
        }
    }
}

