using System;

namespace ProjectSECURE.Models
{
    public class Chat
    {
        public string ChatId { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public string? AdminId { get; set; }
    }
}
