using ProjectSECURE.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ProjectSECURE.Data
{
    // Repository for user-related database operations
    public static class UserRepository
    {
        // Creates a new user in the database, throws if username already exists
        public static void CreateUser(User user)
        {
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            // Check if a user with the same name already exists
            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Name = $name";
            checkCmd.Parameters.AddWithValue("$name", user.Name);

            var result = checkCmd.ExecuteScalar();
            long count = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;
            if (count > 0)
                throw new Exception("User already exists.");

            // Insert new user
            var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Users (UserId, Name, Password) 
                VALUES ($id, $name, $password)";
            insertCmd.Parameters.AddWithValue("$id", user.UserId);
            insertCmd.Parameters.AddWithValue("$name", user.Name);
            insertCmd.Parameters.AddWithValue("$password", user.Password);
            insertCmd.ExecuteNonQuery();

            // Upload database after creating user
            _ = ProjectSECURE.Services.DbSyncService.UploadDatabaseAsync();
        }

        // Validates login credentials and returns the user if found
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

        // Returns all users in the database
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

        // Returns a user by their userId
        public static User? GetUserById(string userId)
        {
            using var conn = new SqliteConnection(DatabaseService.GetConnectionString());
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT UserId, Name, Password 
                FROM Users 
                WHERE UserId = $id";
            cmd.Parameters.AddWithValue("$id", userId);

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

    }
}
