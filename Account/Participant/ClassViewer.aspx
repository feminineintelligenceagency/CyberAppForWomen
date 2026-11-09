<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ClassViewer.aspx.cs" Inherits="CyberApp_FIA.Participant.ClassViewer" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Classes</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />
  <style>
    :root{ --fia-pink:#f06aa9; --fia-blue:#2a99db; --ink:#1c1c1c; --muted:#6b7280; --card-border:#e8eef7; --good:#065f46; --bad:#b91c1c; }
    *{ box-sizing:border-box; }
    body{ margin:0; font-family:Lato, Arial, sans-serif; color:var(--ink); background:linear-gradient(135deg,#ffffff,#f9fbff); }
    .wrap{ min-height:100vh; padding:24px 16px; max-width:760px; margin:0 auto; }
    h1{ font-family:Poppins; margin:0 0 6px 0; font-size:1.4rem; }
    h2{ font-family:Poppins; margin:0 0 8px 0; font-size:1.05rem; }
    .sub{ color:var(--muted); margin:0 0 16px 0; }
    .pill{ display:inline-block; padding:6px 10px; border-radius:999px; background:#f0f7fd; border:1px solid #d9e9f6;
           color:var(--fia-blue); text-decoration:none; font-size:.9rem; margin-bottom:16px; }
    .card{ background:#fff; border:1px solid var(--card-border); border-radius:14px; padding:18px; margin-bottom:14px; }
    .objective{ border-left:4px solid var(--fia-pink); }
    .time{ color:var(--muted); font-size:.85rem; }
    .ask{ background:#f6f7fb; border-radius:10px; padding:10px; margin-top:10px; }
    .video{ position:relative; padding-top:56.25%; margin:10px 0; }
    .video iframe{ position:absolute; inset:0; width:100%; height:100%; border:0; border-radius:10px; }
    ol{ margin:6px 0 0 18px; padding:0; }
    .q{ margin-bottom:16px; }
    .q-text{ font-weight:700; margin-bottom:6px; }
    .btn{ border:0; border-radius:10px; padding:10px 16px; font-size:1rem; cursor:pointer; background:var(--fia-blue); color:#fff; }
    .good{ color:var(--good); } .bad{ color:var(--bad); }
  </style>
</head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <a class="pill" href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>">&larr; Back to home</a>

      <%-- List of all classes (shown when no class is picked) --%>
      <asp:Panel ID="ListPanel" runat="server">
        <h1>Classes</h1>
        <p class="sub">Short classes you can take at your own pace. Pass the quiz to complete one.</p>
        <asp:Repeater ID="ClassListRepeater" runat="server">
          <ItemTemplate>
            <div class="card">
              <h2><%#: Eval("Title") %></h2>
              <p><%#: Eval("Objective") %></p>
              <a class="pill" href="<%# ResolveUrl("~/Account/Participant/ClassViewer.aspx?class=" + HttpUtility.UrlEncode((string)Eval("Slug"))) %>">Start class</a>
            </div>
          </ItemTemplate>
        </asp:Repeater>
        <asp:Literal ID="NoClasses" runat="server" Visible="false" Text="<p>No classes yet.</p>" />
      </asp:Panel>

      <%-- One class --%>
      <asp:Panel ID="ClassPanel" runat="server" Visible="false">
        <h1><asp:Literal ID="ClassTitle" runat="server" /></h1>

        <%-- Template rule: objective is shown up front --%>
        <div class="card objective">
          <strong>By the end of this class, you will be able to:</strong>
          <p><asp:Literal ID="Objective" runat="server" /></p>
        </div>

        <%-- Time blocks: Hook, The One Thing, Core Concept, Real Examples, Hands-On --%>
        <asp:Repeater ID="BlocksRepeater" runat="server" OnItemDataBound="BlocksRepeater_ItemDataBound">
          <ItemTemplate>
            <div class="card">
              <div class="time"><%#: Eval("Minutes") %> min</div>
              <h2><%#: Eval("Name") %></h2>

              <asp:Repeater ID="TextsRepeater" runat="server">
                <ItemTemplate><p><%#: Container.DataItem %></p></ItemTemplate>
              </asp:Repeater>

              <asp:PlaceHolder ID="VideoHolder" runat="server" Visible="false">
                <div class="video"><iframe src="<%#: Eval("VideoUrl") %>" title="Class video" allowfullscreen></iframe></div>
              </asp:PlaceHolder>

              <asp:PlaceHolder ID="StepsHolder" runat="server" Visible="false">
                <strong>Try it on your own device:</strong>
                <ol>
                  <asp:Repeater ID="StepsRepeater" runat="server">
                    <ItemTemplate><li><%#: Container.DataItem %></li></ItemTemplate>
                  </asp:Repeater>
                </ol>
              </asp:PlaceHolder>

              <asp:PlaceHolder ID="QuestionHolder" runat="server" Visible="false">
                <div class="ask"><strong>Think about it:</strong> <%#: Eval("Question") %></div>
              </asp:PlaceHolder>
            </div>
          </ItemTemplate>
        </asp:Repeater>

        <%-- Quiz --%>
        <asp:Panel ID="QuizPanel" runat="server" CssClass="card">
          <h2>Quiz</h2>
          <p class="sub">Answer every question. You need <asp:Literal ID="PassScore" runat="server" />% to pass.</p>
          <asp:Repeater ID="QuestionsRepeater" runat="server" OnItemDataBound="QuestionsRepeater_ItemDataBound">
            <ItemTemplate>
              <div class="q">
                <div class="q-text"><%#: Eval("Id") %>. <%#: Eval("Text") %></div>
                <asp:HiddenField ID="QuestionId" runat="server" Value='<%# Eval("Id") %>' />
                <asp:RadioButtonList ID="Answers" runat="server" RepeatLayout="Flow" />
              </div>
            </ItemTemplate>
          </asp:Repeater>
          <asp:Button ID="BtnSubmit" runat="server" Text="Submit answers" CssClass="btn" OnClick="BtnSubmit_Click" />
          <p class="bad"><asp:Literal ID="QuizMessage" runat="server" /></p>
        </asp:Panel>

        <%-- Result and the close --%>
        <asp:Panel ID="ResultPanel" runat="server" Visible="false" CssClass="card">
          <h2><asp:Literal ID="ScoreLiteral" runat="server" /></h2>

          <asp:Repeater ID="FeedbackRepeater" runat="server">
            <ItemTemplate><p class="bad"><%#: Container.DataItem %></p></ItemTemplate>
          </asp:Repeater>

          <h2>Your one action for the next 24 hours</h2>
          <p><asp:Literal ID="OneAction" runat="server" /></p>

          <h2>Now teach it</h2>
          <p><asp:Literal ID="TeachingPrompt" runat="server" /></p>
          <p class="sub">Your opening line: "<asp:Literal ID="NorthStar" runat="server" />"</p>

          <asp:Repeater ID="ResourcesRepeater" runat="server">
            <HeaderTemplate><h2>Learn more</h2></HeaderTemplate>
            <ItemTemplate><p><a href="<%#: Eval("Url") %>" target="_blank" rel="noopener"><%#: Eval("Title") %></a></p></ItemTemplate>
          </asp:Repeater>
        </asp:Panel>
      </asp:Panel>
    </div>
  </form>
</body>
</html>