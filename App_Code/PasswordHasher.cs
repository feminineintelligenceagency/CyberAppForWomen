using System;
using System.Security.Cryptography;
using System.Text;

namespace CyberApp_FIA.Services
{
    /// Epic #7: the ONE place passwords are hashed and checked.
    /// Replaces the copies that used to live in Login, CreateAccountPage,
    /// CreateUniversityAdmin, UniversityAdminAddHelper and PasswordReset.
    ///
    /// New passwords:  PBKDF2 with HMAC-SHA256, 310,000 iterations, 16-byte salt, 32-byte hash.
    /// Old passwords:  PBKDF2 with HMAC-SHA1, 100,000 iterations (still accepted, upgraded at next login).
    public static class PasswordHasher
    {
        // Saved in each user's <passwordAlgorithm> so we know how their hash was made.
        public const string CurrentAlgorithm = "PBKDF2-SHA256";

        private const int SaltSize = 16;              // bytes of random salt per user
        private const int HashSize = 32;              // bytes of hash output (256 bits)
        private const int CurrentIterations = 310000; // OWASP-recommended minimum for PBKDF2-SHA256
        private const int LegacyIterations = 100000;  // what the old code used (SHA1)

        /// Makes a new random salt and hashes the password with the current algorithm.
        /// Returns both as Base64 text, ready to store in users.xml.
        public static void CreateHash(string password, out string hashB64, out string saltB64)
        {
            // Fill the salt with cryptographically secure random bytes.
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var hash = Pbkdf2Sha256(password, salt, CurrentIterations, HashSize);

            hashB64 = Convert.ToBase64String(hash);
            saltB64 = Convert.ToBase64String(salt);
        }

        /// Returns true if the password matches the stored hash.
        /// "algorithm" is the user's <passwordAlgorithm> value ("" for old accounts).
        public static bool Verify(string password, string hashB64, string saltB64, string algorithm)
        {
            // Decode the stored values; if they're corrupted, treat it as a wrong password.
            byte[] salt, storedHash;
            try
            {
                salt = Convert.FromBase64String(saltB64);
                storedHash = Convert.FromBase64String(hashB64);
            }
            catch (FormatException)
            {
                return false;
            }

            // Re-hash the typed password the same way this account's hash was made.
            byte[] enteredHash = algorithm == CurrentAlgorithm
                ? Pbkdf2Sha256(password, salt, CurrentIterations, HashSize)
                : LegacyPbkdf2Sha1(password, salt);

            return SecureEquals(storedHash, enteredHash);
        }

        /// True if this account's hash was made with an older algorithm.</summary>
        public static bool NeedsUpgrade(string algorithm)
        {
            return algorithm != CurrentAlgorithm;
        }

        /// The old hashing, exactly as before Piece 4. Rfc2898DeriveBytes uses HMAC-SHA1
        /// on .NET Framework 4.7. Only used to CHECK old accounts, never to create new hashes.
        private static byte[] LegacyPbkdf2Sha1(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, LegacyIterations))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        /// PBKDF2 with HMAC-SHA256, following the standard (RFC 8018).
        /// .NET 4.7's built-in Rfc2898DeriveBytes can only do SHA1, so this builds PBKDF2
        /// on top of the built-in HMACSHA256 class.
        /// How it works: hash (salt + block number) with the password as the key, then
        /// re-hash the result 'iterations' times, XOR-ing every result together.
        /// The repetition is what makes each guess slow for an attacker.
        private static byte[] Pbkdf2Sha256(string password, byte[] salt, int iterations, int outputLength)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(password)))
            {
                int hashLength = hmac.HashSize / 8;                                 // 32 bytes per block
                int blockCount = (outputLength + hashLength - 1) / hashLength;     // blocks needed (1 for 32 bytes)
                var output = new byte[outputLength];

                for (int block = 1; block <= blockCount; block++)
                {
                    // Input for the first round: salt followed by the block number (4 bytes, big-endian).
                    var input = new byte[salt.Length + 4];
                    Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
                    input[salt.Length] = (byte)(block >> 24);
                    input[salt.Length + 1] = (byte)(block >> 16);
                    input[salt.Length + 2] = (byte)(block >> 8);
                    input[salt.Length + 3] = (byte)block;

                    // Round 1.
                    var u = hmac.ComputeHash(input);
                    var result = (byte[])u.Clone();

                    // Rounds 2..iterations: re-hash the previous round and XOR it into the result.
                    for (int i = 2; i <= iterations; i++)
                    {
                        u = hmac.ComputeHash(u);
                        for (int j = 0; j < result.Length; j++) result[j] ^= u[j];
                    }

                    // Copy this block into the output.
                    int offset = (block - 1) * hashLength;
                    Buffer.BlockCopy(result, 0, output, offset, Math.Min(hashLength, outputLength - offset));
                }

                return output;
            }
        }

        /// Compares every byte even after a mismatch, so an attacker can't learn
        /// how close a guess was from how long the check took.
        private static bool SecureEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}