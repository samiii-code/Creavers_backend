namespace Creavers.API.DTOs.Chat
{
    public class SendChatMessageRequest
    {
        /// <example>Hello, I am on my way!</example>
        public string Message { get; set; } = string.Empty;
    }

    public class ChatMessageResponse
    {
        public Guid     Id         { get; set; }
        public Guid     BookingId  { get; set; }
        public Guid     SenderId   { get; set; }
        public string   SenderName { get; set; } = string.Empty;
        public string   Message    { get; set; } = string.Empty;
        public DateTime SentAt     { get; set; }
        public bool     IsRead     { get; set; }
    }
}
