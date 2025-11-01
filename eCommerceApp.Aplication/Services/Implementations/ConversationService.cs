using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Chat;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Identity;
using eCommerceApp.Domain.Entities.Identity;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ConversationService : IConversationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatRealtimeNotifier _chatNotifier;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager; // ✅ New: For user validation

        // Convenience properties để truy cập repositories từ UnitOfWork
        private IConversationRepository Conversations => _unitOfWork.Conversations;
        private IMessageRepository Messages => _unitOfWork.Messages;
        private IOrderRepository Orders => _unitOfWork.Orders;
        private IProductRepository Products => _unitOfWork.Products;

        public ConversationService(
            IUnitOfWork unitOfWork,
            IChatRealtimeNotifier chatNotifier,
            IMapper mapper,
            UserManager<User> userManager) // ✅ New: Inject UserManager
        {
            _unitOfWork = unitOfWork;
            _chatNotifier = chatNotifier;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<ServiceResponse<ConversationSummaryDto>> CreateConversationAsync(string currentUserId, CreateConversationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TargetUserId))
            {
                return ServiceResponse<ConversationSummaryDto>.Fail("Target user is required.");
            }

            if (request.TargetUserId == currentUserId)
            {
                return ServiceResponse<ConversationSummaryDto>.Fail("Cannot create conversation with yourself.");
            }

            // ✅ New: Validate TargetUserId exists
            var targetUser = await _userManager.FindByIdAsync(request.TargetUserId);
            if (targetUser == null || targetUser.IsDeleted)
            {
                return ServiceResponse<ConversationSummaryDto>.Fail("Target user not found.");
            }

            var existing = await Conversations.GetBetweenUsersAsync(currentUserId, request.TargetUserId);
            if (existing != null)
            {
                var summary = MapConversationSummary(existing, currentUserId);
                return ServiceResponse<ConversationSummaryDto>.Success(summary, "Conversation already exists.");
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                User1Id = currentUserId,
                User2Id = request.TargetUserId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow,
            };

            await Conversations.AddAsync(conversation);
            await _unitOfWork.SaveChangesAsync();

            var dto = MapConversationSummary(conversation, currentUserId);
            return ServiceResponse<ConversationSummaryDto>.Success(dto, "Conversation created successfully.");
        }

        public async Task<ServiceResponse<ConversationDetailDto>> GetConversationAsync(string currentUserId, Guid conversationId, int messagePage = 1, int pageSize = 50)
        {
            // ✅ Fix: Validate page >= 1
            if (messagePage < 1) messagePage = 1;

            var conversation = await Conversations.GetByIdAsync(conversationId);
            if (conversation == null || conversation.IsDeleted)
            {
                return ServiceResponse<ConversationDetailDto>.Fail("Conversation not found.");
            }

            if (!IsParticipant(conversation, currentUserId))
            {
                return ServiceResponse<ConversationDetailDto>.Fail("You are not a participant of this conversation.");
            }

            pageSize = NormalizePageSize(pageSize);
            var skip = Math.Max(0, (messagePage - 1) * pageSize);

            var messages = await Messages.GetMessagesAsync(conversationId, skip, pageSize);
            var messageDtos = MapMessages(messages);

            var detail = _mapper.Map<ConversationDetailDto>(conversation);
            detail.Messages = messageDtos;

            return ServiceResponse<ConversationDetailDto>.Success(detail);
        }

        public async Task<ServiceResponse<PagedResult<ConversationSummaryDto>>> GetConversationsAsync(string currentUserId, int page = 1, int pageSize = 20)
        {
            // ✅ Fix: Validate page >= 1
            if (page < 1) page = 1;
            pageSize = NormalizePageSize(pageSize);
            var skip = Math.Max(0, (page - 1) * pageSize);

            var conversations = await Conversations.GetUserConversationsAsync(currentUserId, skip, pageSize);
            var totalCount = await Conversations.GetUserConversationsCountAsync(currentUserId);

            var results = conversations
                .Select(conversation => MapConversationSummary(conversation, currentUserId))
                .ToList();

            var pagedResult = new PagedResult<ConversationSummaryDto>
            {
                Data = results,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ServiceResponse<PagedResult<ConversationSummaryDto>>.Success(pagedResult);
        }

        public async Task<ServiceResponse<bool>> MarkMessagesReadAsync(string currentUserId, Guid conversationId, MarkMessagesReadRequest request)
        {
            // ✅ Fix: Wrap trong transaction để tránh race condition
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var conversation = await Conversations.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("Conversation not found.");
                }

                if (!IsParticipant(conversation, currentUserId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("You are not a participant of this conversation.");
                }

                var upTo = request.UpTo ?? DateTime.UtcNow;
                var unreadMessages = await Messages.GetUnreadMessagesAsync(conversationId, currentUserId, upTo);

                if (unreadMessages.Count == 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Success(true, "No unread messages to update.");
                }

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.UtcNow;
                    message.UpdatedAt = DateTime.UtcNow;
                }

                // ✅ Fix: Re-calculate unread count để tránh race condition
                var isUser1 = conversation.User1Id == currentUserId;
                var actualUnreadCount = await Messages.CountUnreadAsync(conversationId, currentUserId);
                
                if (isUser1)
                {
                    conversation.User1UnreadCount = actualUnreadCount;
                }
                else
                {
                    conversation.User2UnreadCount = actualUnreadCount;
                }

                conversation.UpdatedAt = DateTime.UtcNow;
                await Conversations.UpdateAsync(conversation);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _chatNotifier.BroadcastMessagesReadAsync(
                    conversationId,
                    currentUserId,
                    conversation.User1UnreadCount,
                    conversation.User2UnreadCount,
                    unreadMessages.Select(m => m.Id).ToList());

                return ServiceResponse<bool>.Success(true, "Marked messages as read.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<bool>.Fail($"Error marking messages as read: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<MessageDto>> SendMessageAsync(string currentUserId, Guid conversationId, SendMessageRequest request)
        {
            // ✅ Fix: Wrap trong transaction để tránh race condition
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // ✅ New: Validate AttachmentUrl (file size, type, URL format)
                if (!string.IsNullOrWhiteSpace(request.AttachmentUrl))
                {
                    if (!Uri.TryCreate(request.AttachmentUrl, UriKind.Absolute, out var uri))
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<MessageDto>.Fail("Invalid attachment URL format.");
                    }

                    // Check URL scheme (http/https only)
                    if (uri.Scheme != "http" && uri.Scheme != "https")
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<MessageDto>.Fail("Attachment URL must use http or https protocol.");
                    }

                    // Note: File size validation should be done at upload time, not here
                    // Metadata có thể chứa file size info để validate
                }

                if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("Message content or attachment is required.");
                }

                var conversation = await Conversations.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("Conversation not found.");
                }

                if (!IsParticipant(conversation, currentUserId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("You are not a participant of this conversation.");
                }

                if (conversation.IsBlocked && conversation.BlockedByUserId != currentUserId)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("Conversation is blocked.");
                }

            Message? replyTarget = null;
            if (request.ReplyToMessageId.HasValue)
            {
                replyTarget = await Messages.GetByIdAsync(request.ReplyToMessageId.Value);
                if (replyTarget == null || replyTarget.ConversationId != conversationId)
                {
                    return ServiceResponse<MessageDto>.Fail("Invalid reply target.");
                }
            }

            var now = DateTime.UtcNow;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = currentUserId,
                Content = request.Content?.Trim() ?? string.Empty,
                Type = request.Type,
                AttachmentUrl = request.AttachmentUrl,
                Metadata = request.Metadata,
                ReplyToMessageId = request.ReplyToMessageId,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (string.IsNullOrWhiteSpace(message.Content) && string.IsNullOrWhiteSpace(message.AttachmentUrl))
            {
                return ServiceResponse<MessageDto>.Fail("Message cannot be empty.");
            }

            if (replyTarget != null)
            {
                message.ReplyToMessage = replyTarget;
            }

            if (request.OrderId.HasValue)
            {
                var order = await Orders.GetByIdAsync(request.OrderId.Value);
                if (order == null)
                {
                    return ServiceResponse<MessageDto>.Fail("Order not found.");
                }

                if (order.CustomerId != conversation.User1Id && order.CustomerId != conversation.User2Id)
                {
                    return ServiceResponse<MessageDto>.Fail("Order is not related to this conversation.");
                }

                message.OrderId = order.Id;
                message.Order = order;
            }

            if (request.ProductId.HasValue)
            {
                var product = await Products.GetDetailByIdAsync(request.ProductId.Value);
                if (product == null)
                {
                    return ServiceResponse<MessageDto>.Fail("Product not found.");
                }

                var sellerId = product.Shop?.SellerId;
                if (!string.IsNullOrEmpty(sellerId) && sellerId != conversation.User1Id && sellerId != conversation.User2Id)
                {
                    return ServiceResponse<MessageDto>.Fail("Product is not related to this conversation.");
                }

                message.ProductId = product.Id;
                message.Product = product;

                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    message.Content = product.Name;
                }
            }

                await Messages.AddAsync(message);

                UpdateConversationOnNewMessage(conversation, message, currentUserId);
                await Conversations.UpdateAsync(conversation);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var messageDto = MapMessage(message);

                var senderSummary = MapConversationSummary(conversation, currentUserId);
                var receiverId = conversation.User1Id == currentUserId ? conversation.User2Id : conversation.User1Id;
                var receiverSummary = MapConversationSummary(conversation, receiverId);

                await _chatNotifier.BroadcastMessageAsync(messageDto, senderSummary, receiverSummary);

                return ServiceResponse<MessageDto>.Success(messageDto, "Message sent successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<MessageDto>.Fail($"Error sending message: {ex.Message}");
            }
        }

        public async Task<ServiceResponse<bool>> UpdatePreferencesAsync(string currentUserId, Guid conversationId, UpdateConversationPreferenceRequest request)
        {
            var conversation = await Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                return ServiceResponse<bool>.Fail("Conversation not found.");
            }

            if (!IsParticipant(conversation, currentUserId))
            {
                return ServiceResponse<bool>.Fail("You are not a participant of this conversation.");
            }

            var isUser1 = conversation.User1Id == currentUserId;

            if (request.IsArchived.HasValue)
            {
                if (isUser1)
                    conversation.IsArchivedByUser1 = request.IsArchived.Value;
                else
                    conversation.IsArchivedByUser2 = request.IsArchived.Value;
            }

            if (request.IsMuted.HasValue)
            {
                if (isUser1)
                    conversation.IsMutedByUser1 = request.IsMuted.Value;
                else
                    conversation.IsMutedByUser2 = request.IsMuted.Value;
            }

            // ✅ New: Pin conversation
            if (request.IsPinned.HasValue)
            {
                if (isUser1)
                    conversation.IsPinnedByUser1 = request.IsPinned.Value;
                else
                    conversation.IsPinnedByUser2 = request.IsPinned.Value;
            }

            if (request.IsBlocked.HasValue && request.IsBlocked.Value != conversation.IsBlocked)
            {
                conversation.IsBlocked = request.IsBlocked.Value;
                conversation.BlockedByUserId = request.IsBlocked.Value ? currentUserId : null;
                conversation.BlockedAt = request.IsBlocked.Value ? DateTime.UtcNow : null;
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            await Conversations.UpdateAsync(conversation);
            await _unitOfWork.SaveChangesAsync();

            return ServiceResponse<bool>.Success(true, "Preferences updated.");
        }

        private static bool IsParticipant(Conversation conversation, string userId)
            => conversation.User1Id == userId || conversation.User2Id == userId;

        private static int NormalizePageSize(int pageSize)
            => Math.Clamp(pageSize, 1, 100);

        private ConversationSummaryDto MapConversationSummary(Conversation conversation, string currentUserId)
        {
            var dto = _mapper.Map<ConversationSummaryDto>(conversation);
            dto.CurrentUserId = currentUserId;

            var isUser1 = conversation.User1Id == currentUserId;
            dto.PartnerId = isUser1 ? conversation.User2Id : conversation.User1Id;
            dto.UnreadCount = isUser1 ? conversation.User1UnreadCount : conversation.User2UnreadCount;
            dto.IsMuted = isUser1 ? conversation.IsMutedByUser1 : conversation.IsMutedByUser2;
            dto.IsArchived = isUser1 ? conversation.IsArchivedByUser1 : conversation.IsArchivedByUser2;
            dto.IsPinned = isUser1 ? conversation.IsPinnedByUser1 : conversation.IsPinnedByUser2; // ✅ New: Pin status
            dto.IsBlocked = conversation.IsBlocked;
            dto.LastMessageAt = conversation.LastMessageAt;
            dto.LastMessageContent = conversation.LastMessageContent;
            dto.LastMessageSenderId = conversation.LastMessageSenderId;

            return dto;
        }

        private IEnumerable<MessageDto> MapMessages(List<Message> messages)
        {
            if (messages.Count == 0)
            {
                return Enumerable.Empty<MessageDto>();
            }

            var messageLookup = messages.ToDictionary(m => m.Id, m => m);
            var mapped = messages.Select(m => _mapper.Map<MessageDto>(m)).ToList();
            var lookup = mapped.ToDictionary(m => m.MessageId, m => m);

            foreach (var dto in mapped)
            {
                var message = messageLookup[dto.MessageId];
                PopulateAttachments(dto, message);

                if (dto.ReplyToMessageId.HasValue && lookup.TryGetValue(dto.ReplyToMessageId.Value, out var replyDto))
                {
                    dto.ReplyToMessage = replyDto;
                }
            }

            return mapped;
        }

        private MessageDto MapMessage(Message message)
        {
            var dto = _mapper.Map<MessageDto>(message);
            PopulateAttachments(dto, message);

            if (message.ReplyToMessage != null)
            {
                var replyDto = _mapper.Map<MessageDto>(message.ReplyToMessage);
                PopulateAttachments(replyDto, message.ReplyToMessage);
                replyDto.ReplyToMessage = null;
                dto.ReplyToMessage = replyDto;
            }

            return dto;
        }

        private static void PopulateAttachments(MessageDto dto, Message message)
        {
            if (message.Order != null)
            {
                dto.OrderAttachment = new MessageOrderAttachmentDto
                {
                    OrderId = message.Order.Id,
                    TotalAmount = message.Order.TotalAmount,
                    Status = message.Order.Status.ToString(),
                    PaymentStatus = message.Order.PaymentStatus.ToString(),
                    CreatedAt = message.Order.CreatedAt
                };
            }

            if (message.Product != null)
            {
                dto.ProductAttachment = new MessageProductAttachmentDto
                {
                    ProductId = message.Product.Id,
                    Name = message.Product.Name,
                    Price = message.Product.Price,
                    ThumbnailUrl = message.Product.Images?.FirstOrDefault()?.Url,
                    ShopId = message.Product.ShopId,
                    ShopName = message.Product.Shop?.Name
                };
            }
        }

        private void UpdateConversationOnNewMessage(Conversation conversation, Message message, string senderId)
        {
            var isSenderUser1 = conversation.User1Id == senderId;

            var preview = message switch
            {
                _ when message.OrderId.HasValue => $"[Order] {message.Order?.Status.ToString() ?? "Details"}",
                _ when message.ProductId.HasValue => $"[Product] {message.Product?.Name ?? message.Content}",
                _ => message.Type switch
                {
                    Domain.Enums.MessageType.Text => message.Content,
                    Domain.Enums.MessageType.Image => "[Image]",
                    Domain.Enums.MessageType.File => "[File]",
                    Domain.Enums.MessageType.Video => "[Video]",
                    Domain.Enums.MessageType.Audio => "[Audio]",
                    Domain.Enums.MessageType.Location => "[Location]",
                    Domain.Enums.MessageType.System => message.Content,
                    _ => message.Content
                }
            };

            conversation.LastMessageAt = message.CreatedAt;
            conversation.LastMessageContent = preview;
            conversation.LastMessageSenderId = senderId;
            conversation.UpdatedAt = DateTime.UtcNow;

            if (isSenderUser1)
            {
                conversation.User1UnreadCount = 0;
                conversation.User2UnreadCount += 1;
            }
            else
            {
                conversation.User2UnreadCount = 0;
                conversation.User1UnreadCount += 1;
            }
        }

        // ✅ New: Edit Message
        public async Task<ServiceResponse<MessageDto>> EditMessageAsync(string currentUserId, Guid messageId, EditMessageRequest request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var message = await Messages.GetByIdAsync(messageId);
                if (message == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("Message not found.");
                }

                // Validate ownership - chỉ sender mới có thể edit
                if (message.SenderId != currentUserId)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("You can only edit your own messages.");
                }

                // Validate conversation exists và user là participant
                var conversation = await Conversations.GetByIdAsync(message.ConversationId);
                if (conversation == null || !IsParticipant(conversation, currentUserId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("Conversation not found or access denied.");
                }

                // Validate content không rỗng
                if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<MessageDto>.Fail("Message content or attachment is required.");
                }

                // Validate AttachmentUrl nếu có
                if (!string.IsNullOrWhiteSpace(request.AttachmentUrl))
                {
                    if (!Uri.TryCreate(request.AttachmentUrl, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != "http" && uri.Scheme != "https"))
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return ServiceResponse<MessageDto>.Fail("Invalid attachment URL format.");
                    }
                }

                // Update message
                message.Content = request.Content?.Trim() ?? string.Empty;
                message.AttachmentUrl = request.AttachmentUrl;
                message.Metadata = request.Metadata;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;

                await Messages.UpdateAsync(message);

                // Update conversation last message content nếu đây là message cuối cùng
                if (conversation.LastMessageSenderId == currentUserId && 
                    conversation.LastMessageAt <= message.CreatedAt)
                {
                    var preview = string.IsNullOrWhiteSpace(message.Content) 
                        ? "[Attachment]" 
                        : message.Content.Length > 100 
                            ? message.Content.Substring(0, 100) + "..." 
                            : message.Content;
                    conversation.LastMessageContent = preview;
                    conversation.UpdatedAt = DateTime.UtcNow;
                    await Conversations.UpdateAsync(conversation);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var messageDto = MapMessage(message);
                return ServiceResponse<MessageDto>.Success(messageDto, "Message edited successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<MessageDto>.Fail($"Error editing message: {ex.Message}");
            }
        }

        // ✅ New: Delete Message
        public async Task<ServiceResponse<bool>> DeleteMessageAsync(string currentUserId, Guid messageId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var message = await Messages.GetByIdAsync(messageId);
                if (message == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("Message not found.");
                }

                // Validate ownership - chỉ sender mới có thể delete
                if (message.SenderId != currentUserId)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("You can only delete your own messages.");
                }

                // Validate conversation exists
                var conversation = await Conversations.GetByIdAsync(message.ConversationId);
                if (conversation == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("Conversation not found.");
                }

                // Soft delete message
                message.IsDeleted = true;
                message.DeletedAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;

                await Messages.UpdateAsync(message);

                // Update conversation last message nếu đây là message cuối cùng
                if (conversation.LastMessageSenderId == currentUserId && 
                    conversation.LastMessageAt <= message.CreatedAt)
                {
                    // Tìm message cuối cùng không bị xóa
                    var lastMessage = await Messages.GetMessagesAsync(message.ConversationId, 0, 1);
                    if (lastMessage.Count > 0)
                    {
                        var lastMsg = lastMessage[0];
                        conversation.LastMessageAt = lastMsg.CreatedAt;
                        conversation.LastMessageContent = lastMsg.Content?.Length > 100 
                            ? lastMsg.Content.Substring(0, 100) + "..." 
                            : lastMsg.Content ?? "[Message]";
                        conversation.LastMessageSenderId = lastMsg.SenderId;
                    }
                    else
                    {
                        conversation.LastMessageContent = null;
                        conversation.LastMessageSenderId = null;
                    }
                    conversation.UpdatedAt = DateTime.UtcNow;
                    await Conversations.UpdateAsync(conversation);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResponse<bool>.Success(true, "Message deleted successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<bool>.Fail($"Error deleting message: {ex.Message}");
            }
        }

        // ✅ New: Delete Conversation
        public async Task<ServiceResponse<bool>> DeleteConversationAsync(string currentUserId, Guid conversationId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var conversation = await Conversations.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("Conversation not found.");
                }

                if (!IsParticipant(conversation, currentUserId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return ServiceResponse<bool>.Fail("You are not a participant of this conversation.");
                }

                // Soft delete conversation
                conversation.IsDeleted = true;
                conversation.UpdatedAt = DateTime.UtcNow;

                await Conversations.UpdateAsync(conversation);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return ServiceResponse<bool>.Success(true, "Conversation deleted successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ServiceResponse<bool>.Fail($"Error deleting conversation: {ex.Message}");
            }
        }

        // ✅ New: Search Messages
        public async Task<ServiceResponse<PagedResult<MessageDto>>> SearchMessagesAsync(string currentUserId, Guid conversationId, string keyword, int page = 1, int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResponse<PagedResult<MessageDto>>.Fail("Keyword is required.");
            }

            if (page < 1) page = 1;
            pageSize = NormalizePageSize(pageSize);
            var skip = Math.Max(0, (page - 1) * pageSize);

            // Validate conversation và ownership
            var conversation = await Conversations.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                return ServiceResponse<PagedResult<MessageDto>>.Fail("Conversation not found.");
            }

            if (!IsParticipant(conversation, currentUserId))
            {
                return ServiceResponse<PagedResult<MessageDto>>.Fail("You are not a participant of this conversation.");
            }

            var (messages, totalCount) = await Messages.SearchMessagesAsync(conversationId, keyword, skip, pageSize);
            var messageDtos = MapMessages(messages).ToList();

            var pagedResult = new PagedResult<MessageDto>
            {
                Data = messageDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ServiceResponse<PagedResult<MessageDto>>.Success(pagedResult);
        }

        // ✅ New: Search Conversations
        public async Task<ServiceResponse<PagedResult<ConversationSummaryDto>>> SearchConversationsAsync(string currentUserId, string keyword, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResponse<PagedResult<ConversationSummaryDto>>.Fail("Keyword is required.");
            }

            if (page < 1) page = 1;
            pageSize = NormalizePageSize(pageSize);
            var skip = Math.Max(0, (page - 1) * pageSize);

            var (conversations, totalCount) = await Conversations.SearchConversationsAsync(currentUserId, keyword, skip, pageSize);

            var results = conversations
                .Select(conversation => MapConversationSummary(conversation, currentUserId))
                .ToList();

            var pagedResult = new PagedResult<ConversationSummaryDto>
            {
                Data = results,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return ServiceResponse<PagedResult<ConversationSummaryDto>>.Success(pagedResult);
        }

        // ✅ New: Get Total Unread Count
        public async Task<ServiceResponse<int>> GetTotalUnreadCountAsync(string currentUserId)
        {
            try
            {
                var totalUnread = await Messages.GetTotalUnreadCountAsync(currentUserId);
                return ServiceResponse<int>.Success(totalUnread, "Total unread count retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<int>.Fail($"Error getting total unread count: {ex.Message}");
            }
        }
    }
}

