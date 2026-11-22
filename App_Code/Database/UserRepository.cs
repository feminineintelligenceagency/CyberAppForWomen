using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Xml.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Drawing;
using System.Drawing.Drawing2D;

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

        /// <summary>One audit log entry.</summary>
        public sealed class AuditEntry
        {
            public string LogId { get; set; }
            public DateTime TimestampUtc { get; set; }
            public string Type { get; set; }
            public string UserId { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string Role { get; set; }
            public string University { get; set; }
            public string Details { get; set; }
        }


        /// <summary>One certificate.</summary>
        public sealed class Certificate
        {
            public string VerificationCode { get; set; }
            public string UserId { get; set; }
            public string CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string RecipientName { get; set; }
            public DateTime IssuedAtUtc { get; set; }
        }

        /// <summary>One participant badge.</summary>
        public sealed class Badge
        {
            public string UserId { get; set; }
            public string CourseId { get; set; }
            public string CourseTitle { get; set; }
            public string ImagePath { get; set; }
            public DateTime AwardedAtUtc { get; set; }
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
        /// Permanently deletes a user's account and everything the database stores about them.
        ///
        /// Deleting the dbo.Users row automatically removes:
        ///   UserCredentials -> UserMfaBackupCodes, PasswordResetTokens ->
        ///   QuizProgress -> QuizProgressAnswers, QuizResults -> QuizResultDomainScores, QuizResultItems
        ///
        /// Audit log entries are kept but email and first name are removed.
        /// either everything is deleted or nothing is. Returns false if the user didn't exist.
        /// </summary>
        public static bool DeleteUser(string userId)
        {
            return Database.InTransaction((conn, tx) =>
            {
                // Get the email first. some audit entries only have the email.
                var email = Database.ExecuteScalar(conn, tx,
                    "SELECT Email FROM dbo.Users WITH (UPDLOCK) WHERE UserId = @UserId;",
                    UserIdParam(userId)) as string;

                if (email == null) return false; 

                // Anonymize the audit trail instead of deleting it.
                Database.ExecuteNonQuery(conn, tx, @"
                    UPDATE dbo.AuditLog
                       SET Email = NULL, FirstName = NULL
                     WHERE UserId = @UserId OR Email = @Email;",
                    UserIdParam(userId),
                    EmailParam(email));

                // Delete the account. foreign keys cascade to all tables.
                var rows = Database.ExecuteNonQuery(conn, tx,
                    "DELETE FROM dbo.Users WHERE UserId = @UserId;",
                    UserIdParam(userId));

                return rows == 1;
            });
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
               SET FailedLoginCount =
                       CASE WHEN LockoutUntilUtc IS NOT NULL AND LockoutUntilUtc <= @Now THEN 1
                            ELSE FailedLoginCount + 1 END,
                   LockoutUntilUtc =
                       CASE WHEN (CASE WHEN LockoutUntilUtc IS NOT NULL AND LockoutUntilUtc <= @Now THEN 1
                                       ELSE FailedLoginCount + 1 END) >= @Max
                                 THEN DATEADD(SECOND, @LockSeconds, @Now)
                            WHEN LockoutUntilUtc <= @Now THEN NULL
                            ELSE LockoutUntilUtc END,
                   LastFailedLoginUtc = @Now
            OUTPUT inserted.LockoutUntilUtc
             WHERE UserId = @UserId;";

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

        //Database audit stuff

        /// <summary>
        /// Adds an audit entry. Best-effort: returns false (and never throws) if it couldn't be saved,
        /// so audit logging can never break sign-in, sign-up or any other action.
        /// </summary>
        /// <param name="type">e.g. "Sign In", "Sign In Failed (Bad Password)", "Account Created".</param>
        public static bool AppendAudit(string type, string details,
                                       string userId = null, string email = null, string firstName = null,
                                       string role = null, string university = null)
        {
            try
            {
                Database.ExecuteNonQuery(@"
                    INSERT INTO dbo.AuditLog (LogId, TimestampUtc, Type, UserId, Email, FirstName, Role, University, Details)
                    VALUES (@Id, SYSUTCDATETIME(), @Type, @AuditUserId, @AuditEmail, @First, @Role, @Uni, @Details);",
                    Database.Param("@Id", SqlDbType.VarChar, 40, "log-" + Guid.NewGuid().ToString("N")),
                    Database.Param("@Type", SqlDbType.NVarChar, 100, Truncate(type, 100) ?? "Unknown"),
                    Database.Param("@AuditUserId", SqlDbType.Char, 32, AuditUserIdOrNull(userId)),
                    Database.Param("@AuditEmail", SqlDbType.NVarChar, 256, Truncate(NullIfBlank(email)?.ToLowerInvariant(), 256)),
                    Database.Param("@First", SqlDbType.NVarChar, 100, Truncate(NullIfBlank(firstName), 100)),
                    Database.Param("@Role", SqlDbType.VarChar, 30, Truncate(NullIfBlank(role) ?? "Unknown", 30)),
                    Database.Param("@Uni", SqlDbType.NVarChar, 200, Truncate(NullIfBlank(university), 200)),
                    Database.Param("@Details", SqlDbType.NVarChar, -1, details));
                return true;
            }
            catch (Exception ex)
            {
                // Never let audit logging break the action being logged.
                System.Diagnostics.Trace.TraceError("UserRepository.AppendAudit failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Most recent audit entries, newest first. Optional filters:
        /// university (UniversityAdmin view), sinceUtc, and type.
        /// </summary>
        public static List<AuditEntry> GetRecentAudit(int top = 200, string university = null,
                                                      DateTime? sinceUtc = null, string type = null)
        {
            var sql = new StringBuilder(@"
                SELECT TOP (@Top) LogId, TimestampUtc, Type, UserId, Email, FirstName, Role, University, Details
                  FROM dbo.AuditLog
                 WHERE 1 = 1");
            var p = new List<SqlParameter> { Database.Param("@Top", SqlDbType.Int, Math.Max(1, Math.Min(top, 5000))) };

            if (!string.IsNullOrWhiteSpace(university))
            {
                sql.Append(" AND University = @Uni");
                p.Add(Database.Param("@Uni", SqlDbType.NVarChar, 200, university.Trim()));
            }
            if (sinceUtc.HasValue)
            {
                sql.Append(" AND TimestampUtc > @Since");
                p.Add(Database.Param("@Since", SqlDbType.DateTime2, sinceUtc.Value));
            }
            if (!string.IsNullOrWhiteSpace(type))
            {
                sql.Append(" AND Type = @Type");
                p.Add(Database.Param("@Type", SqlDbType.NVarChar, 100, type.Trim()));
            }
            sql.Append(" ORDER BY TimestampUtc DESC;");

            return Database.Query(sql.ToString(), MapAudit, p.ToArray());
        }

        /// <summary>
        /// How many entries of the given types happened after sinceUtc
        /// (all types if none are given). Used for the Super Admin "new critical activity" alert.
        /// </summary>
        public static int CountAuditSince(DateTime sinceUtc, params string[] types)
        {
            var sql = new StringBuilder("SELECT COUNT(*) FROM dbo.AuditLog WHERE TimestampUtc > @Since");
            var p = new List<SqlParameter> { Database.Param("@Since", SqlDbType.DateTime2, sinceUtc) };

            if (types != null && types.Length > 0)
            {
                sql.Append(" AND Type IN (");
                for (int i = 0; i < types.Length; i++)
                {
                    if (i > 0) sql.Append(", ");
                    sql.Append("@T").Append(i);
                    p.Add(Database.Param("@T" + i, SqlDbType.NVarChar, 100, types[i]));
                }
                sql.Append(")");
            }
            sql.Append(";");

            return Convert.ToInt32(Database.ExecuteScalar(sql.ToString(), p.ToArray()));
        }

        private static AuditEntry MapAudit(SqlDataReader r)
        {
            return new AuditEntry
            {
                LogId = r.GetString(r.GetOrdinal("LogId")),
                TimestampUtc = DateTime.SpecifyKind(r.GetDateTime(r.GetOrdinal("TimestampUtc")), DateTimeKind.Utc),
                Type = r.GetString(r.GetOrdinal("Type")),
                UserId = Database.GetStringOrNull(r, "UserId"),
                Email = Database.GetStringOrNull(r, "Email"),
                FirstName = Database.GetStringOrNull(r, "FirstName"),
                Role = Database.GetStringOrNull(r, "Role"),
                University = Database.GetStringOrNull(r, "University"),
                Details = Database.GetStringOrNull(r, "Details")
            };
        }

        /// <summary>Only real 32-character user ids are stored; anything else becomes NULL.</summary>
        private static string AuditUserIdOrNull(string userId)
        {
            var id = (userId ?? "").Trim().ToLowerInvariant();
            return id.Length == 32 ? id : null;
        }

        private static string Truncate(string s, int max)
        {
            if (s == null) return null;
            return s.Length <= max ? s : s.Substring(0, max);
        }

        // certificate helpers
        public static Certificate IssueCertificate(string userId, string courseId, string courseTitle)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user id is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(courseId)) throw new ArgumentException("A course id is required.", nameof(courseId));
            if (string.IsNullOrWhiteSpace(courseTitle)) throw new ArgumentException("A course title is required.", nameof(courseTitle));

            var existing = GetCertificateForUserAndCourse(userId, courseId);
            if (existing != null) return existing;

            // A random code could already exist so retry a few times.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var code = NewCertificateCode();
                try
                {
                    var rows = Database.ExecuteNonQuery(@"
                        INSERT INTO dbo.Certificates (VerificationCode, UserId, CourseId, CourseTitle, RecipientName)
                        SELECT @Code, u.UserId, @CourseId, @Title, LTRIM(RTRIM(u.FirstName + N' ' + u.LastName))
                          FROM dbo.Users u
                         WHERE u.UserId = @UserId;",
                        Database.Param("@Code", SqlDbType.Char, 18, code),
                        UserIdParam(userId),
                        Database.Param("@CourseId", SqlDbType.NVarChar, 100, courseId.Trim()),
                        Database.Param("@Title", SqlDbType.NVarChar, 200, courseTitle.Trim()));

                    if (rows == 0) return null;   // no such user
                    return GetCertificateByCode(code);
                }
                catch (SqlException ex) when (Database.IsUniqueViolation(ex))
                {
                    // Either the code was taken or the same course was certified at the same moment by another request.
                    existing = GetCertificateForUserAndCourse(userId, courseId);
                    if (existing != null) return existing;
                }
            }

            throw new InvalidOperationException("Couldn't create a unique certificate code.");
        }

        /// <summary>Finds a certificate by its verification code.</summary>
        public static Certificate GetCertificateByCode(string verificationCode)
        {
            var code = NormalizeCertificateCode(verificationCode);
            if (code == null) return null;

            return Database.QuerySingle(CertificateSelectSql + " WHERE VerificationCode = @Code;", MapCertificate,
                Database.Param("@Code", SqlDbType.Char, 18, code));
        }

        /// <summary>All of a user's certificates, newest first.</summary>
        public static List<Certificate> GetCertificatesForUser(string userId)
        {
            return Database.Query(CertificateSelectSql + " WHERE UserId = @UserId ORDER BY IssuedAtUtc DESC;", MapCertificate,
                UserIdParam(userId));
        }

        /// <summary>The user's certificate for one course, or null.</summary>
        public static Certificate GetCertificateForUserAndCourse(string userId, string courseId)
        {
            return Database.QuerySingle(CertificateSelectSql + " WHERE UserId = @UserId AND CourseId = @CourseId;", MapCertificate,
                UserIdParam(userId),
                Database.Param("@CourseId", SqlDbType.NVarChar, 100, (courseId ?? "").Trim()));
        }

        // PDF Generation

        /// <summary>Draws the certificate as a one-page landscape PDF and returns the file bytes.</summary>
        public static byte[] CreateCertificatePdf(Certificate cert)
        {
            if (cert == null) throw new ArgumentNullException(nameof(cert));

            using (var doc = new PdfDocument())
            {
                doc.Info.Title = "Certificate of Completion";
                doc.Info.Author = "Feminine Intelligence Agency";

                var page = doc.AddPage();
                page.Size = PdfSharp.PageSize.Letter;
                page.Orientation = PdfSharp.PageOrientation.Landscape;

                using (var g = XGraphics.FromPdfPage(page))
                {
                    double w = page.Width.Point, h = page.Height.Point;

                    var pink = XColor.FromArgb(240, 106, 169);
                    var blue = XColor.FromArgb(42, 153, 219);
                    var teal = XColor.FromArgb(69, 195, 179);
                    var ink = XColor.FromArgb(28, 28, 28);
                    var muted = XColor.FromArgb(107, 114, 128);

                    // Border
                    var frame = new XRect(24, 24, w - 48, h - 48);
                    g.DrawRectangle(new XLinearGradientBrush(frame.TopLeft, frame.BottomRight, pink, blue), frame);
                    g.DrawRectangle(XBrushes.White, new XRect(34, 34, w - 68, h - 68));
                    g.DrawRectangle(new XPen(teal, 1), new XRect(44, 44, w - 88, h - 88));

                    // FIA badge
                    var badge = new XRect(w / 2 - 28, 62, 56, 56);
                    g.DrawRoundedRectangle(new XLinearGradientBrush(badge.TopLeft, badge.BottomRight, pink, blue),
                                           badge, new XSize(16, 16));
                    g.DrawString("FIA", new XFont("Arial", 18, XFontStyle.Bold), XBrushes.White, badge, XStringFormats.Center);

                    // Text
                    var center = XStringFormats.TopCenter;
                    g.DrawString("FEMININE INTELLIGENCE AGENCY", new XFont("Arial", 12, XFontStyle.Bold),
                                 new XSolidBrush(muted), new XRect(0, 132, w, 20), center);

                    g.DrawString("Certificate of Completion", new XFont("Georgia", 36, XFontStyle.Bold),
                                 new XSolidBrush(ink), new XRect(0, 165, w, 50), center);

                    g.DrawString("This certifies that", new XFont("Arial", 14, XFontStyle.Regular),
                                 new XSolidBrush(muted), new XRect(0, 235, w, 20), center);

                    g.DrawString(cert.RecipientName, new XFont("Georgia", 30, XFontStyle.Bold),
                                 new XSolidBrush(blue), new XRect(0, 262, w, 44), center);
                    g.DrawLine(new XPen(teal, 1.5), w / 2 - 190, 312, w / 2 + 190, 312);

                    g.DrawString("has successfully completed", new XFont("Arial", 14, XFontStyle.Regular),
                                 new XSolidBrush(muted), new XRect(0, 326, w, 20), center);

                    g.DrawString(cert.CourseTitle, new XFont("Arial", 22, XFontStyle.Bold),
                                 new XSolidBrush(ink), new XRect(60, 354, w - 120, 34), center);

                    // Bottom row: date (left) and verification code (right)
                    var small = new XFont("Arial", 11, XFontStyle.Regular);
                    var smallBold = new XFont("Arial", 13, XFontStyle.Bold);

                    g.DrawString("Date of completion", small, new XSolidBrush(muted), new XRect(90, h - 128, 240, 16), center);
                    g.DrawString(cert.IssuedAtUtc.ToString("MMMM d, yyyy"), smallBold, new XSolidBrush(ink),
                                 new XRect(90, h - 110, 240, 18), center);

                    g.DrawString("Verification number", small, new XSolidBrush(muted), new XRect(w - 330, h - 128, 240, 16), center);
                    g.DrawString(cert.VerificationCode, smallBold, new XSolidBrush(ink),
                                 new XRect(w - 330, h - 110, 240, 18), center);

                    g.DrawString("Employers can confirm this certificate with the verification number.",
                                 new XFont("Arial", 9, XFontStyle.Italic), new XSolidBrush(muted),
                                 new XRect(0, h - 76, w, 14), center);
                }

                using (var ms = new MemoryStream())
                {
                    doc.Save(ms, false);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Bottom line of the certificate
        /// </summary>
        private static string CertificateVerifyLine()
        {
            var baseUrl = (System.Configuration.ConfigurationManager.AppSettings["Notifications:BaseUrl"] ?? "").Trim();
            if (string.IsNullOrEmpty(baseUrl))
                return "Employers can confirm this certificate with the verification number.";

            return "Verify this certificate at " + baseUrl.TrimEnd('/') + "/VerifyCertificate.aspx";
        }

        /// <summary>Safe download filename, e.g. "FIA-Certificate-FIA-7KQ2-M9XD-P4TR.pdf".</summary>
        public static string CertificatePdfFileName(Certificate cert) => "FIA-Certificate-" + cert.VerificationCode + ".pdf";

        // certificate helpers 

        private const string CertificateSelectSql = @"
        SELECT VerificationCode, UserId, CourseId, CourseTitle, RecipientName, IssuedAtUtc
          FROM dbo.Certificates";

        // Only numbers and letters allowed
        private const string CertificateCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Random code like FIA-7KQ2-M9XD-P4TR (12 random characters, about 10^17 possibilities).</summary>
        private static string NewCertificateCode()
        {
            var chars = new char[12];
            var bytes = new byte[1];
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    // Rejection sampling so every character is equally likely.
                    int limit = 256 - (256 % CertificateCodeAlphabet.Length);
                    do { rng.GetBytes(bytes); } while (bytes[0] >= limit);
                    chars[i] = CertificateCodeAlphabet[bytes[0] % CertificateCodeAlphabet.Length];
                }
            }
            var s = new string(chars);
            return "FIA-" + s.Substring(0, 4) + "-" + s.Substring(4, 4) + "-" + s.Substring(8, 4);
        }

        /// <summary>
        /// Accepts what a person might type ("fia 7kq2 m9xd p4tr", "7KQ2M9XDP4TR") and returns
        /// FIA-7KQ2-M9XD-P4TR, or null if it can't be a valid code.
        /// </summary>
        public static string NormalizeCertificateCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var raw = new System.Text.StringBuilder();
            foreach (var c in input.ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(c)) raw.Append(c);
            }
            var s = raw.ToString();
            if (s.StartsWith("FIA")) s = s.Substring(3);
            if (s.Length != 12) return null;
            foreach (var c in s)
            {
                if (CertificateCodeAlphabet.IndexOf(c) < 0) return null;
            }
            return "FIA-" + s.Substring(0, 4) + "-" + s.Substring(4, 4) + "-" + s.Substring(8, 4);
        }

        private static Certificate MapCertificate(SqlDataReader r)
        {
            return new Certificate
            {
                VerificationCode = r.GetString(r.GetOrdinal("VerificationCode")),
                UserId = r.GetString(r.GetOrdinal("UserId")),
                CourseId = r.GetString(r.GetOrdinal("CourseId")),
                CourseTitle = r.GetString(r.GetOrdinal("CourseTitle")),
                RecipientName = r.GetString(r.GetOrdinal("RecipientName")),
                IssuedAtUtc = DateTime.SpecifyKind(r.GetDateTime(r.GetOrdinal("IssuedAtUtc")), DateTimeKind.Utc)
            };
        }

        //Badges

        /// <summary>
        /// Awards a badge for a course (does nothing if the user already has that badge).
        /// With issueCertificate = true (the default), the course certificate is created in the
        /// same transaction, unless the user already has one.
        /// Returns true if a NEW badge was awarded; false if they already had it or the user doesn't exist.
        /// </summary>
        public static bool AwardBadge(string userId, string courseId, string courseTitle,
                                      string imagePath = null, bool issueCertificate = true)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user id is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(courseId)) throw new ArgumentException("A course id is required.", nameof(courseId));
            if (string.IsNullOrWhiteSpace(courseTitle)) throw new ArgumentException("A course title is required.", nameof(courseTitle));

            var cid = courseId.Trim();
            var title = courseTitle.Trim();

            // A random certificate code could (very rarely) already exist, or another request could
            // award the same badge at the same moment. Both show up as unique-key errors: just retry,
            // and the NOT EXISTS checks below then skip whatever was already saved.
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    return Database.InTransaction((conn, tx) =>
                    {
                        var badgeRows = Database.ExecuteNonQuery(conn, tx, @"
                    INSERT INTO dbo.ParticipantBadges (UserId, CourseId, CourseTitle, ImagePath)
                    SELECT u.UserId, @CourseId, @Title, @Image
                      FROM dbo.Users u
                     WHERE u.UserId = @UserId
                       AND NOT EXISTS (SELECT 1 FROM dbo.ParticipantBadges b WITH (UPDLOCK, HOLDLOCK)
                    WHERE b.UserId = @UserId AND b.CourseId = @CourseId);",
                            UserIdParam(userId),
                            Database.Param("@CourseId", SqlDbType.NVarChar, 100, cid),
                            Database.Param("@Title", SqlDbType.NVarChar, 200, title),
                            Database.Param("@Image", SqlDbType.NVarChar, 400, NullIfBlank(imagePath)));

                        if (issueCertificate)
                        {
                            Database.ExecuteNonQuery(conn, tx, @"
                    INSERT INTO dbo.Certificates (VerificationCode, UserId, CourseId, CourseTitle, RecipientName)
                    SELECT @Code, u.UserId, @CourseId, @Title, LTRIM(RTRIM(u.FirstName + N' ' + u.LastName))
                      FROM dbo.Users u
                     WHERE u.UserId = @UserId
                       AND NOT EXISTS (SELECT 1 FROM dbo.Certificates c WITH (UPDLOCK, HOLDLOCK)
                    WHERE c.UserId = @UserId AND c.CourseId = @CourseId);",
                                Database.Param("@Code", SqlDbType.Char, 18, NewCertificateCode()),
                                UserIdParam(userId),
                                Database.Param("@CourseId", SqlDbType.NVarChar, 100, cid),
                                Database.Param("@Title", SqlDbType.NVarChar, 200, title));
                        }

                        return badgeRows == 1;
                    });
                }
                catch (SqlException ex) when (Database.IsUniqueViolation(ex))
                {
                    // Retry (see comment above).
                }
            }

            throw new InvalidOperationException("Couldn't save the badge.");
        }

        /// <summary>All of a participant's badges, newest first.</summary>
        public static List<Badge> GetBadgesForUser(string userId)
        {
            return Database.Query(@"
            SELECT UserId, CourseId, CourseTitle, ImagePath, AwardedAtUtc
              FROM dbo.ParticipantBadges
             WHERE UserId = @UserId
             ORDER BY AwardedAtUtc DESC;", MapBadge, UserIdParam(userId));
        }

        /// <summary>True if the participant has the badge for this course.</summary>
        public static bool HasBadge(string userId, string courseId)
        {
            return Convert.ToInt32(Database.ExecuteScalar(
                "SELECT COUNT(*) FROM dbo.ParticipantBadges WHERE UserId = @UserId AND CourseId = @CourseId;",
                UserIdParam(userId),
                Database.Param("@CourseId", SqlDbType.NVarChar, 100, (courseId ?? "").Trim()))) > 0;
        }

        private static Badge MapBadge(SqlDataReader r)
        {
            return new Badge
            {
                UserId = r.GetString(r.GetOrdinal("UserId")),
                CourseId = r.GetString(r.GetOrdinal("CourseId")),
                CourseTitle = r.GetString(r.GetOrdinal("CourseTitle")),
                ImagePath = Database.GetStringOrNull(r, "ImagePath"),
                AwardedAtUtc = DateTime.SpecifyKind(r.GetDateTime(r.GetOrdinal("AwardedAtUtc")), DateTimeKind.Utc)
            };
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