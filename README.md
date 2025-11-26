# ProjectSECURE — Secure Communications System

Short description
-----------------
ProjectSECURE is a student project for developing a secure communications system focused on exchanging encrypted text messages between users. The solution integrates a WireGuard VPN layer with client applications (Windows desktop in C# and Android in Kotlin) and a server-side component (Python scripts + Flask) to synchronize encrypted SQLite databases between peers.

This README is derived from the project report (Relatorio.tex) and summarizes the architecture, technologies, usage notes and development considerations.

Key features
------------
- End-to-end encrypted message exchange (symmetric encryption).
- Per-message encrypted SQLite database files used as the message container.
- Two symmetric cipher options used in the project: AES (Rijndael) and Serpent (AEAD mode via BouncyCastle).
- WireGuard VPN integration for an additional networking security layer.
- Cross-platform clients: Windows (C#/.NET) and Android (Kotlin).
- Server utilities in Python (Flask) for file sync and for serving WireGuard client configuration files.
- Framing and AEAD-based envelope for robust message transfer (LEN-prefix framing, AAD authenticated headers, anti-replay SEQ checks).

Architecture overview
---------------------
- Clients (Android, Windows) connect to the server and to each other through a WireGuard VPN.
- Instead of sending individual message rows, each message transmission is implemented by encrypting and sending the entire SQLite database file (snapshot), ensuring consistent sync between peers.
- The DbCrypto module (Kotlin/Java) performs encryption and decryption:
  - PBKDF2-SHA256 (100k iterations) derives a 256-bit key from a passphrase/masterKey + salt.
  - Encryption uses AES-GCM or Serpent-GCM; AAD authenticates a canonical header that contains metadata (magic, version, obfuscated alg id, iterations, salt, nonce).
  - Encrypted envelope format: header || ciphertext || tag.
- Transport robustness:
  - Length-prefixed frames (4 bytes, big-endian).
  - BufferAssembler to handle TCP fragmentation/coalescing.
  - SEQ-based anti-replay and sequential processing to avoid race conditions.
  - Atomic DB writes with snapshots (VACUUM INTO) on Android to avoid locking the main DB during sync.

Technologies used
-----------------
- Languages: C#, Kotlin, Python, Bash
- Client frameworks: .NET (Windows), Android SDK (Kotlin)
- Server: Flask (Python) for file sync and configuration serving
- VPN: WireGuard
- Database: SQLite
- Crypto: AES-GCM (Rijndael) and Serpent-GCM (via BouncyCastle); PBKDF2-SHA256 for key derivation; AEAD framing
- Tools: Git, VS Code, Android Studio

Security notes
--------------
- AEAD (GCM) is used to provide confidentiality and integrity. AAD authenticates header metadata; any mismatch causes decryption failure.
- Nonces must be unique per key. The project uses nonce construction derived from sequence numbers and other context to ensure uniqueness.
- The current prototype uses a static "master" key/passphrase in places; treat this as a development artifact. In production, use secure per-user key management and rotation.
- Sending the full DB file per message is simple and consistent but inefficient at scale. Consider moving to per-message records and delta sync for production.

Quick setup (server)
--------------------
1. Prepare AlmaLinux/AlmaOS 8.10 server (or other Linux) and install dependencies.
2. Install WireGuard (example steps used in the project):
   - sudo dnf install epel-release
   - sudo dnf install https://www.elrepo.org/elrepo-release-8.el8.elrepo.noarch.rpm
   - sudo /usr/bin/crb enable
   - sudo dnf makecache
   - sudo dnf --enablerepo=elrepo install kmod-wireguard -y
   - sudo dnf install wireguard-tools -y
3. Generate server keys and protect the private key:
   - wg genkey | tee server_private.key | wg pubkey > server_public.key
   - chmod 600 server_private.key
4. Sample /etc/wireguard/wg0.conf (edit keys / addresses):
   ```
   [Interface]
   PrivateKey = <ServerPrivateKey>
   Address = 10.0.0.1/24
   ListenPort = 51820
   SaveConfig = true

   [Peer]
   PublicKey = <ClientPublicKey>
   AllowedIPs = 10.0.0.2/32
   ```
5. Enable kernel forwarding and firewall rules:
   - echo "net.ipv4.ip_forward = 1" | sudo tee -a /etc/sysctl.conf
   - sudo sysctl -p
   - sudo firewall-cmd --add-masquerade --permanent
   - sudo firewall-cmd --add-port=51820/udp --permanent
   - sudo firewall-cmd --add-port=8000/tcp --permanent  # Flask API
   - sudo firewall-cmd --reload
   - Enable wg-quick: sudo systemctl enable --now wg-quick@wg0

Server file sync and config serving
----------------------------------
- A Python Flask API is used to allow clients to download:
  - The WireGuard client config file (created by an admin script).
  - Encrypted database snapshots / messages.
- Example endpoint used in the project (conceptual): http://192.168.1.106:9595/{username}
- Admin scripts:
  - Script to generate and register client WireGuard configurations and add peers to wg0.conf.
  - Script to provide DB snapshots to clients (download/upload endpoints).

Client notes
------------
- Windows client: C# / .NET UI; integrates with WireGuard state and allows download of config file.
- Android client: Kotlin, Room/SQLite local DB; uses DbCrypto for encryption and snapshot logic (VACUUM INTO) before upload.
- Permissions for Android: ensure INTERNET and ACCESS_NETWORK_STATE are declared in AndroidManifest.xml.
- Ensure WireGuard is installed and the client config is imported before app authentication (the project enforces auth only when the WireGuard VPN is active).

Development and testing notes
-----------------------------
- Framing and AEAD header consistency across platforms is critical. Use the canonical header (LEN, MAGIC, VER, ALG, FLAGS, SEQ, TIMESTAMP, NONCE, TAG, AAD_LEN, CT_LEN, AAD, CIPHERTEXT).
- Logging, hex/base64 dumps and deterministic self-tests were used during debugging to find BAD_DECRYPT issues.
- Use BufferAssembler and length-prefix parsing to handle TCP fragmentation and coalescing.
- Test coverage includes:
  - Deterministic vectors at startup (self-test).
  - Stress/fuzzing of parser and envelope handling (rapid bursts, truncated/corrupted frames).
  - Negative tests (replayed nonces, truncated tags, modified AAD).

Known limitations
-----------------
- Prototype uses full DB-file transfers for each message — not efficient for production.
- Key management is rudimentary; master passphrase "Spartacus" appears in report as example — replace with secure key exchange in real deployment.
- WireGuard client config provisioning currently requires trusting the admin pipeline; rotate keys and secure transport before production.

Future improvements
-------------------
- Move from full DB transfer to per-message delta synchronization.
- Introduce TLS/HTTPS for the Flask endpoints and authenticate clients with mutual TLS or token-based auth.
- Implement a proper key-exchange (e.g., X25519 + authenticated key agreement) and per-session keys with HKDF.
- Add multimedia support (images, voice) with chunked encrypted transfer.
- Scale the server with load balancer and database backend (instead of file-based SQLite snapshots) when needed.

Authors & contributors
----------------------
- Martinho José Novo Caeiro — 23917 
- Paulo António Tavares Abade — 23919
- Rafael Conceição Narciso — 24473

Acknowledgements
----------------
- Professor Rui Miguel Soares Silva — project advisor
- WireGuard, SQLite, BouncyCastle and other open-source projects used in this work.

License
-------
- This repository is licensed under the GNU General Public License v3.0 (GPL-3.0).


