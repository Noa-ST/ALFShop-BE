using eCommerceApp.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eCommerceApp.Infrastructure.Realtime
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IConversationRepository _conversationRepository;

        public const string ReceiveMessageMethod = "ReceiveMessage";
        public const string ConversationUpdatedMethod = "ConversationUpdated";
        public const string MessagesReadMethod = "MessagesRead";

        public ChatHub(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository;
        }

        public static string ConversationGroup(Guid conversationId)
            => $"conversation:{conversationId}";

        public async Task JoinConversation(Guid conversationId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Unauthorized");
            }

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null || conversation.IsDeleted || !IsParticipant(conversation, userId))
            {
                throw new HubException("Conversation access denied");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
        }

        public Task LeaveConversation(Guid conversationId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
        }

        private static bool IsParticipant(Domain.Entities.Conversation conversation, string userId)
            => conversation.User1Id == userId || conversation.User2Id == userId;
    }
}

