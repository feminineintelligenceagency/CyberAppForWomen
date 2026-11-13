<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CertificationRules.aspx.cs" Inherits="CyberApp_FIA.Account.CertificationRules" MaintainScrollPositionOnPostBack="true"%>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Certification Rules</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <!-- Google Fonts -->
  <link
    href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap"
    rel="stylesheet" />

  <style>
    /* =========================
       Design tokens / base theme
       ========================= */
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

    /* =========================
       Page layout and typography
       ========================= */
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

    /* =========================
       Top header / hero
       ========================= */
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
      max-width:540px;
    }

    .page-header-side{
      display:flex;
      flex-direction:column;
      gap:10px;
      align-items:flex-end;
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
      display:inline-flex;
      align-items:center;
      gap:6px;
      text-align:center;
    }


    /* =========================
       Main grid and cards
       ========================= */
    .page-main{
      display:grid;
      grid-template-columns:minmax(0,1.15fr) minmax(0,1.1fr);
      gap:18px;
    }

    @media (max-width:960px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
      .page-main{
        grid-template-columns:1fr;
      }
    }
        /* Sprint 4 (#15): let cards shrink to the phone width */
    .page-main > *{ min-width:0; }

    .card{
      background:var(--card-bg);
      border-radius:20px;
      border:1px solid var(--card-border);
      box-shadow:0 14px 30px rgba(15,23,42,0.05);
      padding:18px 18px 20px;
    }

    .card-title{
      margin:0 0 4px 0;
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.1rem;
      display:flex;
      align-items:center;
      gap:8px;
    }

    .card-sub{
      margin:0 0 14px 0;
      color:var(--muted);
      font-size:0.9rem;
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

    /* =========================
       Form controls / layout
       ========================= */
    .form-group{
      margin-top:10px;
    }

    .form-label{
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.9rem;
      color:#111827;
      display:block;
      margin-bottom:3px;
    }

    .form-help{
      font-size:0.8rem;
      color:var(--muted);
      margin-top:2px;
    }

    .text-input,
    .textarea-input,
    .number-input{
      width:100%;
      padding:9px 11px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-family:inherit;
      font-size:0.9rem;
    }

    .text-input:focus,
    .textarea-input:focus,
    .number-input:focus{
      outline:0;
      box-shadow:0 0 0 3px var(--ring);
      border-color:var(--fia-blue);
    }

    .textarea-input{
      resize:vertical;
      min-height:70px;
    }

    .inline-grid-3{
      display:grid;
      grid-template-columns:repeat(3, minmax(0,1fr));
      gap:12px;
    }

    .quiz-toggle-row{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:12px;
      margin-top:10px;
      margin-bottom:4px;
    }

    .quiz-toggle-main{
      max-width:260px;
    }

    .fieldset{
      border-radius:14px;
      border:1px dashed #e5e7eb;
      padding:10px 11px 11px;
      margin-top:6px;
      background:#f9fafb;
    }

    /* =========================
       Buttons
       ========================= */
    .btn-row{
      display:flex;
      flex-wrap:wrap;
      gap:10px;
      margin-top:14px;
      align-items:center;
    }

    .btn{
      border:0;
      border-radius:999px;
      padding:9px 14px;
      font-weight:700;
      font-family:Poppins, system-ui, sans-serif;
      cursor:pointer;
      font-size:0.88rem;
      display:inline-flex;
      align-items:center;
      gap:6px;
    }

    .btn-primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
      box-shadow:0 10px 26px rgba(37,99,235,0.18);
    }

    .btn-secondary{
      background:#ffffff;
      color:var(--fia-blue);
      border:1px solid rgba(148,163,184,0.7);
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    .btn-destructive{
      background:#ffffff;
      color:#b91c1c;
      border:1px solid #fecaca;
      box-shadow:0 8px 18px rgba(248,113,113,0.20);
    }

    /* =========================
       Form message
       ========================= */
    .form-message{
      display:block;
      margin-top:10px;
      font-size:0.85rem;
      color:var(--muted);
    }

    /* =========================
       Rules table
       ========================= */
    .rules-table{
      width:100%;
      border-collapse:collapse;
      font-size:0.86rem;
    }

    .rules-table thead{
      background:#f9fafb;
    }

    .rules-table th,
    .rules-table td{
      padding:7px 8px;
      border-bottom:1px solid #edf2f7;
      text-align:left;
      vertical-align:top;
    }

    .rules-table th{
      font-family:Poppins, system-ui, sans-serif;
      font-weight:600;
      font-size:0.8rem;
      color:#4b5563;
      text-transform:uppercase;
      letter-spacing:.06em;
    }

    .rules-table tr:hover td{
      background:#f9fbff;
    }

    .pill-empty{
      display:inline-flex;
      align-items:center;
      padding:6px 10px;
      border-radius:999px;
      background:#f3f4f6;
      border:1px solid #e5e7eb;
      font-size:0.82rem;
      color:#4b5563;
    }

    .rules-edit-link{
      color:#2563eb;
      text-decoration:none;
      font-weight:600;
      font-size:0.82rem;
    }

    .rules-edit-link:hover{
      text-decoration:underline;
    }

    @media (max-width:720px){
      .rules-table{
        font-size:0.8rem;
      }
      .inline-grid-3{
        grid-template-columns:1fr;
      }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <!-- =========================
           Page header / hero
           ========================= -->
      <div class="page-header">
        <div class="page-header-main">
          <div class="page-eyebrow">
            <span class="page-eyebrow-pill">FIA</span>
            <span>Super Admin workspace</span>
          </div>
          <h1 class="page-title">Certification rules</h1>
          <p class="page-sub">
            Define what Helpers must complete to earn or keep a certification for each FIA cybersecurity microcourse.
            Rules stay stable over time so progress and audits remain clear.
          </p>
        </div>
        <div class="page-header-side">
          <a class="back-link" href="<%: ResolveUrl("~/Account/SuperAdmin/SuperAdminHome.aspx") %>">
            <span class="icon">←</span>
            <span>Back to Super Admin Home</span>
          </a>
          <div class="page-chip">
            <span>Tip:</span>
            <span>Use short, stable rule IDs (e.g., <strong>safe-banking-v1</strong>).</span>
          </div>
        </div>
      </div>

      <!-- =========================
           Main content grid
           ========================= -->
      <div class="page-main">

        <!-- ===== Rule editor (left card) ===== -->
        <div class="card">
          <div class="card-title">
            Add / Edit rule
            <span class="card-pill">Rule definition</span>
          </div>
          <p class="card-sub">
            Create or update a certification rule. Each rule can be linked to one or more microcourses and
            defines quiz, teaching, 1:1 help, and expiry requirements.
          </p>

          <!-- Rule ID -->
          <div class="form-group">
            <label for="RuleId" class="form-label">Rule ID (unique, stable)</label>
            <asp:TextBox ID="RuleId" runat="server" CssClass="text-input" />
            <div class="form-help">
              This ID is stored in XML and used to connect microcourses to the right certification logic.
            </div>
          </div>

          <!-- Name -->
          <div class="form-group">
            <label for="RuleName" class="form-label">Name</label>
            <asp:TextBox ID="RuleName" runat="server" CssClass="text-input" />
          </div>

          <!-- Description -->
          <div class="form-group">
            <label for="RuleDesc" class="form-label">Description</label>
            <asp:TextBox ID="RuleDesc" runat="server" TextMode="MultiLine" Rows="3" CssClass="textarea-input" />
            <div class="form-help">
              Briefly describe what this certification covers (e.g., “Safe online banking basics”).
            </div>
          </div>

          <!-- Quiz requirement -->
          <div class="form-group">
            <div class="quiz-toggle-row">
              <div class="quiz-toggle-main">
                <span class="form-label">Quiz requirement</span>
                <div class="form-help">
                  Toggle on if this certification requires a quiz score.
                </div>
              </div>
              <div>
                <asp:CheckBox ID="RequireQuiz"
                              runat="server"
                              AutoPostBack="true"
                              OnCheckedChanged="RequireQuiz_CheckedChanged" />
                <span class="form-help">Require quiz</span>
              </div>
            </div>

            <asp:Panel ID="PassScorePanel" runat="server" CssClass="fieldset">
              <label for="PassScore" class="form-label">Pass score %</label>
              <asp:TextBox ID="PassScore" runat="server" TextMode="Number" CssClass="number-input" />
              <div class="form-help">
                Enter a value between 0–100. Leave 0 if you are still drafting this rule.
              </div>
            </asp:Panel>
          </div>

          <!-- Numeric fields: teaching sessions, 1:1 help, expiry -->
          <div class="form-group">
            <div class="inline-grid-3">
              <div>
                <label for="MinSessions" class="form-label">Teaching sessions (min)</label>
                <asp:TextBox ID="MinSessions" runat="server" TextMode="Number" CssClass="number-input" />
              </div>
              <div>
                <label for="HelpSessions" class="form-label">1:1 help sessions (min)</label>
                <asp:TextBox ID="HelpSessions" runat="server" TextMode="Number" CssClass="number-input" />
              </div>
              <div>
                <label for="ExpiryDays" class="form-label">Expiry (days)</label>
                <asp:TextBox ID="ExpiryDays" runat="server" TextMode="Number" CssClass="number-input" />
                <div class="form-help">
                  How long the certification stays valid once earned.
                </div>
              </div>
            </div>
          </div>

          <!-- Form actions: Save / Clear / Delete -->
          <div class="btn-row">
            <asp:Button
              ID="BtnSave"
              runat="server"
              CssClass="btn btn-primary"
              Text="Save rule"
              OnClick="BtnSave_Click" />

            <asp:Button
              ID="BtnClear"
              runat="server"
              CssClass="btn btn-secondary"
              Text="Clear"
              OnClick="BtnClear_Click"
              CausesValidation="false" />

            <asp:Button
              ID="BtnDelete"
              runat="server"
              CssClass="btn btn-destructive"
              Text="Delete"
              OnClick="BtnDelete_Click"
              CausesValidation="false" />
          </div>

          <!-- Inline form status / messages -->
          <asp:Label ID="FormMessage" runat="server" CssClass="form-message" />
        </div>

        <!-- ===== Rules list (right card) ===== -->
        <div class="card">
          <div class="card-title">
            Existing rules
            <span class="card-pill">Overview</span>
          </div>
          <p class="card-sub">
            Select a rule to edit its thresholds. Changes will apply the next time helper progress
            is recalculated for linked microcourses.
          </p>

          <!-- Empty state when there are no rules yet -->
          <asp:PlaceHolder ID="NoRulesPH" runat="server" Visible="false">
            <div class="pill-empty">
              No rules yet. Create your first certification rule using the form on the left.
            </div>
          </asp:PlaceHolder>

          <!-- Rules table rendered with a Repeater -->
          <asp:Repeater ID="RulesRepeater" runat="server" OnItemCommand="RulesRepeater_ItemCommand">
            <HeaderTemplate>
              <table class="rules-table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Name</th>
                    <th>Requires quiz?</th>
                    <th>Pass</th>
                    <th>Teaching sessions</th>
                    <th>1:1 help</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
            </HeaderTemplate>

            <ItemTemplate>
              <tr>
                <td><%# Eval("id") %></td>
                <td><%# Eval("name") %></td>
                <td><%# Eval("requireQuizText") %></td>
                <td><%# Eval("passScore") %>%</td>
                <td><%# Eval("minSessions") %></td>
                <td><%# Eval("minHelpSessions") %></td>
                <td>
                  <asp:LinkButton
                    runat="server"
                    CommandName="edit"
                    CommandArgument='<%# Eval("id") %>'
                    Text="Edit"
                    CssClass="rules-edit-link" />
                </td>
              </tr>
            </ItemTemplate>

            <FooterTemplate>
                </tbody>
              </table>
            </FooterTemplate>
          </asp:Repeater>
        </div>
      </div>

    </div>
  </form>
</body>
</html>
