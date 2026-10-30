using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{

    /// Sprint 2 (April Lee story): permanently deletes a participant's account
    /// and everything stored about them.
    ///
    /// DATABASE (UserRepository.DeleteUser):
    ///   Users, UserCredentials, MFA backup codes, password reset tokens,
    ///   QuizProgress (+ answers), QuizResults (+ scores and items).
    ///   These all go through ON DELETE CASCADE from dbo.Users.
    ///   Audit log entries are kept as a security record, but their email and first name are removed.
    ///
    /// If a new feature stores user data:
    ///   - in a new database table: give its UserId foreign key ON DELETE CASCADE (nothing to add here)
    ///   - in a new XML file: add a RemoveElements line BEFORE the database step
    public static class AccountDeletionService
    {
        // One lock for the whole deletion, so two requests can't half-delete the same account.
        private static readonly object DeleteLock = new object();


        /// Deletes the user with this id from the database and every remaining XML file.
        /// Returns how many records were removed: the account counts as 1,
        /// plus each XML element removed. Returns 0 if the user didn't exist anywhere.
        ///
        /// Order matters: the XML files are cleaned FIRST and the database account is deleted LAST.
        /// The XML cleanup is safe to run again, so if anything throws, the user still has an
        /// account, can still sign in, and can just try again.
        public static int DeleteUser(HttpServerUtility server, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("A user id is required.", nameof(userId));

            userId = userId.Trim();
            int removed = 0;

            lock (DeleteLock)
            {
                // 1. Course progress:  <completions><user id="...">
                removed += RemoveElements(server, "completions.xml", "/completions/user", "id", userId);

                // 2. Badges earned:  <participantBadges><user id="...">
                removed += RemoveElements(server, "participantBadges.xml", "/participantBadges/user", "id", userId);

                // 3. Session sign-ups and waitlists:  <session><enrolled|waitlist><user id="...">
                removed += RemoveElements(server, "enrollments.xml", "//enrolled/user", "id", userId);
                removed += RemoveElements(server, "enrollments.xml", "//waitlist/user", "id", userId);

                // 4. Private conversations with helpers:  <conversation participantId="...">
                removed += RemoveElements(server, "helperMessages.xml", "/helperMessages/conversation", "participantId", userId);

                // 5. No-show records:  <missing participantId="...">
                removed += RemoveElements(server, "missingParticipantSessions.xml", "/missingParticipantSessions/missing", "participantId", userId);

                // 6. Inbox notifications and notification settings (Sprint 3):
                //    <notifications><notification userId="..."> and <preference userId="...">
                removed += RemoveElements(server, "notifications.xml", "/notifications/notification", "userId", userId);
                removed += RemoveElements(server, "notifications.xml", "/notifications/preference", "userId", userId);

                // 7. LAST: the account and everything in the database (see the order note above).
                //    Throws SqlException if the database can't be reached. Nothing in the
                //    database is deleted in that case, so the user can try again.
                if (UserRepository.DeleteUser(userId))
                {
                    removed += 1;
                }
            }

            return removed;
        }


        /// Opens one XML file in App_Data, removes every element at 'path' whose
        /// 'attribute' equals the user id, and saves the file only if something changed.
        private static int RemoveElements(HttpServerUtility server, string fileName, string path,
                                          string attribute, string userId)
        {
            var fullPath = server.MapPath("~/App_Data/" + fileName);
            if (!File.Exists(fullPath)) return 0;   // file not created yet = nothing to delete

            var doc = new XmlDocument();
            doc.Load(fullPath);

            // Collect matches first, then remove them. (Removing while looping would skip items.)
            // The user id is compared as plain text, never placed inside the XPath query,
            // the same injection-safe pattern used in Login.
            var toRemove = new List<XmlElement>();
            foreach (XmlElement el in doc.SelectNodes(path))
            {
                if (string.Equals(el.GetAttribute(attribute), userId, StringComparison.OrdinalIgnoreCase))
                    toRemove.Add(el);
            }

            foreach (var el in toRemove)
            {
                el.ParentNode.RemoveChild(el);
            }

            if (toRemove.Count > 0)
            {
                doc.Save(fullPath);
            }

            return toRemove.Count;
        }
    }
}