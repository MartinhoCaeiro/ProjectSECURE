using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProjectSECURE.Services
{
    public class DbSyncService
    {
        private static readonly string serverUrl = "http://10.0.0.1:8000";
        // Always use the project directory for the database
        private static readonly string localDbPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Database",
            "ProjectSECURE.db");

        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Periodically downloads the database, replaces the local DB file, and reloads the database context.
        /// </summary>
        public static async Task<bool> PeriodicDownloadAndReloadAsync()
        {
            string tempDbPath = Path.Combine(Path.GetTempPath(), "ProjectSECURE_temp.db");
            try
            {
                // Download to temp file
                var url = $"{serverUrl}/download";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Download falhou: {response.StatusCode}");
                    return false;
                }
                var dbBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempDbPath, dbBytes);

                // Close all DB connections (implement this in your DatabaseService)
                try
                {
                    // ProjectSECURE.Data.DatabaseService.CloseConnection();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao fechar conexões antes de substituir a base de dados: " + ex.Message);
                }

                // Replace local DB file
                File.Copy(tempDbPath, localDbPath, true);

                // Reload DB context (implement this in your DatabaseService)
                try
                {
                    // ProjectSECURE.Data.DatabaseService.Reload();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao recarregar contexto da base de dados: " + ex.Message);
                }

                File.Delete(tempDbPath);
                Console.WriteLine("Base de dados atualizada e recarregada com sucesso.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao atualizar base de dados: " + ex.Message);
                try { if (File.Exists(tempDbPath)) File.Delete(tempDbPath); } catch { }
                return false;
            }
        }

        public static async Task<bool> DownloadDatabaseAsync()
        {
            try
            {
                var url = $"{serverUrl}/download";
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var dbBytes = await response.Content.ReadAsByteArrayAsync();
                    var dir = Path.GetDirectoryName(localDbPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    await File.WriteAllBytesAsync(localDbPath, dbBytes);
                    Console.WriteLine("Base de dados baixada com sucesso.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Download falhou: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao fazer download da base de dados: " + ex.Message);
                return false;
            }
        }


        public static async Task<bool> UploadDatabaseAsync()
        {
            try
            {
                if (!File.Exists(localDbPath))
                {
                    Console.WriteLine("Base de dados local não encontrada.");
                    return false;
                }

                // Força o fecho de qualquer possível ligação antes de copiar
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string tempFilePath = Path.GetTempFileName();
                File.Copy(localDbPath, tempFilePath, true); // copia a BD com todos os dados persistidos

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(tempFilePath));
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                content.Add(fileContent, "file", "ProjectSECURE.db");
                Console.WriteLine(content.Headers);
                var response = await httpClient.PostAsync($"{serverUrl}/upload", content);

                Console.WriteLine(response.IsSuccessStatusCode
                    ? "✅ Upload da base de dados feito com sucesso."
                    : $"❌ Erro no upload: {response.StatusCode}");

                File.Delete(tempFilePath); // apaga cópia temporária

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Erro ao fazer upload da base de dados: " + ex.Message);
                return false;
            }
        }


    }
}
