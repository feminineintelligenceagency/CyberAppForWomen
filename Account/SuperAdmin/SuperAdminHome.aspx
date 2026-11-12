<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SuperAdminHome.aspx.cs" Inherits="CyberApp_FIA.Account.SuperAdminHome" MaintainScrollPositionOnPostBack="true"%>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Super Admin</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1c1c1c;
      --muted:#6b7280;
      --bg:#f3f5fb;
      --card-bg:#ffffff;
      --card-border:#e2e8f0;
      --ring:rgba(42,153,219,.25);
    }

    *{
      box-sizing:border-box;
    }

    html,body{
      height:100%;
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
      min-height:100vh;
      padding:24px 16px 40px;
      max-width:1120px;
      margin:0 auto;
    }

    /* ---------- Hero header ---------- */
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
      max-width:620px;
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
    }

    .page-admin-line{
      margin-top:8px;
      font-size:0.9rem;
      color:#4b5563;
    }

    .page-header-side{
      display:flex;
      flex-direction:column;
      gap:10px;
      align-items:flex-end;
      flex-shrink:0;
    }

    /* UPDATED: more FIA color for the “scope” chip */
    .page-chip{
      padding:6px 10px;
      border-radius:999px;
      border:1px solid rgba(42,153,219,0.55);
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.18), transparent 60%),
        radial-gradient(circle at 100% 100%, rgba(69,195,179,0.20), transparent 55%),
        #f0f9ff;
      font-size:.8rem;
      color:#0f172a;
      max-width:260px;
    }

    /* ---------- Buttons ---------- */
    .btn{
      border-radius:999px;
      padding:10px 16px;
      font-weight:700;
      font-family:Poppins, system-ui, sans-serif;
      cursor:pointer;
      font-size:.85rem;
      text-decoration:none;
      display:inline-flex;
      align-items:center;
      justify-content:center;
      white-space:nowrap;
      border:0;
    }

    .btn-primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
      box-shadow:0 10px 26px rgba(37,99,235,0.18);
    }

    .btn-secondary{
      background:#ffffff;
      border:1px solid rgba(148,163,184,0.7);
      color:#1f2933;
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    .header-btn-row{
      display:flex;
      gap:8px;
      flex-wrap:wrap;
    }

    /* ---------- Layout grid below hero ---------- */
    .layout-grid{
      display:grid;
      grid-template-columns:minmax(0,2fr) minmax(0,1.4fr);
      gap:18px;
      align-items:flex-start;
    }

    @media (max-width:900px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
      .layout-grid{
        grid-template-columns:1fr;
      }
    }

    /* ---------- Cards & typography ---------- */
    .card{
      background:var(--card-bg);
      border:1px solid var(--card-border);
      border-radius:20px;
      box-shadow:0 14px 30px rgba(15,23,42,0.05);
      padding:20px 20px 22px;
      margin-bottom:16px;
    }

    .card-title{
      font-family:Poppins, system-ui, sans-serif;
      margin:0 0 6px 0;
      font-size:1.15rem;
      display:flex;
      align-items:center;
      gap:8px;
    }

    .card-pill{
      padding:3px 8px;
      border-radius:999px;
      font-size:.75rem;
      text-transform:uppercase;
      letter-spacing:.06em;
      background:linear-gradient(135deg, rgba(42,153,219,0.12), rgba(240,106,169,0.12));
      color:#374151;
    }

    .card-sub{
      color:var(--muted);
      margin:0 0 12px 0;
      font-size:.9rem;
    }

    .note{
      background:#f9fafb;
      border:1px solid #e5e7eb;
      border-radius:12px;
      padding:10px 12px;
      color:var(--muted);
      font-size:.9rem;
      margin-bottom:10px;
    }

    /* ---------- Form controls ---------- */
    label{
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.9rem;
      display:block;
      margin-bottom:4px;
      color:#111827;
    }

    input[type=text],
    input[type=url],
    textarea,
    select{
      width:100%;
      padding:10px 12px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-family:inherit;
      font-size:.9rem;
    }

    textarea{
      min-height:110px;
      resize:vertical;
    }

    input[type=text]:focus,
    input[type=url]:focus,
    textarea:focus,
    select:focus{
      outline:0;
      box-shadow:0 0 0 3px var(--ring);
      border-color:var(--fia-blue);
    }

    .form-grid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:14px;
    }

    @media (max-width:800px){
      .form-grid{
        grid-template-columns:1fr;
      }
    }

    .btnrow{
      display:flex;
      gap:10px;
      flex-wrap:wrap;
      margin-top:14px;
    }

    .val{
      color:#c21d1d;
      font-size:.9rem;
      margin-top:4px;
      display:block;
    }

    .fieldset{
      border:1px dashed #e5e7eb;
      border-radius:14px;
      padding:12px;
      margin-top:4px;
    }

    .cbgrid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:8px;
    }

    @media (max-width:800px){
      .cbgrid{
        grid-template-columns:1fr;
      }
    }

    /* Prereq / rules checkbox lists, stacked one per line */
    .cblist table{
      width:100%;
      border-collapse:collapse;
    }

    .cblist td{
      padding:8px 6px;
      border-bottom:1px dashed #e5e7eb;
      vertical-align:middle;
    }

    .cblist input{
      margin-right:8px;
    }

    .nav-card-list{
      display:flex;
      flex-direction:column;
      gap:16px;
    }

        /* ---------- Security alert banner ---------- */
    .security-alert{
      border-radius:18px;
      padding:12px 16px;
      background:#fff7ed;
      border:1px solid #f97316;
      color:#7c2d12;
      display:flex;
      align-items:flex-start;
      justify-content:space-between;
      gap:12px;
      margin-bottom:18px;
      box-shadow:0 10px 26px rgba(248,113,22,0.25);
    }

    .security-alert-main{
      font-size:.9rem;
    }

    .security-alert-title{
      font-family:Poppins, system-ui, sans-serif;
      font-weight:600;
      margin-bottom:4px;
      font-size:1rem;
    }

    .security-alert-badge{
      display:inline-flex;
      align-items:center;
      justify-content:center;
      padding:2px 8px;
      border-radius:999px;
      font-size:.7rem;
      text-transform:uppercase;
      letter-spacing:.08em;
      background:#fed7aa;
      color:#7c2d12;
      margin-right:6px;
    }


  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <!-- Hero header -->
      <div class="page-header">
        <div class="page-header-main">
          <div class="page-eyebrow">
            <span class="page-eyebrow-pill">FIA</span>
            <span>Super Admin workspace</span>
          </div>
          <h1 class="page-title">Super Admin Home</h1>
          <p class="page-sub">
            Create and maintain microcourses, connect certification rules, and open system tools
            for audit and configuration across all universities.
          </p>
          <div class="page-admin-line">
            Signed in as <strong><asp:Literal ID="WelcomeName" runat="server" /></strong>
          </div>
        </div>

        <div class="page-header-side">
          <div class="page-chip">
            Manage global content, rules, and security for every FIA campus from one place.
          </div>
          <div class="header-btn-row">
            <asp:Button
              ID="BtnLogout"
              runat="server"
              Text="Sign out"
              CssClass="btn btn-secondary"
              OnClick="BtnLogout_Click"
              CausesValidation="false" />
          </div>
        </div>
      </div>

         <!-- SECURITY ALERT: only shown when there is new critical activity -->
      <asp:Panel ID="SecurityAlertPanel" runat="server" Visible="false">
        <div class="security-alert">
          <div class="security-alert-main">
            <div class="security-alert-title">
              <span class="security-alert-badge">Security</span>
              New critical activity detected
            </div>
            <div>
              We’ve detected new critical activity patterns in the system audit log
              since your last review. Please open the System audit log to investigate.
            </div>
          </div>
          <div>
            <a class="btn btn-primary"
               href="<%: ResolveUrl("~/Account/SuperAdmin/SuperAdminAudit.aspx") %>">
              Review audit log
            </a>
          </div>
        </div>
      </asp:Panel>

      <!-- Main layout: left = microcourse form, right = nav cards -->
      <div class="layout-grid">

        <!-- Left: microcourse creation form -->
        <div>
          <div class="card">
            <div class="card-title">
              Add a new microcourse
              <span class="card-pill">Catalog</span>
            </div>
            <p class="card-sub">
              Define a clear title, summary, and metadata so University Admins can easily adopt and schedule
              this cybersecurity microcourse on their campus.
            </p>

            <div class="note">
              Keep titles concrete (e.g., “Safe Online Banking Practices”) and summaries focused on what
              participants will be able to do afterward.
            </div>

            <div class="form-grid">
              <!-- Title -->
              <div>
                <label for="Title">Title</label>
                <asp:TextBox ID="Title" runat="server" />
                <asp:RequiredFieldValidator
                  runat="server"
                  ControlToValidate="Title"
                  CssClass="val"
                  ErrorMessage="Title is required."
                  Display="Dynamic" />
              </div>

             <!-- Duration -->
<div>
  <label for="Duration">Duration (minutes)</label>
  <asp:TextBox
    ID="Duration"
    runat="server"
    TextMode="Number"
    placeholder="e.g., 60" />
  <asp:RequiredFieldValidator
    runat="server"
    ControlToValidate="Duration"
    CssClass="val"
    ErrorMessage="Duration in minutes is required."
    Display="Dynamic" />
  <asp:RegularExpressionValidator
    runat="server"
    ControlToValidate="Duration"
    CssClass="val"
    ErrorMessage="Enter duration as a whole number of minutes, such as 60."
    ValidationExpression="^[1-9][0-9]*$"
    Display="Dynamic" />
</div>

              <!-- Summary -->
              <div style="grid-column:1/-1">
                <label for="Summary">Summary</label>
                <asp:TextBox ID="Summary" runat="server" TextMode="MultiLine" />
                <asp:RequiredFieldValidator
                  runat="server"
                  ControlToValidate="Summary"
                  CssClass="val"
                  ErrorMessage="Summary is required."
                  Display="Dynamic" />
              </div>

              <!-- External link -->
              <div style="grid-column:1/-1">
                <label for="ExternalLink">External link (Google Classroom Link)</label>
                <asp:TextBox ID="ExternalLink" runat="server" TextMode="Url" />
              </div>

              <!-- Tags -->
              <div>
                <label for="Tags">Tags (comma-separated)</label>
                <asp:TextBox ID="Tags" runat="server" />
              </div>

              <!-- Status -->
              <div>
                <label for="Status">Status</label>
                <asp:DropDownList ID="Status" runat="server">
                  <asp:ListItem Text="Draft" Value="Draft" />
                  <asp:ListItem Text="Published" Value="Published" />
                 
                </asp:DropDownList>
              </div>

              <!-- Certification rules multi-select -->
              <div style="grid-column:1/-1">
                <label>Certification rules required (multi-select)</label>
                <div class="fieldset">
                  <!-- UPDATED: stacked, one-per-line rules -->
                  <asp:CheckBoxList
                    ID="RulesList"
                    runat="server"
                    CssClass="cblist"
                    RepeatLayout="Table"
                    RepeatDirection="Vertical"
                    RepeatColumns="1"
                    CellPadding="0"
                    CellSpacing="0" />
                  <div class="note" style="margin-top:8px;">
                    Selected rules become prerequisites Helpers must satisfy to be certified for this microcourse.
                  </div>
                </div>
              </div>

              <!-- Prerequisites from existing microcourses -->
              <div style="grid-column:1/-1">
                <label>Prerequisites (existing microcourses)</label>
                <div class="fieldset">
                  <asp:CheckBoxList
                    ID="PrereqList"
                    runat="server"
                    CssClass="cblist"
                    RepeatLayout="Table"
                    RepeatDirection="Vertical"
                    RepeatColumns="1"
                    CellPadding="0"
                    CellSpacing="0" />
                  <div class="note" style="margin-top:8px;">
                    Choose any microcourses learners should complete before starting this one.
                  </div>
                </div>
              </div>
            </div>

            <!-- Actions -->
            <div class="btnrow">
              <asp:Button
                ID="BtnSaveMicrocourse"
                runat="server"
                Text="Save microcourse"
                CssClass="btn btn-primary"
                OnClick="BtnSaveMicrocourse_Click" />
              <asp:Button
                ID="BtnClear"
                runat="server"
                Text="Clear"
                CssClass="btn btn-secondary"
                OnClick="BtnClear_Click"
                CausesValidation="false" />
            </div>

            <!-- Status / error message -->
            <div style="margin-top:10px;">
              <asp:Label ID="FormMessage" runat="server" EnableViewState="false" />
            </div>
          </div>
        </div>

        <!-- Right: navigation / tools cards -->
        <div class="nav-card-list">
          <!-- Manage existing microcourses -->
          <div class="card">
            <div class="card-title">
              Manage existing microcourses
              <span class="card-pill">Catalog tools</span>
            </div>
            <p class="card-sub">
              Open the full microcourse list to edit content, adjust status, and review connected rules or prerequisites.
            </p>
            <a class="btn btn-secondary" href="<%: ResolveUrl("~/Account/SuperAdmin/SuperAdminMicrocourses.aspx") %>">
              Open microcourse manager
            </a>
          </div>

          <!-- Certification Rules -->
          <div class="card">
            <div class="card-title">
              Certification rules
              <span class="card-pill">Requirements</span>
            </div>
            <p class="card-sub">
              Create and edit certification rules that define quiz scores, teaching sessions, 1:1 help, and expiry
              for each Helper certification.
            </p>
            <a class="btn btn-secondary" href="<%: ResolveUrl("~/Account/SuperAdmin/CertificationRules.aspx") %>">
              Open certification rules
            </a>
          </div>

            <!-- NEW: Create University Admin accounts -->
          <div class="card">
            <div class="card-title">
              Create University Admin
              <span class="card-pill">Access</span>
            </div>
            <p class="card-sub">
              Add a new University Admin account and link them to the correct campus so they can manage helpers,
              events, and participant activity locally.
            </p>
            <a class="btn btn-secondary"
               href="<%: ResolveUrl("~/Account/SuperAdmin/CreateUniversityAdmin.aspx") %>">
              Open Create University Admin
            </a>
          </div>

            <!-- NEW: Password reset tool -->
<div class="card">
  <div class="card-title">
    Password reset
    <span class="card-pill">Access</span>
  </div>
  <p class="card-sub">
    Enter a user’s email address and a new password to reset their account login.
    The system updates passwordHash and passwordSalt in users.xml.
  </p>
  <a class="btn btn-secondary"
     href="<%: ResolveUrl("~/Account/SuperAdmin/PasswordReset.aspx") %>">
    Open password reset
  </a>
</div>

          <!-- System Audit Log -->
          <div class="card">
            <div class="card-title">
              System audit log
              <span class="card-pill">Security</span>
            </div>
            <p class="card-sub">
              View a read-only, filterable audit log across all universities for security review and troubleshooting.
            </p>
            <a class="btn btn-secondary" href="<%: ResolveUrl("~/Account/SuperAdmin/SuperAdminAudit.aspx") %>">
              Open audit log
            </a>
          </div>
        </div>



      </div>
    </div>
  </form>
</body>
</html>




