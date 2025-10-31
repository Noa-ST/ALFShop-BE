namespace eCommerceApp.Aplication.DTOs.Chat
{
    public class UpdateConversationPreferenceRequest
    {
        public bool? IsArchived { get; set; }
        public bool? IsMuted { get; set; }
        public bool? IsBlocked { get; set; }
    }
}

