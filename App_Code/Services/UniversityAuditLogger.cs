using System;
using System.Web.UI;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Writes University Admin audit log entries (database version).
    /// Entries now go to dbo.AuditLog through UserRepository.AppendAudit instead of
    /// App_Data/Audit_Log/UnvAdminAudit.xml.
    ///
    /// AppendForCurrentUser keeps the same signature as the XML version, so every page
    /// that already calls it (Home, sign-in, 2FA, admin and helper creation...) switches
    /// to the database without any changes.
    ///
    /// Never throws: audit logging must not break the action being logged.
    /// </summary>
    public static class UniversityAuditLogger
    {
        /// <summary>
        /// Adds an entry for the signed-in user. Their id, email, role and university come
        /// from the session; their first name comes from the database.
        /// </summary>
        public static void AppendForCurrentUser(Page page, string type, string details)
        {
            try
            {
                var session = page?.Session;
                var userId = session?["UserId"] as string;
                var email = session?["Email"] as string;
                var role = session?["Role"] as string;
                var university = session?["University"] as string;
                string firstName = null;

                // The session doesn't store the first name, so look it up.
                // If that fails, still write the entry without it.
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    try
                    {
                        var user = UserRepository.GetLoginById(userId);
                        if (user != null)
                        {
                            firstName = user.FirstName;
                            if (string.IsNullOrWhiteSpace(email)) email = user.Email;
                            if (string.IsNullOrWhiteSpace(role)) role = user.Role;
                            if (string.IsNullOrWhiteSpace(university)) university = user.University;
                        }
                    }
                    catch
                    {
                        // Best-effort only.
                    }
                }

                UserRepository.AppendAudit(type, details, userId, email, firstName, role, university);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Adds an entry for someone who isn't signed in yet (failed sign-ins, new accounts).
        /// </summary>
        public static void Append(string type, string details,
                                  string userId = null, string email = null, string firstName = null,
                                  string role = null, string university = null)
        {
            UserRepository.AppendAudit(type, details, userId, email, firstName, role, university);
        }
    }
}