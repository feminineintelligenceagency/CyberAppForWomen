using System;
using System.Collections.Generic;

namespace CyberApp_FIA.Account
{
    /// <summary>
    /// Limits failed sign-ins per IP address, on top of the per-account lockout.
    /// The account lockout only protects accounts that exist and does nothing against someone
    /// trying one password on many accounts, or guessing emails. This counts every failed
    /// sign-in (wrong password, unknown email, locked account) per IP address.
    ///
    /// The limit is deliberately high because a whole university can share one public IP address
    /// (for example campus Wi-Fi at a cybersecurity fair). Counts live in memory, so they reset
    /// when the app restarts, and each server keeps its own count.
    /// </summary>
    internal static class LoginIpThrottle
    {
        /// <summary>
        /// Failed sign-ins allowed per address per window. Default 50. Change it with the appSetting
        /// "Login:MaxFailedAttemptsPerAddress"; set it to 0 to turn this limit off (for example if the
        /// host puts every visitor behind one proxy address, which would make the limit hit everyone).
        /// </summary>
        internal static int MaxFailuresPerWindow
        {
            get
            {
                int value;
                var text = System.Configuration.ConfigurationManager.AppSettings["Login:MaxFailedAttemptsPerAddress"];
                return int.TryParse(text, out value) && value >= 0 ? value : 50;
            }
        }
        internal static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private const int MaxTrackedAddresses = 20000;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Queue<DateTime>> Failures =
            new Dictionary<string, Queue<DateTime>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>True if this address has used up its failed sign-ins for the current window.</summary>
        internal static bool IsBlocked(string ip, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            var limit = MaxFailuresPerWindow;
            if (limit == 0) return false;   // turned off in configuration

            lock (Gate)
            {
                Queue<DateTime> queue;
                if (!Failures.TryGetValue(ip, out queue)) return false;

                Prune(queue, utcNow);
                if (queue.Count == 0)
                {
                    Failures.Remove(ip);
                    return false;
                }
                return queue.Count >= limit;
            }
        }

        /// <summary>Counts one failed sign-in for this address.</summary>
        internal static void RecordFailure(string ip, DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;

            lock (Gate)
            {
                // Keep memory bounded if someone sprays from very many addresses.
                if (Failures.Count > MaxTrackedAddresses) RemoveExpired(utcNow);
                if (Failures.Count > MaxTrackedAddresses) return;

                Queue<DateTime> queue;
                if (!Failures.TryGetValue(ip, out queue))
                {
                    queue = new Queue<DateTime>();
                    Failures[ip] = queue;
                }

                Prune(queue, utcNow);
                queue.Enqueue(utcNow);
            }
        }

        private static void Prune(Queue<DateTime> queue, DateTime utcNow)
        {
            while (queue.Count > 0 && utcNow - queue.Peek() > Window) queue.Dequeue();
        }

        private static void RemoveExpired(DateTime utcNow)
        {
            var expired = new List<string>();
            foreach (var pair in Failures)
            {
                Prune(pair.Value, utcNow);
                if (pair.Value.Count == 0) expired.Add(pair.Key);
            }
            foreach (var key in expired) Failures.Remove(key);
        }
    }
}
