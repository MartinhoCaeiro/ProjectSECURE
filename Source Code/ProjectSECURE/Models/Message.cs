using System;

namespace ProjectSECURE.Models
{
    public class Message
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string? ParticipantId { get; set; }
        public string? Content { get; set; }
        public DateTime Date { get; set; }
        public string? SenderUserId { get; set; } // Adicionado para o Join com Participants
    }
}
