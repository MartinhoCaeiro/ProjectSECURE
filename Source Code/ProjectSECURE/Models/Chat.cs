using System;

namespace ProjectSECURE.Models
{
    // Model representing a chat room
    public class Chat
    {
        // Unique identifier for the chat
        public string ChatId { get; set; } = Guid.NewGuid().ToString();
        // Name of the chat (optional)
        public string? Name { get; set; }
        // User ID of the chat administrator (optional)
        public string? AdminId { get; set; }
    }
}
