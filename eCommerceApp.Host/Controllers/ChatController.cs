using eCommerceApp.Aplication.DTOs.Chat;
using eCommerceApp.Application.Services.Interfaces;
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

        [HttpGet("conversations")]
        [ProducesResponseType(typeof(IEnumerable<ConversationSummaryDto>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _conversationService.GetConversationsAsync(userId, page, pageSize);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("conversations/{conversationId:guid}")]
        [ProducesResponseType(typeof(ConversationDetailDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetConversation(Guid conversationId, [FromQuery] int messagePage = 1, [FromQuery] int pageSize = 50)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _conversationService.GetConversationAsync(userId, conversationId, messagePage, pageSize);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("conversations")]
        [ProducesResponseType(typeof(ConversationSummaryDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _conversationService.CreateConversationAsync(userId, request);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("conversations/{conversationId:guid}/messages")]
        [ProducesResponseType(typeof(MessageDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _conversationService.SendMessageAsync(userId, conversationId, request);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPut("conversations/{conversationId:guid}/read")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> MarkMessagesRead(Guid conversationId, [FromBody] MarkMessagesReadRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _conversationService.MarkMessagesReadAsync(userId, conversationId, request);
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPatch("conversations/{conversationId:guid}/preferences")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePreferences(Guid conversationId, [FromBody] UpdateConversationPreferenceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var response = await _conversationService.UpdatePreferencesAsync(userId, conversationId, request);
            return StatusCode((int)response.StatusCode, response);
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}

