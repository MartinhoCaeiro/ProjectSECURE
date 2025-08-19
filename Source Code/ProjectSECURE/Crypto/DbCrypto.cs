using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// BouncyCastle
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace ProjectSECURE.Crypto
{
    public static class DbCrypto
    {
        private const string Magic = "PSEC";         // 4 bytes
        private const byte Version = 1;
        private const int SaltSize = 16;             // 128-bit salt
        private const int NonceSize = 12;            // 96-bit nonce for GCM
        private const int TagSize = 16;              // 128-bit auth tag
        private const int Iterations = 100_000;      // PBKDF2 rounds
        private const int KeySizeBytes = 32;         // 256-bit keys

        private enum Alg : byte { AesGcm = 1, SerpentGcm = 2 }

        /// <summary>
        /// Deriva uma chave de 32B a partir do masterKey + salt (PBKDF2/HMAC-SHA256).
        /// </summary>
        private static byte[] DeriveKey(byte[] masterKey, byte[] salt, int iterations)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(masterKey, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySizeBytes);
        }

        /// <summary>
        /// Decide o algoritmo com base no LSB do "componente de chave" (primeiro byte do SHA-256 do masterKey).
        /// </summary>
        private static Alg PickAlg(byte[] masterKey)
        {
            using var sha = SHA256.Create();
            var h = sha.ComputeHash(masterKey);
            return (h[0] & 0x01) == 0 ? Alg.AesGcm : Alg.SerpentGcm;
        }

        /// <summary>
        /// Encripta bytes da DB para um envelope autenticado.
        /// </summary>
        public static byte[] Encrypt(byte[] plaintextDb, byte[] masterKey)
        {
            if (plaintextDb is null || plaintextDb.Length == 0)
                throw new ArgumentException("DB vazia.");
            if (masterKey is null || masterKey.Length == 0)
                throw new ArgumentException("masterKey vazia.");

            // <<< ALTERADO: escolher algoritmo aleatoriamente >>>
            var alg = (Alg)(RandomNumberGenerator.GetInt32(0, 2) == 0
                            ? Alg.AesGcm
                            : Alg.SerpentGcm);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var key = DeriveKey(masterKey, salt, Iterations);

            // AAD = cabeçalho estático (sem nonce)
            var header = BuildHeader(alg, Iterations, salt, nonce, aadOnly: true);


            byte[] ciphertext, tag;

            if (alg == Alg.AesGcm)
            {
                ciphertext = new byte[plaintextDb.Length];
                tag = new byte[TagSize];
                using var aes = new AesGcm(key);
                aes.Encrypt(nonce, plaintextDb, ciphertext, tag, header); // header como AAD
            }
            else // Serpent GCM via BouncyCastle
            {
                var gcm = new GcmBlockCipher(new SerpentEngine());
                var parameters = new AeadParameters(new KeyParameter(key), TagSize * 8, nonce, header);
                gcm.Init(true, parameters);
                var outBuf = new byte[gcm.GetOutputSize(plaintextDb.Length)];
                int len = gcm.ProcessBytes(plaintextDb, 0, plaintextDb.Length, outBuf, 0);
                len += gcm.DoFinal(outBuf, len);
                // BC retorna CT||TAG no buffer
                int ctLen = outBuf.Length - TagSize;
                ciphertext = new byte[ctLen];
                tag = new byte[TagSize];
                Buffer.BlockCopy(outBuf, 0, ciphertext, 0, ctLen);
                Buffer.BlockCopy(outBuf, ctLen, tag, 0, TagSize);
            }

            // Monta envelope final (header completo + ciphertext + tag)
            var envelopeHeader = BuildHeader(alg, Iterations, salt, nonce, aadOnly: false);
            var output = new byte[envelopeHeader.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(envelopeHeader, 0, output, 0, envelopeHeader.Length);
            Buffer.BlockCopy(ciphertext, 0, output, envelopeHeader.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, output, envelopeHeader.Length + ciphertext.Length, tag.Length);

            CryptographicOperations.ZeroMemory(key);
            return output;
        }

        /// <summary>
        /// Desencripta o envelope para bytes de DB.
        /// </summary>
        public static byte[] Decrypt(byte[] envelope, byte[] masterKey)
        {
            if (envelope is null || envelope.Length < 4 + 1 + 1 + 4 + SaltSize + NonceSize + TagSize)
                throw new ArgumentException("Envelope inválido.");
            if (masterKey is null || masterKey.Length == 0) throw new ArgumentException("masterKey vazia.");

            int offset = 0;

            // MAGIC
            if (Encoding.ASCII.GetString(envelope, offset, 4) != Magic) throw new CryptographicException("Magic inválido.");
            offset += 4;

            // VERSION
            byte ver = envelope[offset++]; if (ver != Version) throw new CryptographicException($"Versão não suportada: {ver}");

            // ALG
            var alg = (Alg)envelope[offset++];

            // ITER
            int iter = BinaryPrimitives.ReadInt32BigEndian(envelope.AsSpan(offset, 4)); offset += 4;

            // SALT
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(envelope, offset, salt, 0, SaltSize); offset += SaltSize;

            // NONCE
            var nonce = new byte[NonceSize];
            Buffer.BlockCopy(envelope, offset, nonce, 0, NonceSize); offset += NonceSize;

            // rest = CT || TAG
            int ctLen = envelope.Length - offset - TagSize;
            if (ctLen < 0) throw new CryptographicException("Envelope truncado.");
            var ciphertext = new byte[ctLen];
            Buffer.BlockCopy(envelope, offset, ciphertext, 0, ctLen); offset += ctLen;

            var tag = new byte[TagSize];
            Buffer.BlockCopy(envelope, offset, tag, 0, TagSize);

            var key = DeriveKey(masterKey, salt, iter);
            var headerAAD = BuildHeader(alg, iter, salt, nonce, aadOnly: true);

            var plaintext = new byte[ciphertext.Length];

            if (alg == Alg.AesGcm)
            {
                using var aes = new AesGcm(key);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, headerAAD);
            }
            else
            {
                var gcm = new GcmBlockCipher(new SerpentEngine());
                var parameters = new AeadParameters(new KeyParameter(key), TagSize * 8, nonce, headerAAD);
                gcm.Init(false, parameters);
                var inBuf = new byte[ciphertext.Length + TagSize];
                Buffer.BlockCopy(ciphertext, 0, inBuf, 0, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, inBuf, ciphertext.Length, TagSize);

                var outBuf = new byte[gcm.GetOutputSize(inBuf.Length)];
                int len = gcm.ProcessBytes(inBuf, 0, inBuf.Length, outBuf, 0);
                len += gcm.DoFinal(outBuf, len);
                if (len != plaintext.Length) Array.Resize(ref outBuf, len);
                plaintext = outBuf;
            }

            CryptographicOperations.ZeroMemory(key);
            return plaintext;
        }

        /// <summary>
        /// Constrói o cabeçalho (para AAD e/ou para escrever no envelope)
        /// </summary>
        private static byte[] BuildHeader(Alg alg, int iter, byte[] salt, byte[] nonce, bool aadOnly)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, true);
            bw.Write(Encoding.ASCII.GetBytes(Magic));   // 4
            bw.Write(Version);                          // 1
            bw.Write((byte)alg);                        // 1
            var iterBuf = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(iterBuf, iter);
            bw.Write(iterBuf);                          // 4
            bw.Write(salt);                             // 16
            if (!aadOnly) bw.Write(nonce); else bw.Write(nonce.AsSpan(0, NonceSize)); // ainda inclui nonce no AAD
            bw.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Helper caso queiras usar passphrase em vez de bytes de chave.
        /// </summary>
        public static byte[] KeyFromPassphrase(string passphrase) =>
            Encoding.UTF8.GetBytes(passphrase ?? throw new ArgumentNullException(nameof(passphrase)));
    }
}
