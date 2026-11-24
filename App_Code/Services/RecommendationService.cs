using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    // Sprint 5 (user story #16): recommends microcourses from the user's quiz score.
    // The quiz gives a "gap" score out of 10 for each risk area (higher = more risk).
    // Biggest gaps come first. Used by the quiz results page.
    public static class RecommendationService
    {
        // the course that covers accounts a former partner may still access (Sprint 4 class)
        public const string AccountAccessCourseTitle = "Who Else Is Logged In? Find and Remove Unknown Sessions";

        // quiz areas that suggest someone else might be able to get into the user's accounts
        private static readonly string[] AccountRiskAreas =
        {
            "Two-Factor Authentication Setup and Management",
            "Password Management and Security",
            "Detecting Spyware Infections on Devices"
        };

        // gap score (out of 10) in one of those areas that triggers the account-access course
        public const double AccountRiskThreshold = 5.0;

        // a few quiz area names don't exactly match the course titles, so map them here
        private static readonly Dictionary<string, string> TitleAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Two-Factor Authentication Setup and Management", "2FA Setup And Management" },
            { "Detecting Spyware Infections on Devices", "Detecting Spyware Infection on Devices" },
            { "Managing Your Digital Footprint", "Managing Digital Footprint" },
            { "Identifying Hidden Surveillance Devices (Electronic Scanning)", "Identifying Hidden-Surveilance Devices" }
        };

        // one recommended course
        public class Recommendation
        {
            public string CourseId { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string Reason { get; set; }
            public double Gap { get; set; }
            public string ClassUrl { get; set; }   // set if the course is a self-paced class
            public bool IsClass => !string.IsNullOrEmpty(ClassUrl);
        }

        private class Course
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
        }

        // true if the user's score in 2FA, passwords, or spyware is high enough to suggest
        // someone else could have access to their accounts. maxGap = the highest of those scores.
        public static bool ShouldRecommendAccountAccess(Dictionary<string, double> domainScores, out double maxGap)
        {
            maxGap = 0;
            if (domainScores == null) return false;

            foreach (var area in AccountRiskAreas)
            {
                if (domainScores.TryGetValue(area, out var score) && score > maxGap)
                    maxGap = score;
            }
            return maxGap >= AccountRiskThreshold;
        }

        // builds the ranked list of recommended courses for this user
        public static List<Recommendation> GetRecommendations(HttpServerUtility server, string userId,
                                                              Dictionary<string, double> domainScores, int max)
        {
            var results = new List<Recommendation>();
            if (domainScores == null || domainScores.Count == 0) return results;

            var courses = LoadPublishedCourses(server);
            var completed = LoadCompletedCourseIds(server, userId);
            var classUrls = LoadClassUrls(server);

            // 1. one course per quiz area that has a gap
            foreach (var area in domainScores.Where(kv => kv.Value > 0))
            {
                var course = FindCourse(courses, area.Key);
                if (course == null || completed.Contains(course.Id)) continue;
                if (results.Any(r => r.CourseId == course.Id)) continue;

                results.Add(new Recommendation
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    Summary = course.Summary,
                    Gap = area.Value,
                    Reason = "Your " + area.Key + " risk score was " + area.Value.ToString("0.##") + "/10."
                });
            }

            // 2. account-access course if the 2FA, password, or spyware gap is high
            if (ShouldRecommendAccountAccess(domainScores, out var accountGap))
            {
                var course = FindCourse(courses, AccountAccessCourseTitle);
                if (course != null && !completed.Contains(course.Id) && !results.Any(r => r.CourseId == course.Id))
                {
                    results.Add(new Recommendation
                    {
                        CourseId = course.Id,
                        Title = course.Title,
                        Summary = course.Summary,
                        Gap = accountGap,
                        Reason = "Your answers about passwords, two-factor authentication, or spyware suggest someone " +
                                 "else may be able to get into your accounts. This class shows you how to check and lock them down."
                    });
                }
            }

            // 3. biggest gap first, then add the class link if the course is a self-paced class
            var ranked = results.OrderByDescending(r => r.Gap).ThenBy(r => r.Title).Take(max).ToList();
            foreach (var r in ranked)
            {
                if (classUrls.TryGetValue(r.CourseId, out var url)) r.ClassUrl = url;
            }
            return ranked;
        }

        // finds a course by quiz area name or course title (ignores case, dashes, and punctuation)
        private static Course FindCourse(List<Course> courses, string name)
        {
            if (TitleAliases.TryGetValue(name, out var alias)) name = alias;
            var key = Normalize(name);
            return courses.FirstOrDefault(c => Normalize(c.Title) == key);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var chars = value.ToLowerInvariant().Replace("&", " and ")
                             .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray();
            return string.Join(" ", new string(chars).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // only published courses can be recommended (no drafts or test courses)
        private static List<Course> LoadPublishedCourses(HttpServerUtility server)
        {
            var list = new List<Course>();
            var path = server.MapPath("~/App_Data/microcourses.xml");
            if (!File.Exists(path)) return list;

            var doc = new XmlDocument();
            doc.Load(path);
            foreach (XmlElement c in doc.SelectNodes("/microcourses/course"))
            {
                if (!string.Equals(c.GetAttribute("status"), "Published", StringComparison.OrdinalIgnoreCase)) continue;
                list.Add(new Course
                {
                    Id = c.GetAttribute("id"),
                    Title = (c["title"]?.InnerText ?? "").Trim(),
                    Summary = (c["summary"]?.InnerText ?? "").Trim()
                });
            }
            return list;
        }

        // courses this user already finished (so we don't recommend them again)
        private static HashSet<string> LoadCompletedCourseIds(HttpServerUtility server, string userId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var path = server.MapPath("~/App_Data/completions.xml");
            if (string.IsNullOrWhiteSpace(userId) || !File.Exists(path)) return set;

            var doc = new XmlDocument();
            doc.Load(path);
            foreach (XmlElement u in doc.SelectNodes("/completions/user"))
            {
                if (u.GetAttribute("id") != userId) continue;   // compared as text, not built into the query
                foreach (XmlElement c in u.SelectNodes("course")) set.Add(c.GetAttribute("id"));
            }
            return set;
        }

        // course id -> link to its self-paced class page (from the Sprint 4 class files)
        public static Dictionary<string, string> LoadClassUrls(HttpServerUtility server)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in ClassService.LoadAll(server))
            {
                if (!string.IsNullOrWhiteSpace(c.CourseId))
                    map[c.CourseId] = "~/Account/Participant/ClassViewer.aspx?class=" + HttpUtility.UrlEncode(c.Slug);
            }
            return map;
        }
    }
}