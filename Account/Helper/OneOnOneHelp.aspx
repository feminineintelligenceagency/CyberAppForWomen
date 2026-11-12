<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="OneOnOneHelp.aspx.cs" Inherits="CyberApp_FIA.Helper.OneOnOneHelp" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • 1:1 Help Sessions</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <!-- Fonts -->
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

    *{
      box-sizing:border-box;
    }

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

    /* Top hero header (mirrors Schedule) */
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

    .page-header-main{
      max-width:640px;
    }

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
      font-size:1.7rem;
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

    .back-link span.icon{
      font-size:.95rem;
    }

    /* Log 1:1 help shell */
    .log-shell{
      margin-bottom:20px;
      padding:16px 18px 18px;
      border-radius:18px;
      border:1px solid var(--card-border);
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.12), transparent 55%),
        radial-gradient(circle at 100% 100%, rgba(42,153,219,0.14), transparent 55%),
        linear-gradient(135deg,#ffffff,#f6f9ff);
      box-shadow:0 14px 30px rgba(15,23,42,0.06);
    }

    .log-eyebrow{
      font-size:0.75rem;
      text-transform:uppercase;
      letter-spacing:0.12em;
      color:#6b7280;
      font-weight:700;
      margin-bottom:2px;
    }

    .log-title{
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.1rem;
      font-weight:600;
      color:#111827;
      margin:0;
    }

    .log-sub{
      margin:6px 0 0 0;
      color:var(--muted);
      font-size:.9rem;
      max-width:640px;
    }

    .log-form{
      margin-top:14px;
      display:grid;
      grid-template-columns:minmax(0,2.1fr) minmax(0,3fr) auto;
      gap:12px;
      align-items:flex-end;
    }

    .log-field-group{
      display:flex;
      flex-direction:column;
      gap:4px;
    }

    .log-label{
      font-size:.8rem;
      font-weight:600;
      color:#4b5563;
    }

    .log-select{
      width:100%;
      border-radius:999px;
      border:1px solid #d1d5db;
      padding:7px 11px;
      font-size:.9rem;
      background:#ffffff;
      outline:none;
    }

    .log-select:focus{
      border-color:var(--fia-blue);
      box-shadow:0 0 0 2px rgba(42,153,219,0.25);
    }

    .log-notes{
      width:100%;
      border-radius:12px;
      border:1px solid #d1d5db;
      padding:7px 10px;
      font-size:.9rem;
      resize:vertical;
      min-height:40px;
      max-height:120px;
      outline:none;
    }

    .log-notes:focus{
      border-color:var(--fia-pink);
      box-shadow:0 0 0 2px rgba(240,106,169,0.2);
    }

    .log-submit{
      appearance:none;
      border:none;
      cursor:pointer;
      padding:9px 14px;
      border-radius:999px;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.85rem;
      font-weight:600;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      color:#fff;
      box-shadow:0 8px 18px rgba(15,23,42,0.18);
      white-space:nowrap;
    }

    .log-submit:focus{
      outline:none;
      box-shadow:0 0 0 3px rgba(42,153,219,0.35);
    }

    .log-status{
      margin-top:8px;
      font-size:.85rem;
      color:#4b5563;
    }

    .log-status-error{
      color:#b91c1c;
    }

    /* Recent help history */
    .log-history{
      margin-top:16px;
      padding-top:10px;
      border-top:1px dashed rgba(148,163,184,0.6);
    }

    .log-history-header{
      font-size:.8rem;
      font-weight:600;
      text-transform:uppercase;
      letter-spacing:.12em;
      color:#6b7280;
      margin-bottom:6px;
    }

    .log-history-empty{
      font-size:.85rem;
      color:var(--muted);
    }

    .log-history-list{
      display:flex;
      flex-direction:column;
      gap:8px;
    }

    .log-history-item{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:10px;
      padding:8px 10px;
      border-radius:12px;
      background:linear-gradient(135deg,rgba(42,153,219,0.05),rgba(240,106,169,0.06));
      border:1px solid rgba(209,213,219,0.8);
    }

    .log-history-main{
      display:flex;
      flex-direction:column;
      gap:2px;
      min-width:0;
    }

    .log-history-title{
      font-size:.9rem;
      font-weight:600;
      color:#111827;
      white-space:nowrap;
      overflow:hidden;
      text-overflow:ellipsis;
    }

    .log-history-meta{
      font-size:.8rem;
      color:var(--muted);
    }

    .log-history-undo{
      appearance:none;
      border:none;
      cursor:pointer;
      padding:5px 10px;
      border-radius:999px;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.78rem;
      font-weight:600;
      background:#ffffff;
      color:var(--fia-blue);
      border:1px solid rgba(191,219,254,1);
      box-shadow:0 6px 14px rgba(15,23,42,0.08);
      white-space:nowrap;
      flex-shrink:0;
    }

    .log-history-undo:focus{
      outline:none;
      box-shadow:0 0 0 2px rgba(59,130,246,0.5);
    }

    /* Shell for participants list */
    .participants-shell{
      background:linear-gradient(135deg,#ffffff,#f9fbff);
      border-radius:18px;
      border:1px solid var(--card-border);
      padding:18px 18px 20px;
      box-shadow:0 14px 30px rgba(15,23,42,0.05);
    }

    .participants-header{
      margin:0 0 8px 0;
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.1rem;
      display:flex;
      align-items:center;
      gap:8px;
    }

    .participants-header-pill{
      padding:3px 9px;
      border-radius:999px;
      font-size:.75rem;
      text-transform:uppercase;
      letter-spacing:.06em;
      background:linear-gradient(135deg, rgba(42,153,219,0.12), rgba(240,106,169,0.12));
      color:#374151;
    }

    .participants-sub{
      margin:0 0 14px 0;
      color:var(--muted);
      font-size:.92rem;
    }

    .note{
      background:#f9fafb;
      border-radius:12px;
      border:1px solid #e5e7eb;
      padding:10px 12px;
      color:var(--muted);
      font-size:.9rem;
    }

    /* Grid for lots of participants; stays tidy as list grows */
    .participant-grid{
      display:grid;
      grid-template-columns:repeat(auto-fit, minmax(260px, 1fr));
      gap:14px;
      margin-top:10px;
    }

    .participant-card{
      border-radius:14px;
      border:1px solid #e5e7eb;
      background:linear-gradient(135deg,#ffffff,rgba(42,153,219,0.03));
      padding:12px 13px 13px;
      box-shadow:0 10px 22px rgba(15,23,42,0.05);
      display:flex;
      flex-direction:column;
      gap:4px;
      position:relative;
      overflow:hidden;
    }

    .participant-card::before{
      content:"";
      position:absolute;
      inset-inline-start:0;
      top:0;
      bottom:0;
      width:4px;
      background:linear-gradient(180deg,var(--fia-pink),var(--fia-teal));
      opacity:0.75;
    }

    .participant-header{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:8px;
    }

    .participant-name{
      font-family:Poppins, system-ui, sans-serif;
      font-size:1rem;
      font-weight:600;
      color:#111827;
      padding-inline-start:2px;
    }

    .participant-email{
      font-size:.9rem;
      color:var(--muted);
      padding-inline-start:2px;
      word-break:break-all;
    }

    .participant-meta{
      font-size:.85rem;
      color:var(--muted);
      padding-inline-start:2px;
      margin-top:2px;
    }

    /* Message indicator */
    .msg-indicator{
      display:inline-flex;
      align-items:center;
      gap:4px;
      padding:3px 8px;
      border-radius:999px;
      font-size:.72rem;
      font-weight:600;
      background:linear-gradient(135deg, rgba(240,106,169,0.10), rgba(42,153,219,0.08));
      color:#7a103a;
      border:1px solid rgba(240,106,169,0.35);
      white-space:nowrap;
    }

    .msg-dot{
      width:8px;
      height:8px;
      border-radius:999px;
      background:var(--fia-pink);
    }

    .participant-actions{
      margin-top:8px;
      display:flex;
      gap:8px;
      padding-inline-start:2px;
    }

    .btn-convo{
      display:inline-flex;
      align-items:center;
      justify-content:center;
      padding:7px 11px;
      border-radius:999px;
      border:1px solid #dbeafe;
      background:linear-gradient(135deg,#eff6ff,#ffffff);
      font-size:.85rem;
      font-weight:600;
      color:#1d4ed8;
      text-decoration:none;
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    .btn-convo:hover{
      border-color:#bfdbfe;
      box-shadow:0 10px 22px rgba(15,23,42,0.10);
    }

    @media (max-width:640px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
      .log-form{
        grid-template-columns:1fr;
        align-items:stretch;
      }
      .log-history-item{
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
          <h1 class="page-title">1:1 Help Sessions</h1>
          <p class="page-sub">
            These are the participants who are assigned to you for one-on-one support.
            Use this list when you schedule 1:1 time, walk through security tasks together,
            or log quick support moments that count toward your certification.
          </p>
        </div>
        <a href="<%: ResolveUrl("~/Account/Helper/Home.aspx") %>" class="back-link">
          <span class="icon">←</span>
          <span>Back to Helper Home</span>
        </a>
      </div>

      <!-- Log a 1:1 help session -->
      <div class="log-shell">
        <div>
          <div class="log-eyebrow">Certification progress</div>
          <h2 class="log-title">Log a one-on-one help session</h2>
          <p class="log-sub">
            Track one-to-one help you gave for a microcourse category so your support work counts toward certification.
            Add a short note about what you helped with.
          </p>
        </div>

        <div class="log-form">
          <div class="log-field-group">
            <span class="log-label">Microcourse category</span>
            <asp:DropDownList ID="HelpCourseDropDown"
                              runat="server"
                              CssClass="log-select" />
          </div>
          <div class="log-field-group">
            <span class="log-label">What did you help with?</span>
            <asp:TextBox ID="HelpDetailsTextBox"
                         runat="server"
                         CssClass="log-notes"
                         TextMode="MultiLine"
                         Rows="2"
                         placeholder="Example: Helped Abby set stronger privacy settings on Instagram."></asp:TextBox>
          </div>
          <div class="log-field-group">
            <asp:Button ID="HelpSubmitButton"
                        runat="server"
                        CssClass="log-submit"
                        Text="Log one-to-one help"
                        OnClick="HelpSubmitButton_Click" />
          </div>
        </div>

        <asp:Label ID="HelpStatusLabel" runat="server" CssClass="log-status" />

        <div class="log-history">
          <div class="log-history-header">Recent one-to-one help</div>

          <asp:PlaceHolder ID="HelpHistoryEmpty" runat="server">
            <div class="log-history-empty">
              Once you log help sessions, the last three will show here with quick undo.
            </div>
          </asp:PlaceHolder>

          <asp:Repeater ID="HelpHistoryRepeater"
                        runat="server"
                        OnItemCommand="HelpHistoryRepeater_ItemCommand">
            <HeaderTemplate>
              <div class="log-history-list">
            </HeaderTemplate>
            <ItemTemplate>
              <div class="log-history-item">
                <div class="log-history-main">
                  <div class="log-history-title"><%# Eval("CourseTitle") %></div>
                  <div class="log-history-meta"><%# Eval("WhenLabel") %></div>
                </div>
                <asp:Button ID="UndoHelpItemButton"
                            runat="server"
                            CssClass="log-history-undo"
                            Text="Undo"
                            CommandName="undoHelp"
                            CommandArgument='<%# Eval("Snapshot") %>' />
              </div>
            </ItemTemplate>
            <FooterTemplate>
              </div>
            </FooterTemplate>
          </asp:Repeater>
        </div>
      </div>

      <!-- Participants shell -->
      <div class="participants-shell">
        <div class="participants-header">
          Assigned participants
          <span class="participants-header-pill">1:1 list</span>
        </div>
        <p class="participants-sub">
          Each card shows a participant’s first name and email address so you can
          quickly reach out, schedule time, or look them up in another system.
          When a participant has sent you messages, you’ll see a small indicator next to their name.
        </p>

        <!-- Empty state -->
        <asp:PlaceHolder ID="NoParticipantsPH" runat="server" Visible="false">
          <div class="note">
            You don’t have any assigned participants yet. Once a University Admin connects
            participants to you as their Helper, they will appear in this list.
          </div>
        </asp:PlaceHolder>

        <!-- Assigned participants grid -->
        <asp:Repeater ID="ParticipantsRepeater" runat="server">
          <HeaderTemplate>
            <div class="participant-grid">
          </HeaderTemplate>

          <ItemTemplate>
            <div class="participant-card">
              <div class="participant-header">
                <div class="participant-name">
                  <%# Eval("FirstName") %>
                </div>

                <asp:PlaceHolder ID="HasConvoPH" runat="server"
                 Visible='<%# (bool)Eval("HasConversation") %>'>
  <span class="msg-indicator" title="This participant has an unread message.">
    <span class="msg-dot"></span>
    <span>New message</span>
  </span>
</asp:PlaceHolder>
              </div>

              <div class="participant-email">
                <%# Eval("Email") %>
              </div>

              <asp:PlaceHolder ID="UniversityPH" runat="server"
                               Visible='<%# !string.IsNullOrWhiteSpace(Convert.ToString(Eval("University"))) %>'>
                <div class="participant-meta">
                  <%# Eval("University") %>
                </div>
              </asp:PlaceHolder>

              <div class="participant-actions">
                <asp:HyperLink ID="ConversationsLink"
                               runat="server"
                               CssClass="btn-convo"
                               NavigateUrl='<%# Eval("ConversationsUrl") %>'>
                  Conversations
                </asp:HyperLink>
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



