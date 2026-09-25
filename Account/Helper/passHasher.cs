using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using System.Xml;
using Konscious.Security.Cryptography;


namespace CyberApp_FIA.Services
{
    /// <summary>
    /// 
    /// 
    /// 
    /// <summary>

    public class passHasher
    {
        // idntifiers for the hashing algorithms (pbkdf2 is legacy and should be converted)
        public const string Argon2ID = "Argon2";
        public const string pbkdf2ID = "pbkdf2";

        private const string Argon2Version = "19";
        private const string HashElementName = "passwordHash";
        private const string SaltElementName = "passwordSalt";

        // Parameters for Argon2 hashing as recommended by OWASP (19 MiB with 2 iterations and 1 degree parallelism). Can be adjusted for performance and security trade-offs.
        public const int Argon2MemSize = 19456;
        public const int Argon2Iterations = 2;
        public const int Argon2DegreeOfParallelism = 1;

        // Salt and hash sizes for Argon2 and PBKDF2
        public const int Argon2SaltSize = 16;
        public const int Argon2HashSize = 32;

        public const int pbkdf2SaltSize = 100000;
        public const int pbkdf2HashSize = 32;

        private static byte[] dummySalt = GenerateSalt();
        public static void dummyVerify(String password){ HashArgon2Password(password, dummySalt); }

        /// <summary>
        /// Hashes the password with Argon2id and writes passwordHash / passwordSalt onto the user element, creating them if needed. 
        /// Used for sign-up, password changes/resets, and login upgrades. 
        /// <summary>
        public static void SetPassword(XmlElement user, string password)
        {
            if (user == null) { throw new ArgumentNullException(nameof(user)); }

            var doc = user.OwnerDocument;
            // Generate a new salt for Argon2
            var salt = GenerateSalt(Argon2SaltSize);
            var hash = HashArgon2Password(password, salt);

            // Write the hash and salt to the user element
            var userHash = user[HashElementName];
            var userSalt = user[SaltElementName];

            // Create the elements if they don't exist
            if (userHash == null)
            {
                userHash = doc.CreateElement(HashElementName);
                user.AppendChild(userHash);
            }
            if (userSalt == null)
            {
                userSalt = doc.CreateElement(SaltElementName);
                user.AppendChild(userSalt);
            }

            userHash.InnerText = Convert.ToBase64String(hash);
            userHash.SetAttribute("algo", Argon2ID);
            userHash.SetAttribute("m", Argon2MemSize.ToString());
            userHash.SetAttribute("t", Argon2Iterations.ToString());
            userHash.SetAttribute("p", Argon2DegreeOfParallelism.ToString());

            userSalt.InnerText = Convert.ToBase64String(salt);

        }

        /// <summary>
        /// function to verify a user's password against the stored hash and salt in the XML user element. 
        /// This is the main entry point for password verification and will also indicate if a rehash is recommended based on the current hashing parameters.
        /// <summary>
        public static bool VerifyUser(XmlElement user, string password, out bool doRehash)
        {
            doRehash = false;
            if (user == null) { throw new ArgumentNullException(nameof(user)); }

            var userHash = user[HashElementName];
            var userSalt = user[SaltElementName];
            if (userHash == null || userSalt == null){return false;}

            string algo = userHash.GetAttribute("algo");
            int memory = int.TryParse(userHash.GetAttribute("m"), out int m) ? m : Argon2MemSize;
            int iterations = int.TryParse(userHash.GetAttribute("t"), out int t) ? t : Argon2Iterations;
            int parallelism = int.TryParse(userHash.GetAttribute("p"), out int p) ? p : Argon2DegreeOfParallelism;
            return Verify(password, algo, userHash.InnerText, userSalt.InnerText, memory, iterations, parallelism, out doRehash);
        }

        /// <summary>
        /// Verifies a password against the stored hash and salt, and indicates if a rehash is recommended based on the current hashing parameters. 
        /// If the stored hash is from PBKDF2, it will flag the password for rehashing to Argon2 on the next login.
        /// <summary>
        private static bool Verify(
            string password,
            string algo,
            string hashBase64,
            string saltBase64,
            int memory,
            int iterations,
            int parallelism,
            out bool doRehash)
        {
            doRehash = false;

            if (string.IsNullOrEmpty(hashBase64) || string.IsNullOrEmpty(saltBase64)) { return false; }

            //catch any exceptions from invalid base64 strings 
            byte[] salt, hash;
            try
            {
                salt = Convert.FromBase64String(saltBase64);
                hash = Convert.FromBase64String(hashBase64);
            }
            catch(FormatException){return false;}

            //Compare the provided password with the stored hash using Argon2 and mark for rehash if the parameters differ from the recommended ones.
            if (algo == Argon2ID)
            {
                int m = memory > 0 ? memory : Argon2MemSize;
                int t = iterations > 0 ? iterations : Argon2Iterations;
                int p = parallelism > 0 ? parallelism : Argon2DegreeOfParallelism;

                var computedHash = HashArgon2Password(password, Convert.FromBase64String(saltBase64), m, t, p);
                if (!fixedTimeEquals(computedHash, hash)) return false;
                doRehash = m != Argon2MemSize || t != Argon2Iterations || p != Argon2DegreeOfParallelism;

                return true;
            }
            // TODO: if pbldf2 is used, set doRehash to true so that the user can be upgraded to Argon2 on next login.
            else if (algo == pbkdf2ID || string.IsNullOrEmpty(algo))
            {
                var computedHash = HashPassword(password, salt);
                if (!fixedTimeEquals(computedHash, hash)) return false;

                doRehash = true;
                return true;
            }
            else
            {
                throw new ArgumentException("Unknown hashing algorithm specified.");
            }

            return false;
        }

        /// <summary>
        /// generate a cryptographically secure random salt for each user. 
        /// <summary>
        private static byte[] GenerateSalt(int size = Argon2SaltSize)
        {
            var salt = new byte[size];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        /// <summary>
        /// derives a password hash using Argon2id and the recommended parameters from OWASP.
        /// - Uses the provided per-user salt.
        /// - memory size og 19 MiB 
        /// - 2 iterations of hashing
        /// - 1 degree of parallelism
        /// - Memory size, iterations, and parallelism can be adjusted for security/performance trade-offs.
        /// <summary>
        private static byte[] HashArgon2Password(
            string password,
            byte[] salt,
            int memorySize = Argon2MemSize,
            int iterations = Argon2Iterations,
            int parallelism = Argon2DegreeOfParallelism)
        {
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password ?? string.Empty)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = parallelism;
                argon2.MemorySize = memorySize;
                argon2.Iterations = iterations;
                return argon2.GetBytes(Argon2HashSize);
            }
        }

        /// <summary>
        /// Derives a password hash using PBKDF2 (Rfc2898DeriveBytes).
        /// - Uses the provided per-user salt.
        /// - 100,000 iterations (demo-friendly; consider higher for production as hardware allows).
        /// - Returns a 32-byte (256-bit) derived key suitable for storage.
        /// 
        /// This is a legacy method and should be replaced with Argon2 for new accounts.
        /// </summary>
        private static byte[] HashPassword(string password, byte[] salt)
        {
            // NOTE: In .NET Framework, Rfc2898DeriveBytes defaults to HMACSHA1.
            // In newer .NETs, you can specify HMACSHA256 explicitly if available.
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                return pbkdf2.GetBytes(32); // 256-bit hash
            }
        }

        /// <summary>
        /// Time constant comparison of two byte arrays to prevent timing attacks. Returns true if the arrays are equal, false otherwise.
        /// <summary>
        private static bool fixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}