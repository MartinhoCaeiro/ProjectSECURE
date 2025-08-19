using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ProjectSECURE.Crypto; 

namespace ProjectSECURE.Services
{
    public class DbSyncService
    {
        private static readonly string serverUrl = "http://10.0.0.1:8000";
        private static readonly string localDbPath = Path.Combine(
            Directory.GetCurrentDirectory(), "Database", "ProjectSECURE.db");

        private static readonly HttpClient httpClient = new HttpClient();

        // >>>> DEFINE a tua forma real de obter a master key
        private static byte[] GetMasterKey()
        {
            // Exemplo simples: passphrase em memória (troca isto por algo robusto!)
            string passphrase = Environment.GetEnvironmentVariable("PROJECTSECURE_PASSPHRASE")
                                ?? "Spartacus";
            return DbCrypto.KeyFromPassphrase(passphrase);
        }

        public static async Task<bool> PeriodicDownloadAndReloadAsync()
        {
            string tempDbPath = Path.Combine(Path.GetTempPath(), "ProjectSECURE_temp.db");
            try
            {
                var url = $"{serverUrl}/download";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Download falhou: {response.StatusCode}");
                    return false;
                }

                // RECEBES ENVELOPE -> DESENCRIPTAS -> gravar .db temporária
                var envelope = await response.Content.ReadAsByteArrayAsync();
                var plaintextDb = DbCrypto.Decrypt(envelope, GetMasterKey());
                await File.WriteAllBytesAsync(tempDbPath, plaintextDb);

                try { /* ProjectSECURE.Data.DatabaseService.CloseConnection(); */ }
                catch (Exception ex) { Console.WriteLine("Erro ao fechar conexões: " + ex.Message); }

                File.Copy(tempDbPath, localDbPath, true);

                try { /* ProjectSECURE.Data.DatabaseService.Reload(); */ }
                catch (Exception ex) { Console.WriteLine("Erro ao recarregar contexto: " + ex.Message); }

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
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Download falhou: {response.StatusCode}");
                    return false;
                }

                // Envelope -> plaintext DB
                var envelope = await response.Content.ReadAsByteArrayAsync();
                var plaintextDb = DbCrypto.Decrypt(envelope, GetMasterKey());

                var dir = Path.GetDirectoryName(localDbPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(localDbPath, plaintextDb);
                Console.WriteLine("Base de dados baixada e desencriptada com sucesso.");
                return true;
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

                // Força fechos, copia para temp e lê bytes
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string tempFilePath = Path.GetTempFileName();
                File.Copy(localDbPath, tempFilePath, true);

                var dbBytes = await File.ReadAllBytesAsync(tempFilePath);

                // <<< ENCRIPTA A DB ANTES DE ENVIAR >>>
                var envelope = DbCrypto.Encrypt(dbBytes, GetMasterKey());

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(envelope);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                // podes mudar o nome para .enc se quiseres
                content.Add(fileContent, "file", "ProjectSECURE.db.enc");

                var response = await httpClient.PostAsync($"{serverUrl}/upload", content);

                Console.WriteLine(response.IsSuccessStatusCode
                    ? "✅ Upload encriptado feito com sucesso."
                    : $"❌ Erro no upload: {response.StatusCode}");

                File.Delete(tempFilePath);
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
