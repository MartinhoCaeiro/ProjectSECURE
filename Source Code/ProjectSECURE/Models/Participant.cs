using System;

namespace ProjectSECURE.Models
{
    // Model representing a chat participant
    public class Participant
    {
        // Unique identifier for the participant
        public string ParticipantId { get; set; } = Guid.NewGuid().ToString();
        // ID of the chat the participant belongs to (optional)
        public string? ChatId { get; set; }
        // ID of the user who is the participant (optional)
        public string? UserId { get; set; }
    }
}
