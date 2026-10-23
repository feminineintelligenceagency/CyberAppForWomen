using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;


namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Everything sign-up and login need from the Users and UserCredentials tables.
    /// Pages call these methods, they never write SQL directly.
    /// </summary>
    public static class UserRepository
    {
        /// <summary>A user plus their password data, as needed by the login page.</summary>
        public sealed class LoginRecord
        {
            public string UserId { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string Role { get; set; }
            public string University { get; set; }

            public byte[] PasswordHash { get; set; }
            public byte[] PasswordSalt { get; set; }
            public string Algorithm { get; set; }
            public int? Argon2MemoryKiB { get; set; }
            public int? Argon2Iterations { get; set; }
            public int? Argon2Parallelism { get; set; }

            public int FailedLoginCount { get; set; }
            public DateTime? LockoutUntilUtc { get; set; }

            public bool MfaEnabled { get; set; }
            public string MfaSecretProtected { get; set; }
            public DateTime? PasswordUpdatedAtUtc { get; set; }
            public string PasswordStamp => FormatStamp(PasswordUpdatedAtUtc);

            public bool IsLockedOut(DateTime nowUtc)
            {
                return LockoutUntilUtc.HasValue && LockoutUntilUtc.Value > nowUtc;
            }
        }

        /// <summary>The profile fields collected at sign-up.</summary>
        public sealed class NewUser
        {
            public string UserId { get; set; }          
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Role { get; set; }           
            public string University { get; set; }
            public DateTime? ConsentAcceptedAtUtc { get; set; }
            public string ConsentIp { get; set; }
        }

        /// <summary>Maps a SqlDataReader row to a LoginRecord.</summary>
        /// used for both GetLoginByEmail and GetLoginById. Returns null if the reader is empty.
        private static LoginRecord MapLogin(SqlDataReader r)
        {
            return new LoginRecord
            {
                UserId = r.GetString(r.GetOrdinal("UserId")),
                Email = r.GetString(r.GetOrdinal("Email")),
                FirstName = Database.GetStringOrNull(r, "FirstName"),
                Role = r.GetString(r.GetOrdinal("Role")).Trim(),
                University = Database.GetStringOrNull(r, "University"),
                PasswordHash = Database.GetBytesOrNull(r, "PasswordHash"),
                PasswordSalt = Database.GetBytesOrNull(r, "PasswordSalt"),
                Algorithm = r.GetString(r.GetOrdinal("Algorithm")),
                Argon2MemoryKiB = Database.GetNullableInt(r, "Argon2MemoryKiB"),
                Argon2Iterations = Database.GetNullableInt(r, "Argon2Iterations"),
                Argon2Parallelism = Database.GetNullableInt(r, "Argon2Parallelism"),
                FailedLoginCount = r.GetInt32(r.GetOrdinal("FailedLoginCount")),
                LockoutUntilUtc = Database.GetNullableDateTime(r, "LockoutUntilUtc"),
                MfaEnabled = r.GetBoolean(r.GetOrdinal("MfaEnabled")),
                MfaSecretProtected = Database.GetStringOrNull(r, "MfaSecret"),
                PasswordUpdatedAtUtc = Database.GetNullableDateTime(r, "PasswordUpdatedAtUtc")
            };
        }


        private const string LoginSelect = @"
            SELECT u.UserId, u.Email, u.FirstName, u.Role, u.University,
                   c.PasswordHash, c.PasswordSalt, c.Algorithm,
                   c.Argon2MemoryKiB, c.Argon2Iterations, c.Argon2Parallelism,
                   c.FailedLoginCount, c.LockoutUntilUtc,
                   c.MfaEnabled, c.MfaSecret, c.PasswordUpdatedAtUtc
              FROM dbo.Users u
              JOIN dbo.UserCredentials c ON c.UserId = u.UserId
            ";

        /// <summary>
        /// Looks up a user and their password data by email (case-insensitive).
        /// Returns null if no such user exists.
        /// </summary>
        public static LoginRecord GetLoginByEmail(string email)
        {
            return Database.QuerySingle<LoginRecord>(LoginSelect + " WHERE u.Email = @Email;", MapLogin, EmailParam(email));
        }

        /// <summary>
        /// Same as GetLoginByEmail but by user id. Used by Verify2FA, which only knows the
        /// pending user's id. Returns null if no such user exists.
        /// </summary>
        public static LoginRecord GetLoginById(string userId)
        {
            return Database.QuerySingle<LoginRecord>(LoginSelect + " WHERE u.UserId = @UserId;", MapLogin, UserIdParam(userId));
        }



        /// <summary>True if any account already uses this email (case-insensitive).</summary>
        public static bool EmailExists(string email)
        {
            var result = Database.ExecuteScalar(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email) THEN 1 ELSE 0 END;",
                EmailParam(email));
            return Convert.ToInt32(result) == 1;
        }

        /// <summary>
        /// Creates the user and their credentials in one transaction, so there is never a
        /// user without a password. Returns false if the email is already registered
        /// </summary>
        public static bool CreateUser(NewUser user, passHasher.PasswordHashResult password)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (password == null) throw new ArgumentNullException(nameof(password));

            try
            {
                Database.InTransaction((conn, tx) =>
                {
                    Database.ExecuteNonQuery(conn, tx, @"
                    INSERT INTO dbo.Users (UserId, Email, FirstName, LastName, Role, University,
                                           ConsentAcceptedAtUtc, ConsentIp)
                    VALUES (@UserId, @Email, @FirstName, @LastName, @Role, @University,
                            @ConsentAt, @ConsentIp);",
                        UserIdParam(user.UserId),
                        EmailParam(user.Email),
                        Database.Param("@FirstName", SqlDbType.NVarChar, 100, (user.FirstName ?? "").Trim()),
                        Database.Param("@LastName", SqlDbType.NVarChar, 100, (user.LastName ?? "").Trim()),
                        Database.Param("@Role", SqlDbType.VarChar, 20, (user.Role ?? "").Trim()),
                        Database.Param("@University", SqlDbType.NVarChar, 200, NullIfBlank(user.University)),
                        Database.Param("@ConsentAt", SqlDbType.DateTime2, user.ConsentAcceptedAtUtc),
                        Database.Param("@ConsentIp", SqlDbType.VarChar, 45, NullIfBlank(user.ConsentIp)));

                    Database.ExecuteNonQuery(conn, tx, @"
                    INSERT INTO dbo.UserCredentials (UserId, PasswordHash, PasswordSalt, Algorithm,
                                                     Argon2MemoryKiB, Argon2Iterations, Argon2Parallelism)
                    VALUES (@UserId, @Hash, @Salt, @Algo, @M, @T, @P);",
                        PasswordParams(user.UserId, password));
                });
                return true;
            }
            catch (SqlException ex) when (Database.IsUniqueViolation(ex))
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces a user's password hash. Used when upgrading PBKDF2 hashes to Argon2
        /// at login, and for password changes/resets.
        /// </summary>
        public static void UpdatePasswordHash(string userId, passHasher.PasswordHashResult password, bool passwordChanged = true)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var parameters = new System.Collections.Generic.List<SqlParameter>(PasswordParams(userId, password))
            {
                Database.Param("@Changed", SqlDbType.Bit, passwordChanged)
            };


            Database.ExecuteNonQuery(@"
            UPDATE dbo.UserCredentials
               SET PasswordHash = @Hash, PasswordSalt = @Salt, Algorithm = @Algo,
                   Argon2MemoryKiB = @M, Argon2Iterations = @T, Argon2Parallelism = @P,
                   PasswordUpdatedAtUtc = CASE WHEN @Changed = 1 THEN SYSUTCDATETIME() ELSE PasswordUpdatedAtUtc END
             WHERE UserId = @UserId;",

                parameters.ToArray());
        }

        /// <summary>
        /// The user's current password stamp (same format as LoginRecord.PasswordStamp),
        /// or null if the user doesn't exist. Pages compare this to Session["PasswordStamp"]
        /// to end sessions that started before a password reset.
        /// </summary>
        public static string GetPasswordStamp(string userId)
        {
            var value = Database.ExecuteScalar(
                "SELECT PasswordUpdatedAtUtc FROM dbo.UserCredentials WHERE UserId = @UserId;",
                UserIdParam(userId));
            if (value == null) return null;
            return FormatStamp(DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc));
        }

        private static string FormatStamp(DateTime? utc)
        {
            return utc.HasValue
                ? utc.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture)
                : "";
        }


        /// <summary>
        /// Records a failed login in a single UPDATE, so simultaneous attempts can't be undercounted.
        /// When the count reaches <paramref maxAttempts/>,
        /// the account is locked for <paramref lockoutDuration/>. A failure after an
        /// earlier lockout has expired starts counting from 1 again.
        /// Returns the lockout end time if the account is now locked, otherwise null.
        /// </summary>
        public static DateTime? RecordFailedLogin(string userId, int maxAttempts, TimeSpan lockoutDuration)
        {
            const string sql = @"
            DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

            UPDATE dbo.UserCredentials
            SET FailedLoginCount = CASE WHEN LockoutUntilUtc IS NOT NULL AND LockoutUntilUtc <= @Now THEN 1
                ELSE FailedLoginCount + 1 END,
            LockoutUntilUtc ='CASE WHEN (CASE WHEN LockoutUntilUtc IS NOT NULL AND LockoutUntilUtc <= @Now THEN 1
                ELSE FailedLoginCount + 1 END) >= @Max
            THEN DATEADD(SECOND, @LockSeconds, @Now)
            WHEN LockoutUntilUtc <= @Now THEN NULL
                ELSE LockoutUntilUtc END, LastFailedLoginUtc = @Now 
            OUTPUT inserted.LockoutUntilUtc WHERE UserId = @UserId;";

            var result = Database.ExecuteScalar(sql,
                UserIdParam(userId),
                Database.Param("@Max", SqlDbType.Int, maxAttempts),
                Database.Param("@LockSeconds", SqlDbType.Int, (int)lockoutDuration.TotalSeconds));

            if (result == null) return null;
            var until = DateTime.SpecifyKind((DateTime)result, DateTimeKind.Utc);
            return until > DateTime.UtcNow ? until : (DateTime?)null;
        }

        /// <summary> 
        /// Clears the failure counter and any lockout after a successful login
        /// </summary>
        public static void ResetFailedLogins(string userId)
        {
            Database.ExecuteNonQuery(@"
            UPDATE dbo.UserCredentials
               SET FailedLoginCount = 0, LockoutUntilUtc = NULL
            WHERE UserId = @UserId AND (FailedLoginCount <> 0 OR LockoutUntilUtc IS NOT NULL);",
                UserIdParam(userId));
        }

        private static readonly string[] MfaSecretPurposes = { "FIA", "MFA-TOTP-Secret" };

        /// <summary>Encrypts a Base32 TOTP secret for storage.</summary>
        public static string ProtectMfaSecret(string base32Secret)
        {
            var bytes = MachineKey.Protect(Encoding.UTF8.GetBytes(base32Secret), MfaSecretPurposes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decrypts a stored secret back to Base32, or returns null if it can't be decrypted
        /// Used to verify a TOTP code or to generate backup codes.
        /// </summary>
        public static string UnprotectMfaSecret(string protectedSecret)
        {
            if (string.IsNullOrWhiteSpace(protectedSecret)) return null;
            try
            {
                var bytes = MachineKey.Unprotect(Convert.FromBase64String(protectedSecret), MfaSecretPurposes);
                return bytes == null ? null : Encoding.UTF8.GetString(bytes);
            }
            catch (CryptographicException) { return null; }
            catch (FormatException) { return null; }
        }

        /// <summary>
        /// SHA-256 of a backup code after removing dashes/spaces and uppercasing,
        /// </summary>
        public static byte[] HashBackupCode(string code)
        {
            var normalized = (code ?? "").Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            }
        }

        public static bool IsMfaEnabled(string userId)
        {
            var v = Database.ExecuteScalar(
                "SELECT MfaEnabled FROM dbo.UserCredentials WHERE UserId = @UserId;",
                UserIdParam(userId));
            return v != null && (bool)v;
        }

        /// <summary>
        /// Turns MFA on. saves the encrypted secret and replaces any old backup codes in one query. The backup codes are stored as SHA-256 hashes.
        /// </summary>
        public static void EnableMfa(string userId, string protectedSecret, IEnumerable<byte[]> backupCodeHashes)
        {
            if (string.IsNullOrWhiteSpace(protectedSecret))
                throw new ArgumentException("Secret is required.", nameof(protectedSecret));

            Database.InTransaction((conn, tx) =>
            {
                var rows = Database.ExecuteNonQuery(conn, tx, @"
                    UPDATE dbo.UserCredentials
                       SET MfaEnabled = 1, MfaSecret = @S, MfaLastTimeStep = NULL
                     WHERE UserId = @UserId;",
                    UserIdParam(userId),
                    Database.Param("@S", SqlDbType.NVarChar, 512, protectedSecret));

                if (rows != 1) throw new InvalidOperationException("User account could not be found.");

                Database.ExecuteNonQuery(conn, tx,
                    "DELETE FROM dbo.UserMfaBackupCodes WHERE UserId = @UserId;", UserIdParam(userId));

                foreach (var hash in backupCodeHashes ?? new byte[0][])
                {
                    Database.ExecuteNonQuery(conn, tx,
                        "INSERT INTO dbo.UserMfaBackupCodes (UserId, CodeHash) VALUES (@UserId, @H);",
                        UserIdParam(userId),
                        Database.Param("@H", SqlDbType.VarBinary, 32, hash));
                }
            });
        }

        /// <summary>
        /// Records that the authenticator code for this 30-second time step was used.
        /// Returns false if this step (or a later one) was already used, which stops someone
        /// who saw a code from reusing it in the same window
        /// </summary>
        public static bool TryAcceptTotpTimeStep(string userId, long timeStep)
        {
            var rows = Database.ExecuteNonQuery(@"
                UPDATE dbo.UserCredentials
                   SET MfaLastTimeStep = @Step
                 WHERE UserId = @UserId AND (MfaLastTimeStep IS NULL OR MfaLastTimeStep < @Step);",
                UserIdParam(userId),
                Database.Param("@Step", SqlDbType.BigInt, timeStep));
            return rows == 1;
        }

        /// <summary>
        /// Uses a backup code if it's valid and unused, marking it used in one UPDATE so
        /// two simultaneous attempts can't both succeed. Returns true if the code was accepted.
        /// </summary>
        public static bool TryUseBackupCode(string userId, string code)
        {
            var rows = Database.ExecuteNonQuery(@"
                UPDATE TOP (1) dbo.UserMfaBackupCodes
                   SET UsedAtUtc = SYSUTCDATETIME()
                 WHERE UserId = @UserId AND CodeHash = @H AND UsedAtUtc IS NULL;",
                UserIdParam(userId),
                Database.Param("@H", SqlDbType.VarBinary, 32, HashBackupCode(code)));
            return rows == 1;
        }

        /// <summary>Turns MFA off and deletes the secret and backup codes.</summary>
        public static void DisableMfa(string userId)
        {
            Database.InTransaction((conn, tx) =>
            {
                Database.ExecuteNonQuery(conn, tx,
                    "DELETE FROM dbo.UserMfaBackupCodes WHERE UserId = @UserId;", UserIdParam(userId));
                Database.ExecuteNonQuery(conn, tx, @"
                    UPDATE dbo.UserCredentials SET MfaEnabled = 0, MfaSecret = NULL, MfaLastTimeStep = NULL
                     WHERE UserId = @UserId;", UserIdParam(userId));
            });
        }


        /// <summary>
        /// These are helpers for the querys above. Parameterized queries are used to avoid SQL injection, and these helpers ensure consistent casing and trimming of inputs.
        /// </summary>

        private static SqlParameter EmailParam(string email)
        {
            return Database.Param("@Email", SqlDbType.NVarChar, 256, (email ?? "").Trim().ToLowerInvariant());
        }

        private static SqlParameter UserIdParam(string userId)
        {
            return Database.Param("@UserId", SqlDbType.Char, 32, (userId ?? "").Trim().ToLowerInvariant());
        }

        private static SqlParameter[] PasswordParams(string userId, passHasher.PasswordHashResult pw)
        {
            return new[]
            {
                UserIdParam(userId),
                Database.Param("@Hash", SqlDbType.VarBinary, 64, pw.Hash),
                Database.Param("@Salt", SqlDbType.VarBinary, 64, pw.Salt),
                Database.Param("@Algo", SqlDbType.VarChar, 20, pw.Algorithm),
                Database.Param("@M", SqlDbType.Int, pw.MemoryKiB),
                Database.Param("@T", SqlDbType.Int, pw.Iterations),
                Database.Param("@P", SqlDbType.Int, pw.Parallelism)
            };
        }

        private static string NullIfBlank(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}