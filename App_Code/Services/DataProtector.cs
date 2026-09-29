using System;
using System.Text;
using System.Web.Security;

namespace CyberApp_FIA.Services
{
    /// Epic #7: encrypts sensitive text before it's saved to XML,
    /// and decrypts it when it's read back.
    ///
    /// Uses ASP.NET's built-in MachineKey.Protect (AES encryption + HMAC-SHA256
    /// tamper check), so we never write our own encryption.
    ///
    /// Encrypted values are stored as "enc1:" + Base64. Anything without that
    /// prefix is treated as old plain text, so data saved before Piece 5 still displays.
    public static class DataProtector
    {
        // Marks a value as encrypted (and which version of this scheme made it).
        private const string Prefix = "enc1:";

        // "Purpose" string: data encrypted for this purpose can only be decrypted
        // with the same purpose, so it can't be mixed up with other protected data.
        private const string Purpose = "CyberApp_FIA.SensitiveData.v1";

        /// Encrypts text for storage. Empty text stays empty.</summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText ?? string.Empty;

            var bytes = Encoding.UTF8.GetBytes(plainText);            // text -> bytes
            var protectedBytes = MachineKey.Protect(bytes, Purpose);  // encrypt + sign
            return Prefix + Convert.ToBase64String(protectedBytes);   // bytes -> storable text
        }

        /// Decrypts text read from storage. Old unencrypted text is returned as-is.
        /// If the value was tampered with or encrypted with a different key,
        /// a placeholder is shown instead of crashing the page.
        public static string Decrypt(string storedText)
        {
            if (string.IsNullOrEmpty(storedText)) return storedText ?? string.Empty;

            // No prefix = saved before encryption existed, so it's plain text.
            if (!storedText.StartsWith(Prefix, StringComparison.Ordinal)) return storedText;

            try
            {
                var protectedBytes = Convert.FromBase64String(storedText.Substring(Prefix.Length));
                var bytes = MachineKey.Unprotect(protectedBytes, Purpose);   // verify + decrypt
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                // Wrong key (e.g. data encrypted on a teammate's machine) or tampered data.
                return "[This message could not be decrypted.]";
            }
        }
    }
}