using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProjectSECURE.Data
{
    public static class DatabaseMerger
    {
        /// <summary>
        /// Merges all tables from the temp database into the local database, avoiding duplicates by primary key.
        /// </summary>
        public static void MergeAllFromTempDb()
        {
            string tempDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ProjectSECURE_temp.db");
            if (!File.Exists(tempDbPath)) return;

            var tables = new[] { "users", "chats", "participants", "messages" };
            var pkColumns = new Dictionary<string, string>
            {
                { "users", "UserId" },
                { "chats", "ChatId" },
                { "participants", "ParticipantId" },
                { "messages", "MessageId" }
            };

            var tempConnStr = $"Data Source={tempDbPath}";
            using var tempConn = new SqliteConnection(tempConnStr);
            tempConn.Open();

            using var localConn = new SqliteConnection(DatabaseService.GetConnectionString());
            localConn.Open();

            foreach (var table in tables)
            {
                var pk = pkColumns[table];
                var cmd = tempConn.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {table}";
                using var reader = cmd.ExecuteReader();
                var colCount = reader.FieldCount;
                while (reader.Read())
                {
                    // Check if record exists in local DB
                    var checkCmd = localConn.CreateCommand();
                    checkCmd.CommandText = $"SELECT COUNT(1) FROM {table} WHERE {pk} = $pk";
                    checkCmd.Parameters.AddWithValue("$pk", reader[pk]);
                    var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    if (exists) continue;

                    // Build insert command
                    var colNames = new List<string>();
                    var paramNames = new List<string>();
                    var insertCmd = localConn.CreateCommand();
                    for (int i = 0; i < colCount; i++)
                    {
                        var colName = reader.GetName(i);
                        colNames.Add(colName);
                        paramNames.Add("$" + colName);
                        insertCmd.Parameters.AddWithValue("$" + colName, reader.GetValue(i));
                    }
                    insertCmd.CommandText = $"INSERT INTO {table} ({string.Join(",", colNames)}) VALUES ({string.Join(",", paramNames)})";
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
    }
}
