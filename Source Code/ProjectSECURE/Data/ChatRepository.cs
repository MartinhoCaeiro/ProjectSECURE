using ProjectSECURE.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ProjectSECURE.Data
{
    public static class ChatRepository
    {
        public static List<Chat> GetChatsForUser(string userId)
        {
            var chats = new List<Chat>();
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT c.ChatId, c.Name, c.AdminId
                FROM Chats c
                JOIN Participants p ON c.ChatId = p.ChatId
                WHERE p.UserId = $userId";
            cmd.Parameters.AddWithValue("$userId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                chats.Add(new Chat
                {
                    ChatId = reader.GetString(0),
                    Name = reader.GetString(1),
                    AdminId = reader.GetString(2)
                });
            }

            return chats;
        }

        public static Chat? CreateNewChat(string chatName, string adminId)
        {
            var newChat = new Chat
            {
                ChatId = Guid.NewGuid().ToString(),
                Name = chatName,
                AdminId = adminId
            };

            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            using var transaction = conn.BeginTransaction();

            // Inserir chat
            var insertChat = conn.CreateCommand();
            insertChat.CommandText = @"INSERT INTO Chats (ChatId, Name, AdminId) VALUES ($chatId, $name, $adminId)";
            insertChat.Parameters.AddWithValue("$chatId", newChat.ChatId);
            insertChat.Parameters.AddWithValue("$name", newChat.Name);
            insertChat.Parameters.AddWithValue("$adminId", newChat.AdminId);
            insertChat.ExecuteNonQuery();

            // Inserir participante (criador/admin)
            var insertParticipant = conn.CreateCommand();
            insertParticipant.CommandText = @"INSERT INTO Participants (ParticipantId, ChatId, UserId) 
                                              VALUES ($participantId, $chatId, $userId)";
            insertParticipant.Parameters.AddWithValue("$participantId", Guid.NewGuid().ToString());
            insertParticipant.Parameters.AddWithValue("$chatId", newChat.ChatId);
            insertParticipant.Parameters.AddWithValue("$userId", newChat.AdminId);
            insertParticipant.ExecuteNonQuery();

            transaction.Commit();

            return newChat;
        }

        public static string CreateChat(string chatName, string adminId, List<User> participants)
        {
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            using var transaction = conn.BeginTransaction();

            var chatId = Guid.NewGuid().ToString();

            // Criar chat
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Chats (ChatId, Name, AdminId) VALUES ($chatId, $name, $adminId)";
            cmd.Parameters.AddWithValue("$chatId", chatId);
            cmd.Parameters.AddWithValue("$name", chatName);
            cmd.Parameters.AddWithValue("$adminId", adminId);
            cmd.ExecuteNonQuery();

            // Participantes
            foreach (var user in participants)
            {
                var cmdPart = conn.CreateCommand();
                cmdPart.CommandText = @"INSERT INTO Participants (ParticipantId, ChatId, UserId) 
                                        VALUES ($participantId, $chatId, $userId)";
                cmdPart.Parameters.AddWithValue("$participantId", Guid.NewGuid().ToString());
                cmdPart.Parameters.AddWithValue("$chatId", chatId);
                cmdPart.Parameters.AddWithValue("$userId", user.UserId);
                cmdPart.ExecuteNonQuery();
            }

            transaction.Commit();
            return chatId;
        }
    }
}
