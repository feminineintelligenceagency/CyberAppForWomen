<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="HelperConversation.aspx.cs" Inherits="CyberApp_FIA.Helper.HelperConversation" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Conversation Thread</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1f2933;
      --muted:#6b7280;
      --bg:#f3f5fb;
      --card-bg:#ffffff;
      --card-border:#e2e8f0;
    }

    *{ box-sizing:border-box; }

    body{
      margin:0;
      font-family:Lato, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.10), transparent 55%),
        radial-gradient(circle at 100% 100%, rgba(42,153,219,0.10), transparent 55%),
        var(--bg);
      color:var(--ink);
    }

    .wrap{
      max-width:1120px;
      margin:0 auto;
      padding:24px 16px 40px;
    }

    .page-header{
      border-radius:24px;
      padding:20px 22px 22px;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.15), transparent 55%),
        radial-gradient(circle at 100% 0, rgba(69,195,179,0.20), transparent 55%),
        linear-gradient(120deg,#fbfbff,#f3f7ff);
      border:1px solid rgba(226,232,240,0.9);
      box-shadow:0 18px 40px rgba(15,23,42,0.10);
      margin-bottom:24px;
      display:flex;
      justify-content:space-between;
      align-items:flex-start;
      gap:18px;
    }

    .page-header-main{ max-width:640px; }

    .page-eyebrow{
      font-size:0.8rem;
      letter-spacing:0.16em;
      text-transform:uppercase;
      color:#6b7280;
      font-weight:700;
      margin-bottom:4px;
      display:flex;
      align-items:center;
      gap:8px;
    }

    .page-eyebrow-pill{
      display:inline-flex;
      align-items:center;
      justify-content:center;
      width:22px;
      height:22px;
      border-radius:999px;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      color:#fff;
      font-size:0.7rem;
      font-family:Poppins,system-ui,sans-serif;
    }

    .page-title{
      margin:0;
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.6rem;
      color:#111827;
    }

    .page-title span{
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-pink));
      -webkit-background-clip:text;
      color:transparent;
    }

    .page-sub{
      margin:6px 0 0 0;
      color:var(--muted);
      font-size:.95rem;
      max-width:520px;
    }

    .back-link{
      display:inline-flex;
      align-items:center;
      gap:6px;
      padding:7px 11px;
      border-radius:999px;
      border:1px solid rgba(148,163,184,0.6);
      background:#ffffff;
      font-size:.85rem;
      font-weight:600;
      color:#374151;
      text-decoration:none;
      white-space:nowrap;
      box-shadow:0 10px 24px rgba(15,23,42,0.12);
    }

    .back-link span.icon{ font-size:.95rem; }

    .shell{
      background:linear-gradient(135deg,#ffffff,#f9fbff);
      border-radius:18px;
      border:1px solid var(--card-border);
      padding:18px 18px 20px;
      box-shadow:0 14px 30px rgba(15,23,42,0.05);
      display:grid;
      grid-template-columns:minmax(0, 3fr);
      gap:16px;
    }

    .thread-header{
      margin-bottom:10px;
    }

    .thread-topic{
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.05rem;
      margin:0 0 4px 0;
    }

    .thread-meta{
      margin:0;
      font-size:.85rem;
      color:var(--muted);
    }

    .messages-panel{
      border-radius:14px;
      border:1px solid #e5e7eb;
      background:#ffffff;
      padding:12px 12px 10px;
      max-height:420px;
      overflow-y:auto;
    }

    .messages-list{
      display:flex;
      flex-direction:column;
      gap:10px;
    }

    .message-row{
      display:flex;
      flex-direction:column;
      max-width:80%;
    }

    .message-row.helper{
      margin-left:auto;
      align-items:flex-end;
    }

    .message-row.participant{
      margin-right:auto;
      align-items:flex-start;
    }

    .message-bubble{
      border-radius:14px;
      padding:8px 11px;
      font-size:.9rem;
      line-height:1.4;
      box-shadow:0 6px 16px rgba(15,23,42,0.06);
    }

    .message-bubble.helper{
      background:linear-gradient(135deg,#2a99db,#6bc1f1);
      color:#ffffff;
    }

    .message-bubble.participant{
      background:#f9fafb;
      color:#111827;
      border:1px solid #e5e7eb;
    }

    .message-meta{
      margin-top:3px;
      font-size:.75rem;
      color:#9ca3af;
    }

    .reply-panel{
      margin-top:12px;
      border-radius:14px;
      border:1px solid #dbeafe;
      background:linear-gradient(135deg,#eff6ff,#ffffff);
      padding:10px 12px 12px;
      display:flex;
      flex-direction:column;
      gap:8px;
    }

    .reply-label{
      font-size:.85rem;
      font-weight:600;
      color:#1f2937;
    }

    .reply-text{
      width:100%;
      min-height:80px;
      border-radius:10px;
      border:1px solid #d1d5db;
      padding:8px 10px;
      font-family:inherit;
      font-size:.9rem;
      resize:vertical;
    }

    .reply-actions{
      display:flex;
      justify-content:flex-end;
      gap:8px;
    }

    .btn-send{
      appearance:none;
      border:none;
      border-radius:999px;
      padding:8px 14px;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.85rem;
      font-weight:600;
      cursor:pointer;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      color:#ffffff;
      box-shadow:0 8px 20px rgba(15,23,42,0.12);
    }

    .btn-send:focus{
      outline:none;
      box-shadow:0 0 0 3px rgba(59,130,246,0.35);
    }

    .form-message{
      margin-top:8px;
      font-size:.85rem;
    }

    @media (max-width:640px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }

      .shell{
        grid-template-columns:minmax(0, 1fr);
      }

      .messages-panel{
        max-height:360px;
      }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <asp:ScriptManager runat="server" />
    <asp:UpdatePanel runat="server">
    <ContentTemplate>
    <div class="wrap">

      <!-- Header -->
      <div class="page-header">
        <div class="page-header-main">
          <div class="page-eyebrow">
            <span class="page-eyebrow-pill">FIA</span>
            <span>Helper workspace</span>
          </div>
          <h1 class="page-title">
            Conversation with <span><asp:Literal ID="ParticipantName" runat="server" /></span>
          </h1>
          <p class="page-sub">
            Read the full one-on-one message chain and send a clear, encouraging reply.
            Keep messages focused on specific questions and next steps.
          </p>
        </div>
        <asp:HyperLink ID="BackToParticipantConversations" runat="server" CssClass="back-link">
          <span class="icon">←</span>
          <span>Back to conversations</span>
        </asp:HyperLink>
      </div>

      <div class="shell">
        <div>
          <div class="thread-header">
            <h2 class="thread-topic">
              <asp:Literal ID="Topic" runat="server" />
            </h2>
            <p class="thread-meta">
              Started
              <asp:Literal ID="CreatedOn" runat="server" />
              &nbsp;•&nbsp;
              Last updated
              <asp:Literal ID="LastUpdated" runat="server" />
            </p>
          </div>

          <div class="messages-panel">
            <asp:Repeater ID="MessagesRepeater" runat="server">
              <HeaderTemplate>
                <div class="messages-list">
              </HeaderTemplate>

              <ItemTemplate>
                <div class='message-row <%# Eval("FromRoleCss") %>'>
                  <div class='message-bubble <%# Eval("FromRoleCss") %>'>
                    <%# Eval("BodyHtml") %>
                  </div>
                  <div class="message-meta">
                    <%# Eval("FromLabel") %> • <%# Eval("SentOnLocal", "{0:MMM d, yyyy • h:mm tt}") %>
                  </div>
                </div>
              </ItemTemplate>

              <FooterTemplate>
                </div>
              </FooterTemplate>
            </asp:Repeater>
          </div>

          <div class="reply-panel">
            <div class="reply-label">Your reply</div>
            <asp:TextBox ID="ReplyText" runat="server" TextMode="MultiLine" CssClass="reply-text" />
            <div class="reply-actions">
              <asp:Button ID="SendReplyButton" runat="server" CssClass="btn-send"
                          Text="Send reply" OnClick="SendReplyButton_Click" />
            </div>
            <!-- FIXED: wrap Literal in a styled div; Literal itself has no CssClass -->
            <div class="form-message">
              <asp:Literal ID="FormMessage" runat="server" />
            </div>
          </div>
        </div>
      </div>

    </div>
  </ContentTemplate>
    </asp:UpdatePanel>
  </form>
</body>
</html>