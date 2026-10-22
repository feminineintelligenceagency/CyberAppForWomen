using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Risk-assessment quiz (database version).
    ///
    /// Reads questions/domains from QuizQuestions, QuizOptions, QuizOptionWeights, QuizDomains,
    /// and stores progress/results in QuizProgress, QuizProgressAnswers, QuizResults,
    /// QuizResultDomainScores, QuizResultItems.
    ///
    /// "username" everywhere is the user's 32-character UserId (Session["UserId"]),
    /// which is what the XML files already stored.
    ///
    /// PRIVACY: answers and results describe a participant's personal safety situation.
    /// Only call these methods with the signed-in user's own id, or for a helper only
    /// when that participant's result has ShareWithHelper = true.
    /// </summary>
    public class QuizService
    {
        // Kept so existing pages that call new QuizService(Server) still compile.
        public QuizService(HttpServerUtility server) { }

        // ---------- Public DTOs (unchanged) ----------
        public class ScoreResult
        {
            public string Username { get; set; }
            public string RulesetVersion { get; set; }
            public DateTime CompletedUtc { get; set; }
            public double OverallScore { get; set; } // 0–10
            public Dictionary<string, double> DomainScores { get; set; } = new Dictionary<string, double>();
            public List<string> TopFactors { get; set; } = new List<string>();
            public List<string> QuickWins { get; set; } = new List<string>();
            public bool ShareWithHelper { get; set; }
        }

        public class UiQuestion
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
            public string D { get; set; }
        }

        // ---------- Internal models ----------
        private class Domain
        {
            public string Key;
            public string Title;
            public string QuickWin;
            public string Factor;
        }

        private class Option
        {
            public string Label = "";
            public List<(string module, int weight)> Maps = new List<(string, int)>();
        }

        private class Question
        {
            public string Id;
            public string Text;
            public Dictionary<string, Option> Options = new Dictionary<string, Option>(StringComparer.OrdinalIgnoreCase);
        }

        // =====================================================================
        // Ruleset / questions
        // =====================================================================

        /// <summary>The newest ruleset version (e.g. "2.0").</summary>
        public string CurrentRulesetVersion()
        {
            var v = Database.ExecuteScalar(@"
                    SELECT TOP 1 Version FROM dbo.QuizRulesets
                     ORDER BY CASE WHEN CreatedAtUtc IS NULL THEN 1 ELSE 0 END, CreatedAtUtc DESC, Version DESC;");
            return v as string ?? "2.0";
        }

        /// <summary>Questions with their A–D labels, in display order.</summary>
        public List<UiQuestion> LoadQuestionsForUi(out string rulesetVersion)
        {
            rulesetVersion = CurrentRulesetVersion();
            return LoadQuestions().Select(q => new UiQuestion
            {
                Id = q.Id,
                Text = q.Text,
                A = Label(q, "A"),
                B = Label(q, "B"),
                C = Label(q, "C"),
                D = Label(q, "D")
            }).ToList();
        }

        private static string Label(Question q, string key)
        {
            return q.Options.TryGetValue(key, out var o) ? o.Label : "";
        }

        private static Dictionary<string, Domain> LoadDomains()
        {
            return Database.Query(
                "SELECT DomainKey, Title, QuickWin, Factor FROM dbo.QuizDomains;",
                r => new Domain
                {
                    Key = r.GetString(0),
                    Title = r.GetString(1),
                    QuickWin = Database.GetStringOrNull(r, "QuickWin") ?? "",
                    Factor = Database.GetStringOrNull(r, "Factor") ?? ""
                })
                .ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);
        }

        private static List<Question> LoadQuestions()
        {
            var questions = Database.Query(
                "SELECT QuestionId, QuestionText FROM dbo.QuizQuestions WHERE IsActive = 1 ORDER BY SortOrder;",
                r => new Question { Id = r.GetString(0), Text = r.GetString(1) });

            var byId = questions.ToDictionary(q => q.Id, q => q, StringComparer.OrdinalIgnoreCase);

            foreach (var row in Database.Query(
                "SELECT QuestionId, OptionKey, OptionText FROM dbo.QuizOptions;",
                r => new { Q = r.GetString(0), K = r.GetString(1), T = r.GetString(2) }))
            {
                if (byId.TryGetValue(row.Q, out var q)) q.Options[row.K] = new Option { Label = row.T };
            }

            foreach (var row in Database.Query(
                "SELECT QuestionId, OptionKey, DomainKey, Weight FROM dbo.QuizOptionWeights;",
                r => new { Q = r.GetString(0), K = r.GetString(1), D = r.GetString(2), W = r.GetInt32(3) }))
            {
                if (byId.TryGetValue(row.Q, out var q) && q.Options.TryGetValue(row.K, out var o))
                    o.Maps.Add((row.D, row.W));
            }

            return questions;
        }

        // =====================================================================
        // Progress
        // =====================================================================

        /// <summary>
        /// Saves (or changes) one answer and the user's position in the quiz.
        /// If the user's saved progress is for an older ruleset, it is reset first.
        /// </summary>
        public void SaveAnswer(string username, string questionId, string selectedOption, int index, int total, string rulesetVersion)
        {
            var userId = NormalizeUserId(username);
            var choice = (selectedOption ?? "").Trim().ToUpperInvariant();
            var version = string.IsNullOrWhiteSpace(rulesetVersion) ? CurrentRulesetVersion() : rulesetVersion.Trim();

            Database.InTransaction((conn, tx) =>
            {
                EnsureRuleset(conn, tx, version);

                // Lock this user's progress row for the rest of the transaction so two
                // quick clicks can't create duplicate rows.
                Database.ExecuteNonQuery(conn, tx, @"
                    DECLARE @Existing VARCHAR(10) =
                        (SELECT RulesetVersion FROM dbo.QuizProgress WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @U);

                    IF @Existing IS NULL
                        INSERT INTO dbo.QuizProgress (UserId, RulesetVersion, LastIndex, TotalQuestions, IsCompleted, UpdatedAtUtc)
                        VALUES (@U, @V, @I, @T, 0, SYSUTCDATETIME());
                    ELSE IF @Existing <> @V
                    BEGIN
                        -- Different ruleset: start over.
                        DELETE FROM dbo.QuizProgressAnswers WHERE UserId = @U;
                        UPDATE dbo.QuizProgress
                           SET RulesetVersion = @V, LastIndex = @I, TotalQuestions = @T, IsCompleted = 0, UpdatedAtUtc = SYSUTCDATETIME()
                         WHERE UserId = @U;
                    END
                    ELSE
                        UPDATE dbo.QuizProgress
                           SET LastIndex = @I, TotalQuestions = @T, UpdatedAtUtc = SYSUTCDATETIME()
                         WHERE UserId = @U;

                    UPDATE dbo.QuizProgressAnswers SET Choice = @C WHERE UserId = @U AND QuestionId = @Q;
                    IF @@ROWCOUNT = 0
                        INSERT INTO dbo.QuizProgressAnswers (UserId, QuestionId, Choice) VALUES (@U, @Q, @C);",
                    UserIdParam(userId),
                    Database.Param("@V", SqlDbType.VarChar, 10, version),
                    Database.Param("@I", SqlDbType.Int, Math.Max(0, index)),
                    Database.Param("@T", SqlDbType.Int, Math.Max(0, total)),
                    Database.Param("@Q", SqlDbType.VarChar, 20, (questionId ?? "").Trim()),
                    Database.Param("@C", SqlDbType.Char, 1, choice));
            });
        }

        /// <summary>Saved position, answers and completion flag for this ruleset (empty if none).</summary>
        public (int lastIndex, Dictionary<string, string> selections, bool completed) LoadProgress(string username, string rulesetVersion)
        {
            var userId = NormalizeUserId(username);
            var selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var progress = Database.QuerySingle(@"
SELECT LastIndex, IsCompleted FROM dbo.QuizProgress WHERE UserId = @U AND RulesetVersion = @V;",
                r => new { LastIndex = r.GetInt32(0), Completed = r.GetBoolean(1) },
                UserIdParam(userId),
                Database.Param("@V", SqlDbType.VarChar, 10, rulesetVersion));

            if (progress == null) return (0, selections, false);

            foreach (var a in Database.Query(
                "SELECT QuestionId, Choice FROM dbo.QuizProgressAnswers WHERE UserId = @U;",
                r => new { Q = r.GetString(0), C = r.GetString(1) },
                UserIdParam(userId)))
            {
                selections[a.Q] = a.C;
            }

            return (progress.LastIndex, selections, progress.Completed);
        }

        public void MarkCompleted(string username, string rulesetVersion)
        {
            Database.ExecuteNonQuery(@"
UPDATE dbo.QuizProgress SET IsCompleted = 1, UpdatedAtUtc = SYSUTCDATETIME()
 WHERE UserId = @U AND RulesetVersion = @V;",
                UserIdParam(NormalizeUserId(username)),
                Database.Param("@V", SqlDbType.VarChar, 10, rulesetVersion));
        }

        public bool IsCompleted(string username, string rulesetVersion)
        {
            var v = Database.ExecuteScalar(@"
SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.QuizProgress
                          WHERE UserId = @U AND RulesetVersion = @V AND IsCompleted = 1)
            THEN 1 ELSE 0 END;",
                UserIdParam(NormalizeUserId(username)),
                Database.Param("@V", SqlDbType.VarChar, 10, rulesetVersion));
            return Convert.ToInt32(v) == 1;
        }

        // =====================================================================
        // Scoring and results
        // =====================================================================

        /// <summary>
        /// Scores the answers (same rules as before: sum option weights per domain,
        /// clamp 0–10, overall = average), saves the result and marks the quiz complete.
        /// </summary>
        public ScoreResult ComputeAndSaveResult(string username, Dictionary<string, string> selections)
        {
            var userId = NormalizeUserId(username);
            selections = selections ?? new Dictionary<string, string>();

            var version = CurrentRulesetVersion();
            var domains = LoadDomains();
            var questions = LoadQuestions();

            // Accumulate domain scores from each selected option's weights.
            var scores = domains.Keys.ToDictionary(k => k, k => 0.0, StringComparer.OrdinalIgnoreCase);
            foreach (var q in questions)
            {
                if (!selections.TryGetValue(q.Id, out var pick) || pick == null) continue;
                if (!q.Options.TryGetValue(pick.Trim(), out var opt)) continue;
                foreach (var (module, weight) in opt.Maps)
                {
                    if (scores.ContainsKey(module)) scores[module] += weight;
                }
            }

            // Clamp to 0..10
            foreach (var k in scores.Keys.ToList())
            {
                scores[k] = Math.Max(0, Math.Min(10, scores[k]));
            }

            var overall = scores.Values.Any() ? scores.Values.Average() : 0.0;

            // Top 3 highest-risk domains -> their factor and quick win
            var top = scores.OrderByDescending(kv => kv.Value)
                            .ThenBy(kv => domains[kv.Key].Title)
                            .Take(3).ToList();

            var factors = top.Select(kv => $"{domains[kv.Key].Title}: {domains[kv.Key].Factor}").ToList();
            var wins = top.Select(kv => domains[kv.Key].QuickWin)
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .Distinct()
                          .Take(3).ToList();

            var result = new ScoreResult
            {
                Username = userId,
                RulesetVersion = version,
                CompletedUtc = DateTime.UtcNow,
                OverallScore = Math.Round(overall, 2),
                DomainScores = scores.ToDictionary(kv => domains[kv.Key].Title, kv => Math.Round(kv.Value, 2)),
                TopFactors = factors,
                QuickWins = wins,
                ShareWithHelper = false
            };

            // Save the result and mark progress complete together.
            Database.InTransaction((conn, tx) =>
            {
                EnsureRuleset(conn, tx, version);

                var resultId = Convert.ToInt32(Database.ExecuteScalar(conn, tx, @"
INSERT INTO dbo.QuizResults (UserId, RulesetVersion, CompletedAtUtc, OverallScore, ShareWithHelper)
OUTPUT inserted.ResultId
VALUES (@U, @V, @C, @O, @S);",
                    UserIdParam(userId),
                    Database.Param("@V", SqlDbType.VarChar, 10, version),
                    Database.Param("@C", SqlDbType.DateTime2, result.CompletedUtc),
                    Database.Param("@O", SqlDbType.Decimal, (decimal)result.OverallScore),
                    Database.Param("@S", SqlDbType.Bit, result.ShareWithHelper)));

                foreach (var kv in scores)
                {
                    Database.ExecuteNonQuery(conn, tx,
                        "INSERT INTO dbo.QuizResultDomainScores (ResultId, DomainKey, Score) VALUES (@R, @D, @Sc);",
                        Database.Param("@R", SqlDbType.Int, resultId),
                        Database.Param("@D", SqlDbType.VarChar, 50, kv.Key),
                        Database.Param("@Sc", SqlDbType.Decimal, (decimal)Math.Round(kv.Value, 2)));
                }

                InsertItems(conn, tx, resultId, "Factor", factors);
                InsertItems(conn, tx, resultId, "QuickWin", wins);

                Database.ExecuteNonQuery(conn, tx, @"
UPDATE dbo.QuizProgress SET IsCompleted = 1, UpdatedAtUtc = SYSUTCDATETIME()
 WHERE UserId = @U AND RulesetVersion = @V;",
                    UserIdParam(userId),
                    Database.Param("@V", SqlDbType.VarChar, 10, version));
            });

            return result;
        }

        /// <summary>The user's most recent result, or null if they haven't finished the quiz.</summary>
        public ScoreResult LoadLatestResult(string username)
        {
            var userId = NormalizeUserId(username);

            var header = Database.QuerySingle(@"
SELECT TOP 1 ResultId, RulesetVersion, CompletedAtUtc, OverallScore, ShareWithHelper
  FROM dbo.QuizResults
 WHERE UserId = @U
 ORDER BY CompletedAtUtc DESC;",
                r => new
                {
                    Id = r.GetInt32(0),
                    Version = r.GetString(1),
                    Completed = DateTime.SpecifyKind(r.GetDateTime(2), DateTimeKind.Utc),
                    Overall = (double)r.GetDecimal(3),
                    Share = r.GetBoolean(4)
                },
                UserIdParam(userId));

            if (header == null) return null;

            var result = new ScoreResult
            {
                Username = userId,
                RulesetVersion = header.Version,
                CompletedUtc = header.Completed,
                OverallScore = header.Overall,
                ShareWithHelper = header.Share
            };

            foreach (var d in Database.Query(@"
SELECT d.Title, s.Score
  FROM dbo.QuizResultDomainScores s
  JOIN dbo.QuizDomains d ON d.DomainKey = s.DomainKey
 WHERE s.ResultId = @R
 ORDER BY d.Title;",
                r => new { Title = r.GetString(0), Score = (double)r.GetDecimal(1) },
                Database.Param("@R", SqlDbType.Int, header.Id)))
            {
                result.DomainScores[d.Title] = d.Score;
            }

            foreach (var item in Database.Query(@"
SELECT ItemType, ItemText FROM dbo.QuizResultItems WHERE ResultId = @R ORDER BY ItemType, SortOrder;",
                r => new { Type = r.GetString(0), Text = r.GetString(1) },
                Database.Param("@R", SqlDbType.Int, header.Id)))
            {
                if (item.Type == "Factor") result.TopFactors.Add(item.Text);
                else result.QuickWins.Add(item.Text);
            }

            return result;
        }

        /// <summary>Lets the participant share (or stop sharing) all their results with helpers.</summary>
        public void SetShareWithHelper(string username, bool share)
        {
            Database.ExecuteNonQuery(
                "UPDATE dbo.QuizResults SET ShareWithHelper = @S WHERE UserId = @U;",
                Database.Param("@S", SqlDbType.Bit, share),
                UserIdParam(NormalizeUserId(username)));
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static void InsertItems(SqlConnection conn, SqlTransaction tx, int resultId, string type, List<string> items)
        {
            int order = 0;
            foreach (var text in items)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                order++;
                var t = text.Trim();
                if (t.Length > 500) t = t.Substring(0, 500);
                Database.ExecuteNonQuery(conn, tx,
                    "INSERT INTO dbo.QuizResultItems (ResultId, ItemType, SortOrder, ItemText) VALUES (@R, @T, @O, @X);",
                    Database.Param("@R", SqlDbType.Int, resultId),
                    Database.Param("@T", SqlDbType.VarChar, 10, type),
                    Database.Param("@O", SqlDbType.Int, order),
                    Database.Param("@X", SqlDbType.NVarChar, 500, t));
            }
        }

        // Progress and results reference QuizRulesets, so make sure the version row exists.
        private static void EnsureRuleset(SqlConnection conn, SqlTransaction tx, string version)
        {
            Database.ExecuteNonQuery(conn, tx, @"
IF NOT EXISTS (SELECT 1 FROM dbo.QuizRulesets WHERE Version = @V)
    INSERT INTO dbo.QuizRulesets (Version, CreatedAtUtc) VALUES (@V, NULL);",
                Database.Param("@V", SqlDbType.VarChar, 10, version));
        }

        private static string NormalizeUserId(string username)
        {
            var id = (username ?? "").Trim().ToLowerInvariant();
            if (id.Length == 0) throw new ArgumentException("A signed-in user id is required.", nameof(username));
            return id;
        }

        private static SqlParameter UserIdParam(string userId)
        {
            return Database.Param("@U", SqlDbType.Char, 32, userId);
        }
    }
}