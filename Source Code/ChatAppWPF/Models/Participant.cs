using System;

namespace ChatAppWPF.Models
{
    public class Participant
    {
        public string ParticipantId { get; set; } = Guid.NewGuid().ToString();
        public string? ChatId { get; set; }
        public string? UserId { get; set; }
    }
}
