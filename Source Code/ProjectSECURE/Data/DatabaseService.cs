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
                CREATE TABLE IF NOT EXISTS Users (
                    UserId TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Password TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Chats (
                    ChatId TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    AdminId TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Participants (
                    ParticipantId TEXT PRIMARY KEY,
                    ChatId TEXT NOT NULL,
                    UserId TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Messages (
                    MessageId TEXT PRIMARY KEY,
                    Content TEXT NOT NULL,
                    ParticipantId TEXT NOT NULL,
                    Date TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }
    }
}
