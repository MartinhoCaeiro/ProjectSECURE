using ChatAppWPF.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ChatAppWPF.Data
{
    public static class UserRepository
    {
        public static void CreateUser(User user)
        {
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            // Verificar se já existe um usuário com o mesmo nome
            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Name = $name";
            checkCmd.Parameters.AddWithValue("$name", user.Name);

            long count = (long)checkCmd.ExecuteScalar();
            if (count > 0)
                throw new Exception("Usuário já existe.");

            // Inserir novo usuário
            var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Users (UserId, Name, Password) 
                VALUES ($id, $name, $password)";
            insertCmd.Parameters.AddWithValue("$id", user.UserId);
            insertCmd.Parameters.AddWithValue("$name", user.Name);
            insertCmd.Parameters.AddWithValue("$password", user.Password);
            insertCmd.ExecuteNonQuery();
        }

        public static User? ValidateLogin(string username, string password)
        {
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT UserId, Name, Password 
                FROM Users 
                WHERE Name = $name AND Password = $password";
            cmd.Parameters.AddWithValue("$name", username);
            cmd.Parameters.AddWithValue("$password", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId = reader.GetString(0),
                    Name = reader.GetString(1),
                    Password = reader.GetString(2)
                };
            }
            return null;
        }

        public static List<User> GetAllUsers()
        {
            var users = new List<User>();
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UserId, Name, Password FROM Users";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    UserId = reader.GetString(0),
                    Name = reader.GetString(1),
                    Password = reader.GetString(2)
                });
            }
            return users;
        }
    }
}
