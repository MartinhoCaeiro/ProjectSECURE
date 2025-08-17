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
        private static readonly string localDbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Database",
            "ProjectSECURE.db");

        private static readonly HttpClient httpClient = new HttpClient();

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
