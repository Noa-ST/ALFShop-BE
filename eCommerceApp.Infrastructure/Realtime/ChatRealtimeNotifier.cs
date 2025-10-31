using eCommerceApp.Aplication.DTOs.Chat;
using eCommerceApp.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerceApp.Infrastructure.Realtime
{
    public class ChatRealtimeNotifier : IChatRealtimeNotifier
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatRealtimeNotifier(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task BroadcastMessageAsync(MessageDto message, ConversationSummaryDto summaryForSender, ConversationSummaryDto summaryForReceiver)
        {
            var tasks = new List<Task>
            {
                _hubContext.Clients.Group(ChatHub.ConversationGroup(message.ConversationId))
                    .SendAsync(ChatHub.ReceiveMessageMethod, message)
            };

            if (!string.IsNullOrEmpty(summaryForSender.CurrentUserId))
            {
                tasks.Add(
                    _hubContext.Clients.User(summaryForSender.CurrentUserId)
                        .SendAsync(ChatHub.ConversationUpdatedMethod, summaryForSender));
            }

            if (!string.IsNullOrEmpty(summaryForReceiver.CurrentUserId))
            {
                tasks.Add(
                    _hubContext.Clients.User(summaryForReceiver.CurrentUserId)
                        .SendAsync(ChatHub.ConversationUpdatedMethod, summaryForReceiver));
            }

            return Task.WhenAll(tasks);
        }

        public Task BroadcastMessagesReadAsync(Guid conversationId, string readerId, int user1UnreadCount, int user2UnreadCount, IReadOnlyCollection<Guid> messageIds)
        {
            var payload = new
            {
                ConversationId = conversationId,
                ReaderId = readerId,
                MessageIds = messageIds,
                User1UnreadCount = user1UnreadCount,
                User2UnreadCount = user2UnreadCount
            };

            return _hubContext.Clients.Group(ChatHub.ConversationGroup(conversationId))
                .SendAsync(ChatHub.MessagesReadMethod, payload);
        }
    }
}

