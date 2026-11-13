using CyberApp_FIA.Services;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Web.UI.WebControls;

namespace CyberApp_FIA.Participant
{
    // Sprint 4: shows a self-paced class (built from the sponsor's template) and its quiz.
    // ClassViewer.aspx               -> list of all classes
    // ClassViewer.aspx?class=slug    -> one class + quiz
    public partial class ClassViewer : SecurePage
    {
        private ClassService.ClassInfo _class;

        protected void Page_Load(object sender, EventArgs e)
        {
            // participants only
            var role = Session["Role"] as string;
            if (!string.Equals(role, "Participant", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            var slug = Request.QueryString["class"];

            // no class picked -> show the list
            if (string.IsNullOrWhiteSpace(slug))
            {
                if (!IsPostBack) ShowList();
                return;
            }

            _class = ClassService.Load(Server, slug);
            if (_class == null)
            {
                // bad or missing class name -> back to the list
                Response.Redirect("~/Account/Participant/ClassViewer.aspx");
                return;
            }

            if (!IsPostBack) ShowClass();
        }

        private void ShowList()
        {
            var classes = ClassService.LoadAll(Server);
            ClassListRepeater.DataSource = classes;
            ClassListRepeater.DataBind();
            NoClasses.Visible = classes.Count == 0;
        }

        private void ShowClass()
        {
            ListPanel.Visible = false;
            ClassPanel.Visible = true;

            ClassTitle.Text = Server.HtmlEncode(_class.Title);
            Objective.Text = Server.HtmlEncode(_class.Objective);
            PassScore.Text = _class.PassScore.ToString();

            BlocksRepeater.DataSource = _class.Blocks;
            BlocksRepeater.DataBind();

            QuestionsRepeater.DataSource = _class.Questions;
            QuestionsRepeater.DataBind();
        }

        // fills in each block's text, video, steps and question
        protected void BlocksRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;
            var block = (ClassService.ClassBlock)e.Item.DataItem;

            var texts = (Repeater)e.Item.FindControl("TextsRepeater");
            texts.DataSource = block.Texts;
            texts.DataBind();

            // only show the video if it's a real YouTube embed link
            e.Item.FindControl("VideoHolder").Visible = ClassService.IsSafeVideoUrl(block.VideoUrl);

            if (block.Steps.Count > 0)
            {
                e.Item.FindControl("StepsHolder").Visible = true;
                var steps = (Repeater)e.Item.FindControl("StepsRepeater");
                steps.DataSource = block.Steps;
                steps.DataBind();
            }

            e.Item.FindControl("QuestionHolder").Visible = !string.IsNullOrWhiteSpace(block.Question);
        }

        // adds the answer choices to each quiz question
        protected void QuestionsRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;
            var q = (ClassService.QuizQuestion)e.Item.DataItem;

            var answers = (RadioButtonList)e.Item.FindControl("Answers");
            foreach (var o in q.Options)
            {
                answers.Items.Add(new ListItem(Server.HtmlEncode(o.Key + ") " + o.Text), o.Key));
            }
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            // collect what the student picked: question id -> option key
            var picked = new Dictionary<string, string>();
            foreach (RepeaterItem item in QuestionsRepeater.Items)
            {
                var id = ((HiddenField)item.FindControl("QuestionId")).Value;
                var answer = ((RadioButtonList)item.FindControl("Answers")).SelectedValue;
                if (!string.IsNullOrEmpty(answer)) picked[id] = answer;
            }

            if (picked.Count < _class.Questions.Count)
            {
                QuizMessage.Text = "Please answer every question before submitting.";
                return;
            }
            QuizMessage.Text = "";

            var result = ClassService.Grade(_class, picked);

            // passed -> mark the course complete (counts toward progress and badges)
            if (result.Passed)
            {
                ClassService.MarkCompleted(Server, Session["UserId"] as string, _class.CourseId);
            }

            ShowResult(result);
        }

        private void ShowResult(ClassService.GradeResult result)
        {
            ResultPanel.Visible = true;

            ScoreLiteral.Text = result.Passed
                ? "<span class='good'>You passed! " + result.Percent + "% (" + result.CorrectCount + "/" + result.Total + ")</span>"
                : "<span class='bad'>" + result.Percent + "% (" + result.CorrectCount + "/" + result.Total +
                  "). Review the feedback below and try again.</span>";

            FeedbackRepeater.DataSource = result.Feedback;
            FeedbackRepeater.DataBind();

            OneAction.Text = Server.HtmlEncode(_class.OneAction);
            TeachingPrompt.Text = Server.HtmlEncode(_class.TeachingPrompt);
            NorthStar.Text = Server.HtmlEncode(_class.NorthStar);

            ResourcesRepeater.DataSource = _class.Resources;
            ResourcesRepeater.DataBind();
        }
    }
}