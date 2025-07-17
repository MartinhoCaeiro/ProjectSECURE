using System;

namespace ChatAppWPF.Models
{
    public class User
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        public string? Name { get; set; }
        public string? Password { get; set; }
    }
}
