<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="UniversityAdminHome.aspx.cs"
    Inherits="CyberApp_FIA.Account.UniversityAdminHome" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>FIA • University Admin Home</title>
  <meta charset="utf-8" />

  <!-- FIA fonts -->
  <link
    href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap"
    rel="stylesheet" />

  <style type="text/css">
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1e2640;
      --muted:#5b6477;
      --bg:#f3f5fb;
      --card-border:#e2e8f0;
      --ring:rgba(42,153,219,.25);
    }

    body{
      font-family:Lato, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      margin:0;
      padding:0;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.10), transparent 55%),
        radial-gradient(circle at 100% 100%, rgba(42,153,219,0.10), transparent 55%),
        var(--bg);
      color:var(--ink);
    }

    .fia-shell{
      max-width:1100px;
      margin:0 auto;
      padding:24px 16px 48px 16px;
    }

    /* Header */
    .fia-header{
      display:flex;
      justify-content:space-between;
      align-items:center;
      margin-bottom:24px;
      gap:14px;
    }

    .fia-header-title{
      font-size:1.5rem;
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      margin:0;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-pink));
      -webkit-background-clip:text;
      background-clip:text;
      color:transparent;
    }

    .fia-subtitle{
      font-size:0.85rem;
      color:var(--muted);
      margin-top:4px;
      display:flex;
      flex-wrap:wrap;
      align-items:center;
      gap:6px;
    }

    .fia-badge{
      display:inline-flex;
      align-items:center;
      padding:4px 10px;
      border-radius:999px;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      color:#ffffff;
      font-size:0.72rem;
      font-weight:600;
      letter-spacing:.04em;
      text-transform:uppercase;
    }

    /* Buttons */
    .fia-btn{
      border:none;
      border-radius:999px;
      padding:9px 16px;
      font-size:0.85rem;
      cursor:pointer;
      background:linear-gradient(135deg,#f06aa9,#2a99db);
      color:#fff;
      font-family:Poppins, system-ui, sans-serif;
      font-weight:700;
      display:inline-flex;
      align-items:center;
      justify-content:center;
      white-space:nowrap;
    }

    .fia-btn-secondary{
      border:1px solid #d0d5e6;
      background:#ffffff;
      color:#1e2640;
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    /* Layout rows + cards */
    .fia-row{
      display:flex;
      flex-wrap:wrap;
      gap:16px;
      margin-bottom:24px;
    }

    .fia-card{
      background:#ffffff;
      border-radius:20px;
      padding:16px 18px 18px 18px;
      box-shadow:0 12px 32px rgba(15,23,42,0.06);
      flex:1 1 260px;
      min-width:260px;
      border:1px solid var(--card-border);
    }

    .fia-card-title{
      font-size:1rem;
      font-weight:600;
      color:#1e2640;
      margin-bottom:4px;
      font-family:Poppins, system-ui, sans-serif;
    }

    .fia-card-body{
      font-size:0.85rem;
      color:#4b5563;
      margin-bottom:12px;
    }

    /* Form grid + inputs */
    .fia-form-grid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:12px 16px;
      margin-top:12px;
    }

    .fia-form-grid .full{
      grid-column:1 / -1;
    }

    .fia-label{
      display:block;
      font-size:0.78rem;
      font-weight:600;
      color:#4b5563;
      margin-bottom:4px;
      font-family:Poppins, system-ui, sans-serif;
    }

    .fia-input,
    .fia-textarea{
      width:100%;
      box-sizing:border-box;
      border-radius:12px;
      border:1px solid #d0d5e6;
      padding:9px 11px;
      font-size:0.85rem;
      font-family:inherit;
      background:#ffffff;
    }

    .fia-textarea{
      resize:vertical;
      min-height:60px;
    }

    .fia-input:focus,
    .fia-textarea:focus{
      outline:0;
      box-shadow:0 0 0 3px var(--ring);
      border-color:var(--fia-blue);
    }

    .fia-form-actions{
      margin-top:12px;
      display:flex;
      gap:8px;
      align-items:center;
      flex-wrap:wrap;
    }

    .fia-form-message{
      font-size:0.78rem;
      margin-top:6px;
      color:var(--muted);
    }

    /* Events */
    .fia-section-title{
      font-size:0.95rem;
      font-weight:600;
      color:#1e2640;
      margin:4px 0 8px 0;
      font-family:Poppins, system-ui, sans-serif;
    }

    .fia-empty{
      font-size:0.85rem;
      color:#6b7280;
      margin-top:8px;
    }

    .fia-event-list{
      margin-top:8px;
    }

    .fia-event-row{
      padding:10px 12px;
      border-radius:14px;
      border:1px solid #e2e8f0;
      display:flex;
      justify-content:space-between;
      align-items:center;
      font-size:0.85rem;
      margin-bottom:8px;
      background-color:#ffffff;
    }

    .fia-event-main{
      display:flex;
      flex-direction:column;
    }

    .fia-event-name{
      font-weight:600;
      color:#111827;
    }

    .fia-event-meta{
      font-size:0.8rem;
      color:#6b7280;
      margin-top:2px;
      display:flex;
      flex-wrap:wrap;
      align-items:center;
      gap:6px;
    }

    .fia-chip{
      padding:3px 9px;
      border-radius:999px;
      font-size:0.72rem;
      border:1px solid rgba(148,163,184,0.6);
      color:#374151;
      background:linear-gradient(135deg,rgba(42,153,219,0.08),rgba(240,106,169,0.08));
    }

    @media (max-width:768px){
      .fia-form-grid{
        grid-template-columns:1fr;
      }
      .fia-header{
        flex-direction:column;
        align-items:flex-start;
      }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <asp:HiddenField ID="UniversityValue" runat="server" />

    <div class="fia-shell">
      <!-- Header -->
      <div class="fia-header">
        <div>
          <div class="fia-header-title">
            University Admin Home
          </div>
          <div class="fia-subtitle">
            <span>Welcome, <asp:Literal ID="WelcomeName" runat="server" /></span>
            <span class="fia-badge">
              University:
              <asp:Literal ID="UniversityDisplay" runat="server" />
            </span>
          </div>
        </div>
        <div>
          <asp:Button ID="BtnLogout" runat="server"
            Text="Log out"
            CssClass="fia-btn fia-btn-secondary"
            OnClick="BtnLogout_Click" />
        </div>
      </div>

      <!-- Row: Audit log + helper section -->
      <div class="fia-row">
        <!-- Audit log navigation -->
        <div class="fia-card">
          <div class="fia-card-title">Helper Audit &amp; Activity</div>
          <div class="fia-card-body">
            Review helper logs and microcourse activity scoped to your university.
          </div>
          <asp:Button ID="BtnOpenAudit" runat="server"
            Text="Open Audit Log"
            CssClass="fia-btn"
            OnClick="BtnOpenAudit_Click" />
        </div>

        <!-- Add Helper navigation -->
        <div class="fia-card">
          <div class="fia-card-title">Add New Helper</div>
          <div class="fia-card-body">
            Create a helper account for your university by entering their basic details on the next screen.
          </div>
          <asp:Button ID="BtnAddHelper" runat="server"
            Text="Add Helper for My University"
            CssClass="fia-btn"
            OnClick="BtnAddHelper_Click" />
        </div>
      </div>

      <!-- Event creation + list -->
      <div class="fia-row">
        <!-- Create event -->
        <div class="fia-card">
          <div class="fia-card-title">Create Cyberfair Event</div>
          <div class="fia-card-body">
            Add a new cyberfair event for your university with a simple name, date, and description.
          </div>

          <div class="fia-form-grid">
            <div class="full">
              <label class="fia-label" for="EventName">Event name</label>
              <asp:TextBox ID="EventName" runat="server" CssClass="fia-input"></asp:TextBox>
            </div>
            <div>
              <label class="fia-label" for="EventStartDate">Start date & time</label>
              <asp:TextBox ID="EventStartDate" runat="server" CssClass="fia-input" TextMode="DateTimeLocal"></asp:TextBox>
            </div>
            <div>
              <label class="fia-label" for="EventEndDate">End date & time</label>
              <asp:TextBox ID="EventEndDate" runat="server" CssClass="fia-input" TextMode="DateTimeLocal"></asp:TextBox>
            </div>
            <div class="full">
              <label class="fia-label" for="Description">Description</label>
              <asp:TextBox ID="Description" runat="server" CssClass="fia-textarea" TextMode="MultiLine"></asp:TextBox>
            </div>
          </div>

          <div class="fia-form-actions">
            <asp:Button ID="BtnCreateEvent" runat="server"
              Text="Create event"
              CssClass="fia-btn"
              OnClick="BtnCreateEvent_Click" />
            <asp:Button ID="BtnClear" runat="server"
              Text="Clear"
              CssClass="fia-btn fia-btn-secondary"
              CausesValidation="false"
              OnClick="BtnClear_Click" />
          </div>

          <div class="fia-form-message">
            <asp:Literal ID="FormMessage" runat="server" />
          </div>
        </div>

        <!-- Event lists -->
<div class="fia-card">
  <div class="fia-section-title">Upcoming / Current Events for Your University</div>

  <asp:Panel ID="NoCurrentEventsPlaceholder" runat="server" Visible="false" CssClass="fia-empty">
    No current or upcoming events are scheduled right now.
  </asp:Panel>

  <div class="fia-event-list">
    <asp:Repeater ID="CurrentEventsRepeater" runat="server">
      <ItemTemplate>
        <div class="fia-event-row">
          <div class="fia-event-main">
            <span class="fia-event-name"><%# Eval("name") %></span>
            <span class="fia-event-meta">
              <span><%# Eval("dateHuman") %></span>
              <span class="fia-chip">
                Status: <%# Eval("status") %>
              </span>
            </span>
          </div>
          <div>
                        <a class="manage-link" href="<%# Eval("manageUrl") %>"
               style="font-size:12px; text-decoration:none; color:#2563eb;">
              Manage
            </a>
          </div>
        </div>
      </ItemTemplate>
    </asp:Repeater>
  </div>

  <div class="fia-section-title" style="margin-top:18px;">Past Events</div>

  <asp:Panel ID="NoPastEventsPlaceholder" runat="server" Visible="false" CssClass="fia-empty">
    No past events are available yet.
  </asp:Panel>

  <div class="fia-event-list">
    <asp:Repeater ID="PastEventsRepeater" runat="server">
      <ItemTemplate>
        <div class="fia-event-row">
          <div class="fia-event-main">
            <span class="fia-event-name"><%# Eval("name") %></span>
            <span class="fia-event-meta">
              <span><%# Eval("dateHuman") %></span>
              <span class="fia-chip">
                Status: <%# Eval("status") %>
              </span>
            </span>
          </div>
          <div>
            <a href="<%# Eval("manageUrl") %>"
               style="font-size:12px; text-decoration:none; color:#2563eb;">
              Manage
            </a>
          </div>
        </div>
      </ItemTemplate>
    </asp:Repeater>
  </div>
</div>
      </div>
    </div>
  </form>
</body>
</html>

