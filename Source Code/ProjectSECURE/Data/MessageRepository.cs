using ProjectSECURE.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ProjectSECURE.Data
{
    public static class MessageRepository
    {
        /// <summary>
        /// Adds a list of messages into the local database, avoiding duplicates by MessageId.
        /// Returns the number of new messages inserted.
        /// </summary>
        public static int AddMessages(List<Message> newMessages)
        {
            int insertedCount = 0;
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            foreach (var msg in newMessages)
            {
                // Check if message already exists
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(1) FROM Messages WHERE MessageId = $id";
                checkCmd.Parameters.AddWithValue("$id", msg.MessageId);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                if (exists) continue;

                // Insert new message
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"INSERT INTO Messages (MessageId, Content, ParticipantId, Date) VALUES ($id, $content, $pid, $date)";
                insertCmd.Parameters.AddWithValue("$id", msg.MessageId);
                insertCmd.Parameters.AddWithValue("$content", msg.Content ?? "");
                insertCmd.Parameters.AddWithValue("$pid", msg.ParticipantId ?? "");
                insertCmd.Parameters.AddWithValue("$date", msg.Date.ToString("o"));
                insertCmd.ExecuteNonQuery();
                insertedCount++;
            }
            return insertedCount;
        }
        public static List<Message> LoadMessages(string chatId)
        {
            var messages = new List<Message>();

            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT m.MessageId, m.Content, m.ParticipantId, m.Date,
                       p.UserId
                FROM Messages m
                JOIN Participants p ON m.ParticipantId = p.ParticipantId
                WHERE p.ChatId = $chatId
                ORDER BY m.Date ASC";
            cmd.Parameters.AddWithValue("$chatId", chatId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(new Message
                {
                    MessageId = reader.GetString(0),
                    Content = reader.GetString(1),
                    ParticipantId = reader.GetString(2),
                    Date = DateTime.Parse(reader.GetString(3)),
                    SenderUserId = reader.GetString(4)
                });
            }

            return messages;
        }

        public static void SendMessage(string chatId, string userId, string content)
        {
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            // Buscar participantId
            var getCmd = conn.CreateCommand();
            getCmd.CommandText = @"SELECT ParticipantId FROM Participants WHERE ChatId = $chatId AND UserId = $userId";
            getCmd.Parameters.AddWithValue("$chatId", chatId);
            getCmd.Parameters.AddWithValue("$userId", userId);

            var participantId = getCmd.ExecuteScalar()?.ToString();
            if (participantId == null) return;

            var insert = conn.CreateCommand();
            insert.CommandText = @"INSERT INTO Messages (MessageId, Content, ParticipantId, Date) 
                                   VALUES ($id, $content, $pid, $date)";
            insert.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            insert.Parameters.AddWithValue("$content", content);
            insert.Parameters.AddWithValue("$pid", participantId);
            var bstZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var bstNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bstZone);
            insert.Parameters.AddWithValue("$date", bstNow.ToString("o"));
            insert.ExecuteNonQuery();
        }
    }
}
