using ChatAppWPF.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ChatAppWPF.Data
{
    public static class MessageRepository
    {
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
            insert.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("o"));

            insert.ExecuteNonQuery();
        }
    }
}
