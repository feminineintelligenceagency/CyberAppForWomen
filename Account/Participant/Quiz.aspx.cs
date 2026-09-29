using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using CyberApp_FIA.Services;

namespace CyberApp_FIA.Account.Participant
{
    public partial class Quiz : SecurePage
    {
        private QuizService _svc;
        private List<QuizService.UiQuestion> _questions;
        private string _rules;
        private string UserKey => Session["UserId"] as string ?? "guest";

        private int Index
        {
            get { return ViewState["Index"] == null ? 0 : (int)ViewState["Index"]; }
            set { ViewState["Index"] = value; }
        }

        private Dictionary<string, string> Selections
        {
            get
            {
                if (ViewState["Sel"] == null) ViewState["Sel"] = new Dictionary<string, string>();
                return (Dictionary<string, string>)ViewState["Sel"];
            }
            set { ViewState["Sel"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            _svc = new QuizService(Server);
            _questions = _svc.LoadQuestionsForUi(out _rules);

            if (!IsPostBack)
            {
                // If already completed, go home
                if (_svc.IsCompleted(UserKey, _rules))
                {
                    Response.Redirect("~/Account/Participant/Home.aspx");
                    return;
                }

                // Load saved progress
                var (idx, map, completed) = _svc.LoadProgress(UserKey, _rules);
                Selections = map ?? new Dictionary<string, string>();
                Index = Math.Max(0, Math.Min(idx, _questions.Count - 1));
                BindQuestion();
            }
        }

        private void BindQuestion()
        {
            var q = _questions[Index];
            LblQuestionText.Text = q.Text;

            // Assign values and texts (tile labels show "A) ...", etc.)
            Options.Items[0].Value = "A"; Options.Items[0].Text = $"<span class='tag'>A</span>{q.A}";
            Options.Items[1].Value = "B"; Options.Items[1].Text = $"<span class='tag'>B</span>{q.B}";
            Options.Items[2].Value = "C"; Options.Items[2].Text = $"<span class='tag'>C</span>{q.C}";
            Options.Items[3].Value = "D"; Options.Items[3].Text = $"<span class='tag'>D</span>{q.D}";

            // Render HTML inside label content

            Options.TextAlign = TextAlign.Right; // ensures label content next to hidden radio

            // restore selection if exists
            Options.ClearSelection();
            if (Selections.TryGetValue(q.Id, out var sel))
            {
                var item = Options.Items.FindByValue(sel);
                if (item != null) item.Selected = true;
            }

            // progress bar (Index is 0-based)
            var pct = (int)Math.Round((Index / (double)_questions.Count) * 100.0);
            ProgressBar.Style["width"] = pct + "%";
            LblQuestionNumber.InnerText = $"Question {Index + 1} of {_questions.Count}";

            // buttons
            BtnPrev.Enabled = Index > 0;
            BtnNext.Visible = Index < _questions.Count - 1;
            BtnFinish.Visible = Index == _questions.Count - 1;

            // hide any lingering warning when we bind fresh
            WarnBox.Visible = false;

            // flash "Saved" briefly on postbacks
            BadgeSaved.Visible = IsPostBack;
        }

        private bool HasCurrentSelection()
        {
            // Trust the live RBL state (BindQuestion restores saved selection)
            return Options.SelectedIndex >= 0;
        }

        private void SaveSelectionIfAny()
        {
            var q = _questions[Index];
            var selected = Options.SelectedValue;
            if (!string.IsNullOrEmpty(selected))
            {
                Selections[q.Id] = selected;
                _svc.SaveAnswer(UserKey, q.Id, selected, Index, _questions.Count, _rules);
            }
        }

        protected void Options_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveSelectionIfAny();
            WarnBox.Visible = false; // clear warning on selection
            BindQuestion();
        }

        protected void BtnPrev_Click(object sender, EventArgs e)
        {
            // No selection requirement on going back
            SaveSelectionIfAny();
            Index = Math.Max(0, Index - 1);
            BindQuestion();
        }

        protected void BtnNext_Click(object sender, EventArgs e)
        {
            // Enforce selection before moving forward
            SaveSelectionIfAny();
            if (!HasCurrentSelection())
            {
                WarnBox.Visible = true;
                return;
            }

            WarnBox.Visible = false;
            Index = Math.Min(_questions.Count - 1, Index + 1);
            BindQuestion();
        }

        protected void BtnFinish_Click(object sender, EventArgs e)
        {
            // Enforce selection before finishing
            SaveSelectionIfAny();
            if (!HasCurrentSelection())
            {
                WarnBox.Visible = true;
                return;
            }

            var result = _svc.ComputeAndSaveResult(UserKey, Selections);

            // INSERT: audit log for Pre-Event Quiz completion (no score)
            try
            {
                UniversityAuditLogger.AppendForCurrentUser(
                    this,
                    "Participant Pre-Event Quiz Completion",
                    "Participant completed the pre-event cybersecurity quiz."
                );
            }
            catch
            {
                // Best-effort only; quiz flow should never fail from logging.
            }

            // Show summary
            PanelQuestion.Visible = false;
            PanelComplete.Visible = true;


            // v2 overall is 0–10; v1 is 0–100; show what the engine produced
            LblOverall.Text = $"Overall Score: <strong>{result.OverallScore}</strong>";
            RptDomains.DataSource = result.DomainScores.ToList();
            RptDomains.DataBind();

            BlFactors.DataSource = result.TopFactors;
            BlFactors.DataBind();

            BlWins.DataSource = result.QuickWins;
            BlWins.DataBind();
        }

        protected void BtnSaveShare_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Account/Participant/Home.aspx");
        }
    }
}

