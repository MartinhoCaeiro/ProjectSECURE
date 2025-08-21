using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ProjectSECURE.Crypto;

namespace ProjectSECURE.Services
{
    // Service for synchronizing the local database with the remote server
    public class DbSyncService
    {
        // URL of the remote server
        private static readonly string serverUrl = "http://10.0.0.1:8000";
        // Path to the local database file
        private static readonly string localDbPath = Path.Combine(
            Directory.GetCurrentDirectory(), "Database", "ProjectSECURE.db");

        // Shared HTTP client for requests
        private static readonly HttpClient httpClient = new HttpClient();

        // Returns the master key used for encryption/decryption
        private static byte[] GetMasterKey()
        {
            string passphrase = Environment.GetEnvironmentVariable("PROJECTSECURE_PASSPHRASE")
                                ?? "Spartacus";
            return DbCrypto.KeyFromPassphrase(passphrase);
        }

        // Periodically downloads the database from the server and reloads it locally
        public static async Task<bool> PeriodicDownloadAndReloadAsync()
        {
            string tempDbPath = Path.Combine(Path.GetTempPath(), "ProjectSECURE_temp.db");
            try
            {
                var url = $"{serverUrl}/download";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // Download encrypted database envelope and decrypt it
                var envelope = await response.Content.ReadAsByteArrayAsync();
                var plaintextDb = DbCrypto.Decrypt(envelope, GetMasterKey());
                await File.WriteAllBytesAsync(tempDbPath, plaintextDb);

                // Optionally close and reload database connection (commented out)
                try { /* ProjectSECURE.Data.DatabaseService.CloseConnection(); */ }
                catch { }

                // Replace local database with the new one
                File.Copy(tempDbPath, localDbPath, true);

                // Optionally reload database connection (commented out)
                try { /* ProjectSECURE.Data.DatabaseService.Reload(); */ }
                catch { }

                // Clean up temporary file
                File.Delete(tempDbPath);
                return true;
            }
            catch
            {
                // Clean up temp file if an error occurs
                try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
                return false;
            }
        }

        // Downloads the database from the server and saves it locally
        public static async Task<bool> DownloadDatabaseAsync()
        {
            try
            {
                var url = $"{serverUrl}/download";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // Download and decrypt the database envelope
                var envelope = await response.Content.ReadAsByteArrayAsync();
                var plaintextDb = DbCrypto.Decrypt(envelope, GetMasterKey());

                // Ensure local database directory exists
                var dir = Path.GetDirectoryName(localDbPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(localDbPath, plaintextDb);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Uploads the local database to the server
        public static async Task<bool> UploadDatabaseAsync()
        {
            try
            {
                if (!File.Exists(localDbPath))
                {
                    return false;
                }

                // Force close file handles and copy to a temp file
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string tempFilePath = Path.GetTempFileName();
                File.Copy(localDbPath, tempFilePath, true);

                // Read database bytes from temp file
                var dbBytes = await File.ReadAllBytesAsync(tempFilePath);

                // Encrypt the database before sending
                var envelope = DbCrypto.Encrypt(dbBytes, GetMasterKey());

                // Prepare HTTP content for upload
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(envelope);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                // Add encrypted file to the request
                content.Add(fileContent, "file", "ProjectSECURE.db.enc");

                // Send POST request to upload the database
                var response = await httpClient.PostAsync($"{serverUrl}/upload", content);

                // Clean up temp file
                File.Delete(tempFilePath);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
