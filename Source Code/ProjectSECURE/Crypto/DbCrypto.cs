using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace ProjectSECURE.Crypto
{
    // Static class for encrypting and decrypting database files
    public static class DbCrypto
    {
        // Magic header for envelope format
        private const string Magic = "PSEC";         // 4 bytes
        // Envelope version
        private const byte Version = 1;
        // Salt size for key derivation
        private const int SaltSize = 16;             // 128-bit salt
        // Nonce size for GCM
        private const int NonceSize = 12;            // 96-bit nonce for GCM
        // Authentication tag size
        private const int TagSize = 16;              // 128-bit auth tag
        // PBKDF2 iteration count
        private const int Iterations = 100_000;      // PBKDF2 rounds
        // Key size in bytes
        private const int KeySizeBytes = 32;         // 256-bit keys

        // Supported encryption algorithms
        private enum Alg : byte { AesGcm = 1, SerpentGcm = 2 }

        // Derives a 32-byte key from the master key and salt using PBKDF2/HMAC-SHA256
        private static byte[] DeriveKey(byte[] masterKey, byte[] salt, int iterations)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(masterKey, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySizeBytes);
        }

        // Picks the encryption algorithm based on the master key
        private static Alg PickAlg(byte[] masterKey)
        {
            using var sha = SHA256.Create();
            var h = sha.ComputeHash(masterKey);
            return (h[0] & 0x01) == 0 ? Alg.AesGcm : Alg.SerpentGcm;
        }

        // Encrypts database bytes into an authenticated envelope
        public static byte[] Encrypt(byte[] plaintextDb, byte[] masterKey)
        {
            if (plaintextDb is null || plaintextDb.Length == 0)
                throw new ArgumentException("Empty database.");
            if (masterKey is null || masterKey.Length == 0)
                throw new ArgumentException("Empty masterKey.");

            // Randomly choose encryption algorithm (AesGcm or SerpentGcm)
            var alg = (Alg)(RandomNumberGenerator.GetInt32(0, 2) == 0
                            ? Alg.AesGcm
                            : Alg.SerpentGcm);

            // Generate salt and nonce
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var key = DeriveKey(masterKey, salt, Iterations);

            // Build header for AAD (additional authenticated data)
            var header = BuildHeader(alg, Iterations, salt, nonce, aadOnly: true);

            byte[] ciphertext, tag;

            if (alg == Alg.AesGcm)
            {
                // Encrypt using AES-GCM (specify tag size)
                ciphertext = new byte[plaintextDb.Length];
                tag = new byte[TagSize];
                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, plaintextDb, ciphertext, tag, header);
            }
            else // Serpent GCM via BouncyCastle
            {
                // Encrypt using Serpent-GCM
                var gcm = new GcmBlockCipher(new SerpentEngine());
                var parameters = new AeadParameters(new KeyParameter(key), TagSize * 8, nonce, header);
                gcm.Init(true, parameters);
                var outBuf = new byte[gcm.GetOutputSize(plaintextDb.Length)];
                int len = gcm.ProcessBytes(plaintextDb, 0, plaintextDb.Length, outBuf, 0);
                len += gcm.DoFinal(outBuf, len);
                // BouncyCastle returns CT||TAG in buffer
                int ctLen = outBuf.Length - TagSize;
                ciphertext = new byte[ctLen];
                tag = new byte[TagSize];
                Buffer.BlockCopy(outBuf, 0, ciphertext, 0, ctLen);
                Buffer.BlockCopy(outBuf, ctLen, tag, 0, TagSize);
            }

            // Build final envelope: header + ciphertext + tag
            var envelopeHeader = BuildHeader(alg, Iterations, salt, nonce, aadOnly: false);
            var output = new byte[envelopeHeader.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(envelopeHeader, 0, output, 0, envelopeHeader.Length);
            Buffer.BlockCopy(ciphertext, 0, output, envelopeHeader.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, output, envelopeHeader.Length + ciphertext.Length, tag.Length);

            CryptographicOperations.ZeroMemory(key);
            return output;
        }

        // Decrypts an envelope to database bytes
        public static byte[] Decrypt(byte[] envelope, byte[] masterKey)
        {
            if (envelope is null || envelope.Length < 4 + 1 + 1 + 4 + SaltSize + NonceSize + TagSize)
                throw new ArgumentException("Invalid envelope.");
            if (masterKey is null || masterKey.Length == 0) throw new ArgumentException("Empty masterKey.");

            int offset = 0;

            // Read and validate magic header
            if (Encoding.ASCII.GetString(envelope, offset, 4) != Magic) throw new CryptographicException("Invalid magic.");
            offset += 4;

            // Read and validate version
            byte ver = envelope[offset++]; if (ver != Version) throw new CryptographicException($"Unsupported version: {ver}");

            // Read algorithm
            var alg = (Alg)envelope[offset++];

            // Read iteration count
            int iter = BinaryPrimitives.ReadInt32BigEndian(envelope.AsSpan(offset, 4)); offset += 4;

            // Read salt
            var salt = new byte[SaltSize];
            Buffer.BlockCopy(envelope, offset, salt, 0, SaltSize); offset += SaltSize;

            // Read nonce
            var nonce = new byte[NonceSize];
            Buffer.BlockCopy(envelope, offset, nonce, 0, NonceSize); offset += NonceSize;

            // Read ciphertext and tag
            int ctLen = envelope.Length - offset - TagSize;
            if (ctLen < 0) throw new CryptographicException("Truncated envelope.");
            var ciphertext = new byte[ctLen];
            Buffer.BlockCopy(envelope, offset, ciphertext, 0, ctLen); offset += ctLen;

            var tag = new byte[TagSize];
            Buffer.BlockCopy(envelope, offset, tag, 0, TagSize);

            // Derive key and build header for AAD
            var key = DeriveKey(masterKey, salt, iter);
            var headerAAD = BuildHeader(alg, iter, salt, nonce, aadOnly: true);

            var plaintext = new byte[ciphertext.Length];

            if (alg == Alg.AesGcm)
            {
                // Decrypt using AES-GCM (specify tag size)
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, headerAAD);
            }
            else
            {
                // Decrypt using Serpent-GCM
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

        // Builds the envelope header (for AAD and/or for writing in the envelope)
        private static byte[] BuildHeader(Alg alg, int iter, byte[] salt, byte[] nonce, bool aadOnly)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.ASCII, true);
            bw.Write(Encoding.ASCII.GetBytes(Magic));   // 4 bytes: magic
            bw.Write(Version);                          // 1 byte: version
            bw.Write((byte)alg);                        // 1 byte: algorithm
            var iterBuf = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(iterBuf, iter);
            bw.Write(iterBuf);                          // 4 bytes: iteration count
            bw.Write(salt);                             // 16 bytes: salt
            // Write nonce if not AAD only
            if (!aadOnly) bw.Write(nonce); else bw.Write(nonce.AsSpan(0, NonceSize));
            bw.Flush();
            return ms.ToArray();
        }

        // Helper to use a passphrase instead of a key byte array
        public static byte[] KeyFromPassphrase(string passphrase) =>
            Encoding.UTF8.GetBytes(passphrase ?? throw new ArgumentNullException(nameof(passphrase)));
    }
}
