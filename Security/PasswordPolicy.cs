using System;

namespace CyberApp_FIA
{
    /// <summary>
    /// One set of password rules for every place a password is created or changed
    /// (sign-up, reset link, SuperAdmin reset, accounts created by admins).
    ///
    /// Follows current guidance (NIST 800-63B / OWASP): length matters most, so we ask for a long
    /// password and reject well-known weak ones, but do not force symbols or digits.
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 12;
        public const int MaxLength = 128;   // stops enormous inputs from making the slow hash a denial-of-service tool

        private static readonly string[] CommonPasswords =
        {
            "password", "password1", "password12", "password123", "password1234", "password12345",
            "passw0rd1234", "letmein12345", "welcome12345", "iloveyou1234", "administrator",
            "qwertyuiop12", "qwertyuiopas", "1234567890ab", "123456789012", "1234567890123",
            "abcdefghijkl", "aaaaaaaaaaaa", "000000000000", "111111111111", "changeme1234",
            "feminineintelligenceagency"
        };

        /// <summary>
        /// Returns null when the password is acceptable, otherwise a message to show the user.
        /// </summary>
        /// <param name="password">The password they typed.</param>
        /// <param name="email">Optional: their email, so it can't be used as the password.</param>
        public static string Problem(string password, string email = null)
        {
            password = password ?? "";

            if (password.Length < MinLength)
                return "Your password must be at least " + MinLength + " characters. A few random words works well.";

            if (password.Length > MaxLength)
                return "Your password can be at most " + MaxLength + " characters.";

            var lower = password.ToLowerInvariant();
            foreach (var weak in CommonPasswords)
            {
                if (lower == weak) return "That password is too common. Please choose something less guessable.";
            }

            if (IsOneRepeatedCharacter(password))
                return "That password is too easy to guess. Please use a mix of different characters or words.";

            if (!string.IsNullOrWhiteSpace(email))
            {
                var address = email.Trim().ToLowerInvariant();
                var name = address.Contains("@") ? address.Substring(0, address.IndexOf('@')) : address;
                if (lower == address || (name.Length >= 4 && lower.Contains(name)))
                    return "Your password can't contain your email address.";
            }

            return null;
        }

        private static bool IsOneRepeatedCharacter(string text)
        {
            for (int i = 1; i < text.Length; i++)
            {
                if (text[i] != text[0]) return false;
            }
            return text.Length > 0;
        }
    }
}
