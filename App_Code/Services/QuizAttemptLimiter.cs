using System;
using System.Collections.Generic;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Limits unsuccessful quiz attempts per person per class.
    /// Each wrong submission shows feedback about the answers, so unlimited retries would let a
    /// script (or a very patient person) find every right answer by trial and error and get the
    /// course marked complete without learning it. After MaxFailedAttempts unsuccessful tries,
    /// the person has to wait for the window to pass before trying again.
    ///
    /// Counts live in memory, so they reset when the app restarts.
    /// </summary>
    public static class QuizAttemptLimiter
    {
        public const int MaxFailedAttempts = 5;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private const int MaxTrackedKeys = 20000;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Queue<DateTime>> Failures =
            new Dictionary<string, Queue<DateTime>>(StringComparer.OrdinalIgnoreCase);

        private static string Key(string userId, string classSlug)
        {
            return (userId ?? "") + "|" + (classSlug ?? "");
        }

        /// <summary>True if this person has used up their attempts for this class. minutesLeft says how long to wait.</summary>
        public static bool IsLimited(string userId, string classSlug, DateTime utcNow, out int minutesLeft)
        {
            minutesLeft = 0;
            lock (Gate)
            {
                Queue<DateTime> queue;
                if (!Failures.TryGetValue(Key(userId, classSlug), out queue)) return false;

                Prune(queue, utcNow);
                if (queue.Count < MaxFailedAttempts) return false;

                var unlocksAt = queue.Peek() + Window;
                minutesLeft = Math.Max(1, (int)Math.Ceiling((unlocksAt - utcNow).TotalMinutes));
                return true;
            }
        }

        /// <summary>Counts one unsuccessful attempt.</summary>
        public static void RecordFailure(string userId, string classSlug, DateTime utcNow)
        {
            lock (Gate)
            {
                if (Failures.Count > MaxTrackedKeys) RemoveExpired(utcNow);
                if (Failures.Count > MaxTrackedKeys) return;

                Queue<DateTime> queue;
                var key = Key(userId, classSlug);
                if (!Failures.TryGetValue(key, out queue))
                {
                    queue = new Queue<DateTime>();
                    Failures[key] = queue;
                }

                Prune(queue, utcNow);
                queue.Enqueue(utcNow);
            }
        }

        /// <summary>Forgets earlier failures once the person passes.</summary>
        public static void Clear(string userId, string classSlug)
        {
            lock (Gate) { Failures.Remove(Key(userId, classSlug)); }
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
