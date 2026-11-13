using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace CyberApp_FIA.Services
{
    // Sprint 4: loads the self-paced cybersecurity classes and grades their quizzes.
    // Each class is one XML file in App_Data/classes (built from the sponsor's class template).
    // To add a new class, just add a new XML file there. No new code needed.
    public static class ClassService
    {
        // the pieces of a class (filled in from the XML file)
        public class ClassInfo
        {
            public string Slug { get; set; }          // file name without .xml, used in the page URL
            public string CourseId { get; set; }      // links to the course in microcourses.xml
            public string Title { get; set; }
            public string Objective { get; set; }
            public string NorthStar { get; set; }
            public int PassScore { get; set; }        // percent needed to pass
            public List<ClassBlock> Blocks { get; set; } = new List<ClassBlock>();
            public string OneAction { get; set; }
            public string TeachingPrompt { get; set; }
            public List<ClassResource> Resources { get; set; } = new List<ClassResource>();
            public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        }

        // one time block of the class (Hook, Core Concept, Hands-On, ...)
        public class ClassBlock
        {
            public string Minutes { get; set; }
            public string Name { get; set; }
            public List<string> Texts { get; set; } = new List<string>();
            public string Question { get; set; }
            public string VideoUrl { get; set; }
            public List<string> Steps { get; set; } = new List<string>();
        }

        public class ClassResource
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        public class QuizQuestion
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public string Correct { get; set; }       // key of the right option, like "C"
            public List<QuizOption> Options { get; set; } = new List<QuizOption>();
        }

        public class QuizOption
        {
            public string Key { get; set; }
            public string Text { get; set; }
            public string Feedback { get; set; }      // shown if the student picks this wrong answer
        }

        public class GradeResult
        {
            public int CorrectCount { get; set; }
            public int Total { get; set; }
            public int Percent { get; set; }
            public bool Passed { get; set; }
            public List<string> Feedback { get; set; } = new List<string>();
        }

        private static readonly object CompletionsLock = new object();

        // only letters, numbers and dashes, so nobody can sneak "../" into the URL to read other files
        private static readonly Regex SafeSlug = new Regex("^[a-z0-9-]+$");

        private static string ClassesFolder(HttpServerUtility server)
        {
            return server.MapPath("~/App_Data/classes");
        }

        // loads every class (for the list page)
        public static List<ClassInfo> LoadAll(HttpServerUtility server)
        {
            var list = new List<ClassInfo>();
            var folder = ClassesFolder(server);
            if (!Directory.Exists(folder)) return list;

            foreach (var file in Directory.GetFiles(folder, "*.xml"))
            {
                var c = Load(server, Path.GetFileNameWithoutExtension(file));
                if (c != null) list.Add(c);
            }
            return list;
        }

        // loads one class by its slug, returns null if it doesn't exist
        public static ClassInfo Load(HttpServerUtility server, string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            slug = slug.Trim().ToLowerInvariant();
            if (!SafeSlug.IsMatch(slug)) return null;

            var path = Path.Combine(ClassesFolder(server), slug + ".xml");
            if (!File.Exists(path)) return null;

            var doc = new XmlDocument();
            doc.Load(path);
            var root = doc.DocumentElement;

            var c = new ClassInfo
            {
                Slug = slug,
                CourseId = root.GetAttribute("courseId"),
                Title = Text(root, "title"),
                Objective = Text(root, "objective"),
                NorthStar = Text(root, "northStar"),
                PassScore = int.TryParse(root.GetAttribute("passScore"), out var p) ? p : 80
            };

            // time blocks
            foreach (XmlElement b in root.SelectNodes("block"))
            {
                var block = new ClassBlock
                {
                    Minutes = b.GetAttribute("minutes"),
                    Name = b.GetAttribute("name"),
                    Question = Text(b, "question"),
                    VideoUrl = (b["video"]?.GetAttribute("url") ?? "").Trim()
                };
                foreach (XmlElement t in b.SelectNodes("text")) block.Texts.Add(t.InnerText.Trim());
                foreach (XmlElement s in b.SelectNodes("step")) block.Steps.Add(s.InnerText.Trim());
                c.Blocks.Add(block);
            }

            // the close
            var close = root["close"];
            if (close != null)
            {
                c.OneAction = Text(close, "oneAction");
                c.TeachingPrompt = Text(close, "teachingPrompt");
                foreach (XmlElement r in close.SelectNodes("resource"))
                {
                    c.Resources.Add(new ClassResource { Title = r.InnerText.Trim(), Url = r.GetAttribute("url") });
                }
            }

            // quiz questions
            foreach (XmlElement q in root.SelectNodes("quiz/q"))
            {
                var question = new QuizQuestion
                {
                    Id = q.GetAttribute("id"),
                    Correct = q.GetAttribute("correct"),
                    Text = Text(q, "text")
                };
                foreach (XmlElement o in q.SelectNodes("option"))
                {
                    question.Options.Add(new QuizOption
                    {
                        Key = o.GetAttribute("key"),
                        Text = o.InnerText.Trim(),
                        Feedback = o.GetAttribute("feedback")
                    });
                }
                c.Questions.Add(question);
            }

            return c;
        }

        // grades the quiz. answers = question id -> option key the student picked
        public static GradeResult Grade(ClassInfo c, Dictionary<string, string> answers)
        {
            var result = new GradeResult { Total = c.Questions.Count };

            foreach (var q in c.Questions)
            {
                answers.TryGetValue(q.Id, out var picked);

                if (string.Equals(picked, q.Correct, StringComparison.OrdinalIgnoreCase))
                {
                    result.CorrectCount++;
                    continue;
                }

                // wrong or skipped: explain why
                var chosen = q.Options.Find(o => string.Equals(o.Key, picked, StringComparison.OrdinalIgnoreCase));
                var why = chosen != null && !string.IsNullOrWhiteSpace(chosen.Feedback)
                    ? chosen.Feedback
                    : "No answer selected.";
                result.Feedback.Add("Question " + q.Id + ": " + why);
            }

            result.Percent = result.Total == 0 ? 0 : (int)Math.Round(100.0 * result.CorrectCount / result.Total);
            result.Passed = result.Percent >= c.PassScore;
            return result;
        }

        // saves the course as completed in completions.xml (same format the rest of the app uses)
        public static void MarkCompleted(HttpServerUtility server, string userId, string courseId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(courseId)) return;

            var path = server.MapPath("~/App_Data/completions.xml");

            lock (CompletionsLock)
            {
                var doc = new XmlDocument();
                if (File.Exists(path)) doc.Load(path);
                else doc.AppendChild(doc.CreateElement("completions"));

                // find this user (compared as plain text, not built into the query)
                XmlElement user = null;
                foreach (XmlElement u in doc.SelectNodes("/completions/user"))
                {
                    if (u.GetAttribute("id") == userId) { user = u; break; }
                }
                if (user == null)
                {
                    user = doc.CreateElement("user");
                    user.SetAttribute("id", userId);
                    doc.DocumentElement.AppendChild(user);
                }

                // already completed? then nothing to do
                foreach (XmlElement existing in user.SelectNodes("course"))
                {
                    if (existing.GetAttribute("id") == courseId) return;
                }

                var course = doc.CreateElement("course");
                course.SetAttribute("id", courseId);
                course.SetAttribute("completedOn", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                user.AppendChild(course);

                doc.Save(path);
            }
        }

        // only allow real YouTube embed links in the video player
        public static bool IsSafeVideoUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   (url.StartsWith("https://www.youtube.com/embed/", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("https://www.youtube-nocookie.com/embed/", StringComparison.OrdinalIgnoreCase));
        }

        // reads a child element's text, or "" if it's missing
        private static string Text(XmlElement parent, string name)
        {
            return parent[name]?.InnerText.Trim() ?? "";
        }
    }
}