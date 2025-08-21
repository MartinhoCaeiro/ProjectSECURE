using System;

namespace ProjectSECURE.Models
{
    // Model representing a chat message
    public class Message
    {
        // Unique identifier for the message
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        // ID of the participant who sent the message (optional)
        public string? ParticipantId { get; set; }
        // Message content
        public string? Content { get; set; }
        // Date and time the message was sent
        public DateTime Date { get; set; }
        // User ID of the sender (used for joins with Participants)
        public string? SenderUserId { get; set; }
    }
}
