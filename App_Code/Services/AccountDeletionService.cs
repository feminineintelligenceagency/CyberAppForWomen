using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    
    /// Sprint 2 (April Lee story): permanently deletes a participant's account
    /// and everything stored about them in App_Data.
    ///
    /// One place that knows every file holding a user's data, so nothing is forgotten.
    /// If a new feature starts storing user data in a new file, add it here.
    
    public static class AccountDeletionService
    {
        // One lock for the whole deletion, so two requests can't half-delete the same account.
        private static readonly object DeleteLock = new object();

        
        /// Deletes the user with this id from every data file.
        /// Returns how many records were removed (useful for testing).
        
        public static int DeleteUser(HttpServerUtility server, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("A user id is required.", nameof(userId));

            int removed = 0;

            lock (DeleteLock)
            {
                // 1. The account itself:  <users><user id="...">
                removed += RemoveElements(server, "users.xml", "/users/user", "id", userId);

                // 2. Quiz answers in progress:  <Progress><User name="{userId}">
                removed += RemoveElements(server, "quiz_progress.xml", "/Progress/User", "name", userId);

                // 3. Finished quiz results:  <Results><UserResult name="{userId}">
                removed += RemoveElements(server, "quiz_results.xml", "/Results/UserResult", "name", userId);

                // 4. Course progress:  <completions><user id="...">
                removed += RemoveElements(server, "completions.xml", "/completions/user", "id", userId);

                // 5. Badges earned:  <participantBadges><user id="...">
                removed += RemoveElements(server, "participantBadges.xml", "/participantBadges/user", "id", userId);

                // 6. Session sign-ups and waitlists:  <session><enrolled|waitlist><user id="...">
                removed += RemoveElements(server, "enrollments.xml", "//enrolled/user", "id", userId);
                removed += RemoveElements(server, "enrollments.xml", "//waitlist/user", "id", userId);

                // 7. Private conversations with helpers:  <conversation participantId="...">
                removed += RemoveElements(server, "helperMessages.xml", "/helperMessages/conversation", "participantId", userId);

                // 8. No-show records:  <missing participantId="...">
                removed += RemoveElements(server, "missingParticipantSessions.xml", "/missingParticipantSessions/missing", "participantId", userId);
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