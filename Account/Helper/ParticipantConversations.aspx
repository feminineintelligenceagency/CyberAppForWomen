<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ParticipantConversations.aspx.cs" Inherits="CyberApp_FIA.Helper.ParticipantConversations" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Participant Conversations</title>
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
    }

    .shell-header{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:10px;
      margin-bottom:10px;
    }

    .shell-title{
      margin:0;
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.1rem;
    }

    .shell-pill{
      padding:3px 9px;
      border-radius:999px;
      font-size:.75rem;
      text-transform:uppercase;
      letter-spacing:.06em;
      background:linear-gradient(135deg, rgba(42,153,219,0.12), rgba(240,106,169,0.12));
      color:#374151;
    }

    .shell-sub{
      margin:0 0 12px 0;
      color:var(--muted);
      font-size:.9rem;
    }

    .note{
      background:#f9fafb;
      border-radius:12px;
      border:1px solid #e5e7eb;
      padding:10px 12px;
      color:var(--muted);
      font-size:.9rem;
    }

    .conversations-list{
      display:flex;
      flex-direction:column;
      gap:10px;
      margin-top:8px;
    }

    .conversation-card{
      border-radius:14px;
      border:1px solid #e5e7eb;
      background:linear-gradient(135deg,#ffffff,rgba(42,153,219,0.03));
      padding:11px 13px;
      box-shadow:0 10px 22px rgba(15,23,42,0.05);
      display:flex;
      flex-direction:column;
      gap:4px;
    }

    .conversation-topic{
      font-family:Poppins, system-ui, sans-serif;
      font-size:.98rem;
      font-weight:600;
      color:#111827;
    }

    .conversation-meta{
      font-size:.83rem;
      color:var(--muted);
    }

    .conversation-actions{
      margin-top:6px;
      display:flex;
      gap:8px;
    }

    .btn-view{
      display:inline-flex;
      align-items:center;
      justify-content:center;
      padding:6px 10px;
      border-radius:999px;
      border:1px solid #dbeafe;
      background:linear-gradient(135deg,#eff6ff,#ffffff);
      font-size:.8rem;
      font-weight:600;
      color:#1d4ed8;
      text-decoration:none;
    }

    .btn-view:hover{
      border-color:#bfdbfe;
      box-shadow:0 8px 18px rgba(15,23,42,0.08);
    }

    @media (max-width:640px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <!-- Header -->
      <div class="page-header">
        <div class="page-header-main">
          <div class="page-eyebrow">
            <span class="page-eyebrow-pill">FIA</span>
            <span>Helper workspace</span>
          </div>
          <h1 class="page-title">
            Conversations with <span><asp:Literal ID="ParticipantName" runat="server" /></span>
          </h1>
          <p class="page-sub">
            Here you can scan all one-on-one conversations you have with this participant
            and open any thread to read the full message chain and send a response.
          </p>
        </div>
        <a href="<%: ResolveUrl("~/Account/Helper/OneOnOneHelp.aspx") %>" class="back-link">
          <span class="icon">←</span>
          <span>Back to 1:1 list</span>
        </a>
      </div>

      <div class="shell">
        <div class="shell-header">
          <h2 class="shell-title">Conversation threads</h2>
          <span class="shell-pill">Messages</span>
        </div>
        <p class="shell-sub">
          Each card shows the topic and last updated time. Click “View thread” to open the
          full conversation and write back to the participant.
        </p>

        <asp:PlaceHolder ID="NoConversationsPH" runat="server" Visible="false">
          <div class="note">
            You don’t have any conversations with this participant yet.
            Once they send you a message, their threads will appear here.
          </div>
        </asp:PlaceHolder>

        <asp:Repeater ID="ConversationsRepeater" runat="server">
          <HeaderTemplate>
            <div class="conversations-list">
          </HeaderTemplate>

          <ItemTemplate>
            <div class="conversation-card">
              <div class="conversation-topic">
                <%# Eval("Topic") %>
              </div>
              <div class="conversation-meta">
                Started
                <span><%# Eval("CreatedOnLocal", "{0:MMM d, yyyy • h:mm tt}") %></span>
                &nbsp;•&nbsp;
                Last updated
                <span><%# Eval("LastUpdatedLocal", "{0:MMM d, yyyy • h:mm tt}") %></span>
              </div>
              <div class="conversation-actions">
                <a class="btn-view" href='<%# Eval("ViewUrl") %>'>View thread</a>
              </div>
            </div>
          </ItemTemplate>

          <FooterTemplate>
            </div>
          </FooterTemplate>
        </asp:Repeater>
      </div>

    </div>
  </form>
</body>
</html>
