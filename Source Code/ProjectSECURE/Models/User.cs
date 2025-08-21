using System;

namespace ProjectSECURE.Models
{
    // Model representing a user in the system
    public class User
    {
        // Unique identifier for the user
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        // User's display name (optional)
        public string? Name { get; set; }
        // User's password (optional)
        public string? Password { get; set; }
    }
}
