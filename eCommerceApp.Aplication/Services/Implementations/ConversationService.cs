using AutoMapper;
using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Chat;
using eCommerceApp.Aplication.Services.Interfaces;
using eCommerceApp.Domain.Entities;
using eCommerceApp.Domain.Enums;
using eCommerceApp.Domain.Interfaces;
using eCommerceApp.Domain.Repositories;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eCommerceApp.Aplication.Services.Implementations
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IChatRealtimeNotifier _chatNotifier;
        private readonly IMapper _mapper;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IChatRealtimeNotifier chatNotifier,
            IMapper mapper)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _chatNotifier = chatNotifier;
            _mapper = mapper;
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

            var existing = await _conversationRepository.GetBetweenUsersAsync(currentUserId, request.TargetUserId);
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

            await _conversationRepository.AddAsync(conversation);

            var dto = MapConversationSummary(conversation, currentUserId);
            return ServiceResponse<ConversationSummaryDto>.Success(dto, "Conversation created successfully.");
        }

        public async Task<ServiceResponse<ConversationDetailDto>> GetConversationAsync(string currentUserId, Guid conversationId, int messagePage = 1, int pageSize = 50)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
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

            var messages = await _messageRepository.GetMessagesAsync(conversationId, skip, pageSize);
            var messageDtos = MapMessages(messages);

            var detail = _mapper.Map<ConversationDetailDto>(conversation);
            detail.Messages = messageDtos;

            return ServiceResponse<ConversationDetailDto>.Success(detail);
        }

        public async Task<ServiceResponse<List<ConversationSummaryDto>>> GetConversationsAsync(string currentUserId, int page = 1, int pageSize = 20)
        {
            pageSize = NormalizePageSize(pageSize);
            var skip = Math.Max(0, (page - 1) * pageSize);

            var conversations = await _conversationRepository.GetUserConversationsAsync(currentUserId, skip, pageSize);

            var results = conversations
                .Select(conversation => MapConversationSummary(conversation, currentUserId))
                .ToList();

            return ServiceResponse<List<ConversationSummaryDto>>.Success(results);
        }

        public async Task<ServiceResponse<bool>> MarkMessagesReadAsync(string currentUserId, Guid conversationId, MarkMessagesReadRequest request)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                return ServiceResponse<bool>.Fail("Conversation not found.");
            }

            if (!IsParticipant(conversation, currentUserId))
            {
                return ServiceResponse<bool>.Fail("You are not a participant of this conversation.");
            }

            var upTo = request.UpTo ?? DateTime.UtcNow;
            var unreadMessages = await _messageRepository.GetUnreadMessagesAsync(conversationId, currentUserId, upTo);

            if (unreadMessages.Count == 0)
            {
                return ServiceResponse<bool>.Success(true, "No unread messages to update.");
            }

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                message.UpdatedAt = DateTime.UtcNow;
            }

            await _messageRepository.SaveChangesAsync();

            if (conversation.User1Id == currentUserId)
            {
                conversation.User1UnreadCount = Math.Max(0, conversation.User1UnreadCount - unreadMessages.Count);
            }
            else
            {
                conversation.User2UnreadCount = Math.Max(0, conversation.User2UnreadCount - unreadMessages.Count);
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation);

            await _chatNotifier.BroadcastMessagesReadAsync(
                conversationId,
                currentUserId,
                conversation.User1UnreadCount,
                conversation.User2UnreadCount,
                unreadMessages.Select(m => m.Id).ToList());

            return ServiceResponse<bool>.Success(true, "Marked messages as read.");
        }

        public async Task<ServiceResponse<MessageDto>> SendMessageAsync(string currentUserId, Guid conversationId, SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            {
                return ServiceResponse<MessageDto>.Fail("Message content or attachment is required.");
            }

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                return ServiceResponse<MessageDto>.Fail("Conversation not found.");
            }

            if (!IsParticipant(conversation, currentUserId))
            {
                return ServiceResponse<MessageDto>.Fail("You are not a participant of this conversation.");
            }

            if (conversation.IsBlocked && conversation.BlockedByUserId != currentUserId)
            {
                return ServiceResponse<MessageDto>.Fail("Conversation is blocked.");
            }

            Message? replyTarget = null;
            if (request.ReplyToMessageId.HasValue)
            {
                replyTarget = await _messageRepository.GetByIdAsync(request.ReplyToMessageId.Value);
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
                var order = await _orderRepository.GetByIdAsync(request.OrderId.Value);
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
                var product = await _productRepository.GetDetailByIdAsync(request.ProductId.Value);
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

            await _messageRepository.AddAsync(message);

            UpdateConversationOnNewMessage(conversation, message, currentUserId);
            await _conversationRepository.UpdateAsync(conversation);

            var messageDto = MapMessage(message);

            var senderSummary = MapConversationSummary(conversation, currentUserId);
            var receiverId = conversation.User1Id == currentUserId ? conversation.User2Id : conversation.User1Id;
            var receiverSummary = MapConversationSummary(conversation, receiverId);

            await _chatNotifier.BroadcastMessageAsync(messageDto, senderSummary, receiverSummary);

            return ServiceResponse<MessageDto>.Success(messageDto, "Message sent successfully.");
        }

        public async Task<ServiceResponse<bool>> UpdatePreferencesAsync(string currentUserId, Guid conversationId, UpdateConversationPreferenceRequest request)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
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

            if (request.IsBlocked.HasValue && request.IsBlocked.Value != conversation.IsBlocked)
            {
                conversation.IsBlocked = request.IsBlocked.Value;
                conversation.BlockedByUserId = request.IsBlocked.Value ? currentUserId : null;
                conversation.BlockedAt = request.IsBlocked.Value ? DateTime.UtcNow : null;
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation);

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
    }
}

