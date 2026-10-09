using System;
using System.Data;
using System.Data.SqlClient;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Everything sign-up and login need from the Users and UserCredentials tables.
    /// Pages call these methods, they never write SQL themselves.
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

        /// <summary>
        /// Looks up a user and their password data by email.
        /// Returns null if no such user exists.
        /// </summary>
        public static LoginRecord GetLoginByEmail(string email)
        {
            const string sql = @"
            SELECT u.UserId, u.Email, u.FirstName, u.Role, u.University,
                   c.PasswordHash, c.PasswordSalt, c.Algorithm,
                   c.Argon2MemoryKiB, c.Argon2Iterations, c.Argon2Parallelism,
                   c.FailedLoginCount, c.LockoutUntilUtc
             FROM dbo.Users u
             JOIN dbo.UserCredentials c ON c.UserId = u.UserId
             WHERE u.Email = @Email;";

            return Database.QuerySingle(sql, r => new LoginRecord
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
                LockoutUntilUtc = Database.GetNullableDateTime(r, "LockoutUntilUtc")
            },
            EmailParam(email));
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
        public static void UpdatePasswordHash(string userId, passHasher.PasswordHashResult password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            Database.ExecuteNonQuery(@"
            UPDATE dbo.UserCredentials
            SET PasswordHash = @Hash, PasswordSalt = @Salt, Algorithm = @Algo,
                   Argon2MemoryKiB = @M, Argon2Iterations = @T, Argon2Parallelism = @P,
                   PasswordUpdatedAtUtc = SYSUTCDATETIME()
            WHERE UserId = @UserId;",
                PasswordParams(userId, password));
        }

        /// <summary>
        /// Records a failed login in a single atomic UPDATE, so simultaneous attempts
        /// can't be undercounted. When the count reaches <paramref name="maxAttempts"/>,
        /// the account is locked for <paramref name="lockoutDuration"/>. A failure after an
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

        /// <summary>Clears the failure counter and any lockout after a successful login.</summary>
        public static void ResetFailedLogins(string userId)
        {
            Database.ExecuteNonQuery(@"
            UPDATE dbo.UserCredentials
               SET FailedLoginCount = 0, LockoutUntilUtc = NULL
            WHERE UserId = @UserId AND (FailedLoginCount <> 0 OR LockoutUntilUtc IS NOT NULL);",
                UserIdParam(userId));
        }

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