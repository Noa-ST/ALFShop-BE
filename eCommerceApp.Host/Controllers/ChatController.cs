using eCommerceApp.Aplication.DTOs;
using eCommerceApp.Aplication.DTOs.Chat;
using eCommerceApp.Aplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Collections.Generic;

namespace eCommerceApp.Host.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ChatController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        // ✅ Helper method để lấy UserId từ JWT Claim với validation
        private string GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Không thể xác định người dùng. Vui lòng đăng nhập lại.");
            }
            return userId;
        }

        [HttpGet("conversations")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<ConversationSummaryDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // ✅ Fix: Validate page >= 1
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Max page size

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.GetConversationsAsync(userId, page, pageSize);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi lấy danh sách cuộc trò chuyện: {ex.Message}" });
            }
        }

        [HttpGet("conversations/{conversationId:guid}")]
        [ProducesResponseType(typeof(ServiceResponse<ConversationDetailDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetConversation(Guid conversationId, [FromQuery] int messagePage = 1, [FromQuery] int pageSize = 50)
        {
            if (conversationId == Guid.Empty)
            {
                return BadRequest(new { Message = "ConversationId không hợp lệ." });
            }

            // ✅ Fix: Validate page >= 1
            if (messagePage < 1) messagePage = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100; // Max page size

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.GetConversationAsync(userId, conversationId, messagePage, pageSize);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi lấy cuộc trò chuyện: {ex.Message}" });
            }
        }

        [HttpPost("conversations")]
        [ProducesResponseType(typeof(ServiceResponse<ConversationSummaryDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.TargetUserId))
            {
                return BadRequest(new { Message = "TargetUserId không được để trống." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.CreateConversationAsync(userId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi tạo cuộc trò chuyện: {ex.Message}" });
            }
        }

        [HttpPost("conversations/{conversationId:guid}/messages")]
        [ProducesResponseType(typeof(ServiceResponse<MessageDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            if (conversationId == Guid.Empty)
            {
                return BadRequest(new { Message = "ConversationId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.SendMessageAsync(userId, conversationId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi gửi tin nhắn: {ex.Message}" });
            }
        }

        [HttpPut("conversations/{conversationId:guid}/read")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> MarkMessagesRead(Guid conversationId, [FromBody] MarkMessagesReadRequest request)
        {
            if (conversationId == Guid.Empty)
            {
                return BadRequest(new { Message = "ConversationId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.MarkMessagesReadAsync(userId, conversationId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi đánh dấu tin nhắn đã đọc: {ex.Message}" });
            }
        }

        [HttpPatch("conversations/{conversationId:guid}/preferences")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UpdatePreferences(Guid conversationId, [FromBody] UpdateConversationPreferenceRequest request)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            if (conversationId == Guid.Empty)
            {
                return BadRequest(new { Message = "ConversationId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.UpdatePreferencesAsync(userId, conversationId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi cập nhật tùy chọn: {ex.Message}" });
            }
        }

        // ✅ New: Edit Message
        [HttpPut("messages/{messageId:guid}")]
        [ProducesResponseType(typeof(ServiceResponse<MessageDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (messageId == Guid.Empty)
            {
                return BadRequest(new { Message = "MessageId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.EditMessageAsync(userId, messageId, request);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi chỉnh sửa tin nhắn: {ex.Message}" });
            }
        }

        // ✅ New: Delete Message
        [HttpDelete("messages/{messageId:guid}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                return BadRequest(new { Message = "MessageId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.DeleteMessageAsync(userId, messageId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi xóa tin nhắn: {ex.Message}" });
            }
        }

        // ✅ New: Delete Conversation
        [HttpDelete("conversations/{conversationId:guid}")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> DeleteConversation(Guid conversationId)
        {
            if (conversationId == Guid.Empty)
            {
                return BadRequest(new { Message = "ConversationId không hợp lệ." });
            }

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.DeleteConversationAsync(userId, conversationId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi xóa cuộc trò chuyện: {ex.Message}" });
            }
        }

        // ✅ New: Search Messages
        [HttpGet("conversations/{conversationId:guid}/messages/search")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<MessageDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> SearchMessages(Guid conversationId, [FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (conversationId == Guid.Empty)
            {
                return BadRequest(new { Message = "ConversationId không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { Message = "Keyword không được để trống." });
            }

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.SearchMessagesAsync(userId, conversationId, keyword, page, pageSize);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi tìm kiếm tin nhắn: {ex.Message}" });
            }
        }

        // ✅ New: Search Conversations
        [HttpGet("conversations/search")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<ConversationSummaryDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> SearchConversations([FromQuery] string keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { Message = "Keyword không được để trống." });
            }

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            try
            {
                var userId = GetUserId();
                var response = await _conversationService.SearchConversationsAsync(userId, keyword, page, pageSize);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi tìm kiếm cuộc trò chuyện: {ex.Message}" });
            }
        }

        // ✅ New: Get Total Unread Count
        [HttpGet("unread/count")]
        [ProducesResponseType(typeof(ServiceResponse<int>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetTotalUnreadCount()
        {
            try
            {
                var userId = GetUserId();
                var response = await _conversationService.GetTotalUnreadCountAsync(userId);
                return StatusCode((int)response.StatusCode, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = $"Lỗi khi lấy tổng số tin nhắn chưa đọc: {ex.Message}" });
            }
        }
    }
}

