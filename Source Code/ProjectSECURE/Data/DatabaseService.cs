using Microsoft.Data.Sqlite;
using System;
using System.IO;


namespace ProjectSECURE.Data
{
    // Service for database initialization and connection string management
    public static class DatabaseService
    {
        // Path to the database file
        private static string dbPath;

        // Static constructor sets up the database path and ensures the database is initialized
        static DatabaseService()
        {
            // Always use the current working directory for the database
            string dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Database");
            Directory.CreateDirectory(dataDir); // create folder if it doesn't exist

            dbPath = Path.Combine(dataDir, "ProjectSECURE.db");
            InitializeDatabase();
        }

        // Returns the connection string for SQLite
        public static string GetConnectionString() => $"Data Source={dbPath}";

        // Initializes the database and creates tables if they do not exist
        public static void InitializeDatabase()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    UserId TEXT PRIMARY KEY NOT NULL,
                    Name TEXT NOT NULL,
                    Password TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS chats (
                    ChatId TEXT PRIMARY KEY NOT NULL,
                    Name TEXT NOT NULL,
                    AdminId TEXT NOT NULL,
                    FOREIGN KEY (AdminId) REFERENCES users(UserId) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS participants (
                    ParticipantId TEXT PRIMARY KEY NOT NULL,
                    ChatId TEXT NOT NULL,
                    UserId TEXT NOT NULL,
                    FOREIGN KEY (ChatId) REFERENCES chats(ChatId) ON DELETE CASCADE,
                    FOREIGN KEY (UserId) REFERENCES users(UserId) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS messages (
                    MessageId TEXT PRIMARY KEY NOT NULL,
                    Content TEXT NOT NULL,
                    ParticipantId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    FOREIGN KEY (ParticipantId) REFERENCES participants(ParticipantId) ON DELETE CASCADE
                );
            ";
            command.ExecuteNonQuery();
        }
    }
}
