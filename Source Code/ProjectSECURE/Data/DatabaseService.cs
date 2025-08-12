using Microsoft.Data.Sqlite;
using System;
using System.IO;


namespace ProjectSECURE.Data
{
    public static class DatabaseService
    {
        private static string dbPath;

        static DatabaseService()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir); // cria a pasta se não existir

            dbPath = Path.Combine(dataDir, "ProjectSECURE.db");
            InitializeDatabase();
        }

        public static string GetConnectionString() => $"Data Source={dbPath}";

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
