<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EventSessionEdit.aspx.cs" Inherits="CyberApp_FIA.Account.EventSessionEdit" MaintainScrollPositionOnPostBack="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Edit Session</title>
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

    *{box-sizing:border-box}
    html,body{height:100%}
    body{
      margin:0;
      font-family:Lato,Arial,sans-serif;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.10), transparent 55%),
        radial-gradient(circle at 100% 100%, rgba(42,153,219,0.10), transparent 55%),
        var(--bg);
      color:var(--ink);
    }

    .wrap{
      min-height:100vh;
      padding:24px 16px 40px;
      max-width:1100px;
      margin:0 auto;
    }

    /* ========= Hero header ========= */
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

    .page-meta{
      margin-top:10px;
      display:flex;
      flex-wrap:wrap;
      gap:6px;
    }

    .page-header-side{
      display:flex;
      flex-direction:column;
      gap:10px;
      align-items:flex-end;
      flex-shrink:0;
    }

    .page-chip{
      padding:6px 14px;
      border-radius:999px;
      border:1px solid rgba(42,153,219,0.55);
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.18), transparent 60%),
        radial-gradient(circle at 100% 100%, rgba(69,195,179,0.20), transparent 55%),
        #f0f9ff;
      font-size:.8rem;
      color:#0f172a;
      max-width:280px;
      text-align:center;
    }

    @media (max-width:900px){
      .page-header{
        flex-direction:column;
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
      .page-chip{
        text-align:left;
      }
    }

    /* ========= Cards ========= */
    .card{
      background:var(--card-bg);
      border:1px solid var(--card-border);
      border-radius:20px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);
      padding:20px;
      margin-bottom:16px;
    }

    .card h2{
      font-family:Poppins;
      margin:0 0 6px 0;
      font-size:1.2rem;
    }

    .sub{
      color:var(--muted);
      margin:0 0 16px 0;
      font-size:.9rem;
    }

    /* ========= Form controls ========= */
    label{
      font-weight:600;
      font-family:Poppins;
      font-size:.95rem;
      display:block;
      margin-bottom:4px;
    }
    input[type=text],
    input[type=datetime-local],
    input[type=number],
    textarea{
      width:100%;
      padding:12px 14px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-family:inherit;
      font-size:.9rem;
    }
    input:focus,
    textarea:focus{
      outline:0;
      box-shadow:0 0 0 5px var(--ring);
      border-color:var(--fia-blue);
    }

    .grid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:14px;
    }
    @media (max-width:900px){
      .grid{grid-template-columns:1fr;}
    }

    /* ========= Buttons ========= */
    .btn{
      border:0;
      border-radius:12px;
      padding:12px 18px;
      font-weight:700;
      font-family:Poppins;
      cursor:pointer;
      font-size:.9rem;
    }
    .btn.small{
      padding:8px 12px;
      font-size:.8rem;
    }
    .primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
    }
    .link{
      background:#fff;
      border:2px solid var(--fia-blue);
      color:var(--fia-blue);
    }
    .btnrow{
      display:flex;
      gap:10px;
      flex-wrap:wrap;
      margin-top:14px;
    }

    /* ========= Pills / notes ========= */
    .pill{
      display:inline-block;
      padding:6px 10px;
      border-radius:999px;
      background:#f6f7fb;
      border:1px solid #e8eef7;
      font-size:.85rem;
      color:#374151;
    }
    .note{
      background:#f6f7fb;
      border:1px solid #e8eef7;
      border-radius:12px;
      padding:12px;
      color:var(--muted);
      font-size:.95rem;
    }

    table{width:100%;border-collapse:collapse}
    th,td{
      padding:8px;
      border-bottom:1px solid #f0f3f9;
      text-align:left;
    }

    .val{
      color:#c21d1d;
      font-size:.9rem;
      margin-top:4px;
      display:block;
    }

    /* ========= Helper panel ========= */
    .helper-panel{
      margin-top:18px;
      border-top:1px solid #e8eef7;
      padding-top:12px;
    }
    .helper-panel h3{
      margin:0 0 4px 0;
      font-family:Poppins;
      font-size:1rem;
    }
    .helper-filters{
      margin-bottom:8px;
      display:flex;
      flex-direction:column;
      gap:6px;
      font-size:.9rem;
    }
    .helper-filters label{
      font-weight:400;
      font-family:Lato;
      font-size:.9rem;
    }

    .helper-filters-group{
      display:flex;
      flex-wrap:wrap;
      gap:8px;
      align-items:center;
    }
    .helper-filters-group .group-label{
      font-size:.8rem;
      text-transform:uppercase;
      letter-spacing:.08em;
      color:var(--muted);
      font-weight:600;
    }
    .helper-filters input[type=checkbox]{margin-right:4px;}

    .helper-status{
      display:inline-block;
      padding:4px 8px;
      border-radius:999px;
      font-size:.8rem;
      font-weight:600;
    }
    .helper-status.status-cert{
      background:rgba(22,163,74,.08);
      color:#15803d;
      border:1px solid rgba(22,163,74,.2);
    }
    .helper-status.status-eligible{
      background:rgba(37,99,235,.08);
      color:#1d4ed8;
      border:1px solid rgba(37,99,235,.25);
    }
    .helper-status.status-notcert{
      background:#fef2f2;
      color:#b91c1c;
      border:1px solid #fecaca;
    }
    .helper-status.status-overlap{
      background:#fef3c7;
      color:#92400e;
      border:1px solid #fcd34d;
    }

    .helper-table{
      width:100%;
      border-collapse:collapse;
      border-radius:14px;
      overflow:hidden;
      background:#fdfdff;
      box-shadow:0 10px 26px rgba(15,23,42,.04);
    }
    .helper-table thead th{
      background:linear-gradient(135deg,rgba(42,153,219,.08),rgba(240,106,169,.08));
      font-size:.8rem;
      letter-spacing:.08em;
      text-transform:uppercase;
      color:#4b5563;
      font-family:Poppins;
    }
    .helper-table tbody tr:nth-child(even) td{background:#f9fafb;}
    .helper-name-cell{font-weight:600;color:#111827;}

    .helper-select{
      width:100%;
      padding:10px 14px;
      border-radius:999px;
      border:1px solid #e5e7eb;
      background:#ffffff;
      font-family:Lato,Arial,sans-serif;
      font-size:.9rem;
    }
    .helper-select:focus{
      outline:0;
      box-shadow:0 0 0 4px var(--ring);
      border-color:var(--fia-blue);
    }

    .err{color:#c21d1d;font-size:.9rem;}
    .ok{color:#0a7a3c;font-size:.9rem;}
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
          <span>University Admin workspace</span>
        </div>
        <h1 class="page-title">
          Edit session for: <asp:Literal ID="EventName" runat="server" />
        </h1>
        <p class="page-sub">
          Adjust the course, timing, room, capacity, and helper for this session and review how changes
          impact enrolled and waitlisted participants.
        </p>
        <div class="page-meta">
          <span class="pill">University: <asp:Literal ID="University" runat="server" /></span>
          <span class="pill">Event date: <asp:Literal ID="EventDate" runat="server" /></span>
        </div>
      </div>

      <div class="page-header-side">
        <div class="page-chip">
          Check the impact summary before saving so you can communicate any changes clearly to participants.
        </div>
      </div>
    </div>

    <!-- Session edit card -->
    <div class="card">
      <h2>Session details</h2>
      <p class="sub">Update the course, time, room, capacity, or helper for this session.</p>

      <div class="grid">
        <div>
          <label for="CourseSelect">Course</label>
          <asp:DropDownList ID="CourseSelect" runat="server"
                            AutoPostBack="true"
                            OnSelectedIndexChanged="CourseSelect_SelectedIndexChanged" />
        </div>

        <div>
          <label for="SessionDateTimeStart">Start</label>
          <asp:TextBox ID="SessionDateTimeStart" runat="server" TextMode="DateTimeLocal"
                       AutoPostBack="true"
                       OnTextChanged="SessionTime_TextChanged" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="SessionDateTimeStart" CssClass="val"
            ErrorMessage="Start is required." Display="Dynamic" />
        </div>

        <div>
          <label for="SessionDateTimeEnd">End</label>
          <asp:TextBox ID="SessionDateTimeEnd" runat="server" TextMode="DateTimeLocal"
                       AutoPostBack="true"
                       OnTextChanged="SessionTime_TextChanged" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="SessionDateTimeEnd" CssClass="val"
            ErrorMessage="End is required." Display="Dynamic" />
        </div>

        <div>
          <label for="Room">Room / link (Enter Google Classroom Link)</label>
          <asp:TextBox ID="Room" runat="server" Placeholder="e.g., MU 201 or Zoom link" />
        </div>

        <div>
          <label for="Capacity">Max participants </label>
          <asp:TextBox ID="Capacity" runat="server" TextMode="Number" Placeholder="e.g., 25" />
        </div>
      </div>

      <!-- Helpers -->
      <div class="helper-panel">
        <h3>Helpers for this university</h3>
        <p class="sub">Status is based on the selected microcourse and the current time window.</p>

        <div class="helper-filters">
          <div class="helper-filters-group">
            <span class="group-label">Filter:</span>
            <asp:CheckBox ID="FilterEligible" runat="server" Text="Eligible only"
                          AutoPostBack="true"
                          OnCheckedChanged="HelperFilterChanged" />
            <asp:CheckBox ID="FilterCertified" runat="server" Text="Certified only"
                          AutoPostBack="true"
                          OnCheckedChanged="HelperFilterChanged" />
            <asp:Button ID="BtnClearHelperFilters" runat="server"
                        Text="Clear filters"
                        CssClass="btn link small"
                        CausesValidation="false"
                        OnClick="BtnClearHelperFilters_Click" />
          </div>

          <div class="helper-filters-group">
            <span class="group-label">Sort certified helpers:</span>
            <asp:CheckBox ID="SortByLastDelivered" runat="server"
                          Text="Most recently delivered this course"
                          AutoPostBack="true"
                          OnCheckedChanged="HelperSortChanged" />
            <asp:CheckBox ID="SortByMostDelivered" runat="server"
                          Text="Most sessions delivered for this course"
                          AutoPostBack="true"
                          OnCheckedChanged="HelperSortChanged" />
          </div>
        </div>

        <asp:PlaceHolder ID="NoHelpersPH" runat="server" Visible="false">
          <div class="note">No helpers were found yet for this university.</div>
        </asp:PlaceHolder>

        <asp:Repeater ID="HelpersRepeater" runat="server">
          <HeaderTemplate>
            <table class="helper-table">
              <thead>
                <tr>
                  <th>Helper</th>
                  <th>Certification</th>
                  <th>Schedule</th>
                </tr>
              </thead>
              <tbody>
          </HeaderTemplate>
          <ItemTemplate>
            <tr>
              <td class="helper-name-cell"><%# Eval("Name") %></td>
              <td>
                <span class="helper-status <%# Eval("CertCssClass") %>">
                  <%# Eval("CertLabel") %>
                </span>
              </td>
              <td>
                <!-- Current helper chip (initial view only) -->
                <asp:PlaceHolder ID="CurrentPH" runat="server"
                                 Visible='<%# (bool)Eval("IsCurrent") %>'>
                  <span class="helper-status">Current helper</span>
                </asp:PlaceHolder>

                <!-- Overlap shown only if not current helper -->
                <asp:PlaceHolder ID="OverlapPH" runat="server"
                                 Visible='<%# !(bool)Eval("IsCurrent") && (bool)Eval("HasOverlap") %>'>
                  <span class="helper-status status-overlap">Schedule overlap</span>
                </asp:PlaceHolder>

                <!-- Available shown only if not current helper and no overlap -->
                <asp:PlaceHolder ID="AvailablePH" runat="server"
                                 Visible='<%# !(bool)Eval("IsCurrent") && !(bool)Eval("HasOverlap") %>'>
                  <span class="helper-status">Available</span>
                </asp:PlaceHolder>
              </td>
            </tr>
          </ItemTemplate>
          <FooterTemplate>
              </tbody>
            </table>
          </FooterTemplate>
        </asp:Repeater>

        <!-- Helper selection -->
        <div style="margin-top:12px;">
          <label for="HelperSelect">Helper for this session</label>
          <asp:DropDownList ID="HelperSelect" runat="server" CssClass="helper-select" />
          <asp:RequiredFieldValidator runat="server" ControlToValidate="HelperSelect" CssClass="val"
            InitialValue=""
            ErrorMessage="Pick a certified or eligible helper." Display="Dynamic" />
        </div>
      </div>

      <!-- Impact summary -->
      <h3 style="margin-top:20px;">Impact summary</h3>
      <p class="sub">Participants currently enrolled or waitlisted for this session and how this time window affects their schedule.</p>

      <asp:PlaceHolder ID="NoParticipantsPH" runat="server" Visible="false">
        <div class="note">No participants are currently enrolled or waitlisted for this session.</div>
      </asp:PlaceHolder>

      <asp:Repeater ID="ImpactRepeater" runat="server">
        <HeaderTemplate>
          <table>
            <thead>
              <tr>
                <th>Participant (email)</th>
                <th>Status</th>
                <th>Schedule impact</th>
              </tr>
            </thead>
            <tbody>
        </HeaderTemplate>
        <ItemTemplate>
          <tr>
            <td><%# Eval("Email") %></td>
            <td><%# Eval("Status") %></td>
            <td>
              <asp:PlaceHolder ID="ConflictPH" runat="server" Visible='<%# (bool)Eval("HasConflict") %>'>
                <span class="helper-status status-overlap">Schedule conflict</span>
              </asp:PlaceHolder>
              <asp:PlaceHolder ID="NoConflictPH" runat="server" Visible='<%# !(bool)Eval("HasConflict") %>'>
                <span class="helper-status">No conflict</span>
              </asp:PlaceHolder>
            </td>
          </tr>
        </ItemTemplate>
        <FooterTemplate>
            </tbody>
          </table>
        </FooterTemplate>
      </asp:Repeater>

      <!-- Actions -->
      <div class="btnrow">
        <asp:Button ID="BtnSave" runat="server" Text="Save changes" CssClass="btn primary" OnClick="BtnSave_Click" />

        <asp:Button ID="BtnCancel" runat="server" Text="Cancel" CssClass="btn link"
                    CausesValidation="false"
                    OnClick="BtnCancel_Click" />

        <asp:Button ID="BtnDelete" runat="server"
                    Text="Delete session"
                    CssClass="btn link"
                    CausesValidation="false"
                    OnClick="BtnDelete_Click"
                    OnClientClick="return confirm('Are you sure you want to delete this session? This will remove the session and its enrollments.');" />
      </div>
      <asp:Label ID="EditMessage" runat="server" EnableViewState="false" />

    </div>

    <!-- Back nav -->
    <div class="btnrow">
      <a class="btn link" href="<%: ResolveUrl("~/Account/UniversityAdmin/UniversityAdminHome.aspx") %>">← Back to UA Home</a>
    </div>

  </div>
</form>
</body>
</html>
