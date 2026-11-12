<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SuperAdminAudit.aspx.cs" Inherits="CyberApp_FIA.Account.SuperAdminAudit" MaintainScrollPositionOnPostBack="true"%>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • System Audit Log</title>
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

    /* ---------- Page header / hero ---------- */
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
      max-width:600px;
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
      gap:8px;
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
      display:inline-flex;
      align-items:center;
      justify-content:center;
      gap:6px;
      max-width:260px;
      text-align:center;
    }


    /* ---------- Buttons ---------- */
    .btn{
      border-radius:999px;
      padding:8px 13px;
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

    /* ---------- Cards ---------- */
    .card{
      background:var(--card-bg);
      border:1px solid var(--card-border);
      border-radius:20px;
      box-shadow:0 14px 30px rgba(15,23,42,0.05);
      padding:20px 20px 22px;
      margin-bottom:18px;
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

    /* ---------- Filters / inputs ---------- */
    label{
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.9rem;
      display:block;
      margin-bottom:4px;
      color:#111827;
    }

    input[type=text],
    select{
      width:100%;
      padding:9px 11px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-family:inherit;
      font-size:.9rem;
    }

    input[type=text]:focus,
    select:focus{
      outline:0;
      box-shadow:0 0 0 3px var(--ring);
      border-color:var(--fia-blue);
    }

    .log-filters{
      display:flex;
      flex-wrap:wrap;
      gap:12px;
      margin:12px 0 4px 0;
    }

    .log-filters .field{
      flex:1 1 150px;
      min-width:140px;
    }

    .log-filters .field-buttons{
      display:flex;
      align-items:flex-end;
      gap:8px;
      flex-wrap:wrap;
    }

    /* ---------- Audit tables ---------- */
    .log-table{
      border-radius:16px;
      border:1px solid #e8eef7;
      background:#fff;
      max-height:320px;
      overflow-y:auto;
      overflow-x:hidden;
      margin-top:6px;
    }

    .log-table table{
      width:100%;
      border-collapse:collapse;
      font-size:.85rem;
      table-layout:fixed;
    }

    .log-table th,
    .log-table td{
      padding:8px 10px;
      border-bottom:1px solid #eef1f8;
      text-align:left;
      vertical-align:top;
    }

    .log-table thead th{
      position:sticky;
      top:0;
      z-index:1;
      background:#f3f5fb;
    }

    /* Column sizing */
    .log-table th:nth-child(1),
    .log-table td:nth-child(1){ width:110px; }  /* Timestamp */
    .log-table th:nth-child(2),
    .log-table td:nth-child(2){ width:160px; }  /* University */
    .log-table th:nth-child(3),
    .log-table td:nth-child(3){ width:140px; }   /* Role */
    .log-table th:nth-child(4),
    .log-table td:nth-child(4){ width:150px; }  /* Type */
    .log-table th:nth-child(5),
    .log-table td:nth-child(5){ width:120px; }  /* Name */
    .log-table th:nth-child(6),
    .log-table td:nth-child(6){ width:200px; }  /* Email */
    .log-table th:nth-child(7),
    .log-table td:nth-child(7){ width:auto; }   /* Details */

    .log-table td.details-cell{
      white-space:normal;
      word-break:break-word;
      overflow-wrap:anywhere;
    }

    .pill{
      display:inline-block;
      padding:2px 10px;
      border-radius:999px;
      font-size:.75rem;
      font-weight:600;
      background:rgba(42,153,219,.08);
      color:#2563eb;
    }

    /* ---------- Pagination ---------- */
    .pagination{
      display:flex;
      align-items:center;
      justify-content:flex-end;
      gap:10px;
      margin-top:10px;
      font-size:.85rem;
      color:var(--muted);
    }

    .page-link{
      background:#fff;
      border-radius:999px;
      padding:6px 12px;
      border:1px solid #e5e7eb;
      cursor:pointer;
      font-size:.85rem;
    }

    .page-link[disabled],
    .page-link[aria-disabled="true"]{
      opacity:.45;
      cursor:default;
    }

    .page-info{
      font-size:.85rem;
    }

    @media (max-width:800px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
    }
  </style>

  <script type="text/javascript">
      // Make From/To act like datetime-local inputs for audit filters
      document.addEventListener('DOMContentLoaded', function () {
          [
      '<%=TxtFromTime.ClientID%>',
      '<%=TxtToTime.ClientID%>',
      '<%=TxtCriticalFromTime.ClientID%>',
              '<%=TxtCriticalToTime.ClientID%>'
          ].forEach(function (id) {
              var el = document.getElementById(id);
              if (el) {
                  el.setAttribute('type', 'datetime-local');
              }
          });
      });

  </script>
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
          <h1 class="page-title">System Audit &amp; Activity</h1>
          <p class="page-sub">
            Review detailed audit logs across all universities so you can monitor safety, troubleshoot issues,
            and verify platform-wide actions.
          </p>
          <div class="page-admin-line">
            Signed in as <strong><asp:Literal ID="WelcomeName" runat="server" /></strong>
          </div>
        </div>

        <div class="page-header-side">
          <div class="page-chip">
            <span>Participants, Helpers, University Admins, and Super Admins across all campuses.</span>
          </div>
          <div class="header-btn-row">
            <asp:Button
              ID="BtnBackHome"
              runat="server"
              Text="Back to Super Admin Home"
              CssClass="btn btn-secondary"
              OnClick="BtnBackHome_Click"
              CausesValidation="false" />
          </div>
        </div>
      </div>

      <!-- Audit log card -->
      <div class="card">
        <div class="card-title">
          Audit log across universities
          <span class="card-pill">System-wide activity</span>
        </div>
        <p class="card-sub">
          Filter and search key actions like sign-ins, enrollments, quiz completions, and admin updates.
          Export the current filtered set for deeper review or reporting.
        </p>

        <div class="note">
          Each entry is tagged with its university, role (Participant, Helper, University Admin, Super Admin),
          log type, local timestamp, and identifiers like first name and account email.
        </div>

        <!-- Filters row -->
        <div class="log-filters">
          <div class="field">
            <label for="TxtSearch">Search</label>
            <asp:TextBox
              ID="TxtSearch"
              runat="server"
              Placeholder="Search by email, name, type, details..." />
          </div>

          <div class="field">
            <label for="DdlRoleFilter">Role</label>
            <asp:DropDownList ID="DdlRoleFilter" runat="server" />
          </div>

          <div class="field">
            <label for="DdlTypeFilter">Log type</label>
            <asp:DropDownList ID="DdlTypeFilter" runat="server" />
          </div>

          <div class="field">
            <label for="DdlUniversityFilter">University</label>
            <asp:DropDownList ID="DdlUniversityFilter" runat="server" />
          </div>

          <div class="field field-buttons">
            <asp:Button
              ID="BtnApplyFilters"
              runat="server"
              Text="Apply filters"
              CssClass="btn btn-primary"
              OnClick="BtnApplyFilters_Click" />
            <asp:Button
              ID="BtnClearFilters"
              runat="server"
              Text="Clear"
              CssClass="btn btn-secondary"
              OnClick="BtnClearFilters_Click"
              CausesValidation="false" />
            <asp:Button
              ID="BtnExportCsv"
              runat="server"
              Text="Export CSV"
              CssClass="btn btn-secondary"
              OnClick="BtnExportCsv_Click"
              CausesValidation="false" />
          </div>
        </div>

        <!-- Time range row -->
        <div class="log-filters" style="margin-top:4px;">
          <div class="field">
            <label for="TxtFromTime">From (local time)</label>
            <asp:TextBox
              ID="TxtFromTime"
              runat="server"
              Placeholder="YYYY-MM-DDThh:mm" />
          </div>

          <div class="field">
            <label for="TxtToTime">To (local time)</label>
            <asp:TextBox
              ID="TxtToTime"
              runat="server"
              Placeholder="YYYY-MM-DDThh:mm" />
          </div>
        </div>

        <!-- Empty state -->
        <asp:PlaceHolder ID="NoAuditPlaceholder" runat="server" Visible="false">
          <div class="note" style="margin-top:8px;">
            No audit entries yet. As participants, helpers, and admins interact with the
            system, their actions will appear here.
          </div>
        </asp:PlaceHolder>

        <!-- Scrollable audit table -->
        <div class="log-table" aria-label="Audit log entries">
          <table>
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>University</th>
                <th>Role</th>
                <th>Type</th>
                <th>Name</th>
                <th>Email</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              <asp:Repeater ID="AuditRepeater" runat="server">
                <ItemTemplate>
                  <tr>
                    <td>
                      <div><%# Eval("TimestampDate") %></div>
                      <div style="font-size:.8rem; color:var(--muted);">
                        <%# Eval("TimestampTime") %>
                      </div>
                    </td>
                    <td><%# Eval("University") %></td>
                    <td><span class="pill"><%# Eval("Role") %></span></td>
                    <td><%# Eval("Type") %></td>
                    <td><%# Eval("FirstName") %></td>
                    <td><%# Eval("Email") %></td>
                    <td class="details-cell"><%# Eval("Details") %></td>
                  </tr>
                </ItemTemplate>
              </asp:Repeater>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="pagination">
          <asp:LinkButton
            ID="BtnPrevPage"
            runat="server"
            Text="‹ Previous"
            CssClass="page-link"
            OnClick="BtnPrevPage_Click" />
          <asp:Label
            ID="LblPageInfo"
            runat="server"
            CssClass="page-info" />
          <asp:LinkButton
            ID="BtnNextPage"
            runat="server"
            Text="Next ›"
            CssClass="page-link"
            OnClick="BtnNextPage_Click" />
        </div>
      </div>

      <!-- Critical events card -->
      <div class="card">
        <div class="card-title">
          Critical activity patterns
          <span class="card-pill">Risk signals</span>
        </div>
        <p class="card-sub">
          Automatically surfaced patterns that may indicate risk or require follow-up, such as last-minute session
          changes, bursts of edits, repeated failed sign-ins, or late-night admin activity.
        </p>

        <div class="note">
          This view derives patterns from the same underlying audit log, detecting: session updates/deletions close to
          start time, clusters of event or microcourse edits, helper quiz and delivery bursts, repeated failed sign-ins,
          and admin sign-ins between 12:00–4:00 AM.
        </div>

        <!-- Critical filters row -->
        <div class="log-filters">
          <div class="field">
            <label for="TxtCriticalSearch">Search</label>
            <asp:TextBox
              ID="TxtCriticalSearch"
              runat="server"
              Placeholder="Search by email, name, university, details..." />
          </div>

          <div class="field">
            <label for="DdlCriticalTypeFilter">Critical type</label>
            <asp:DropDownList ID="DdlCriticalTypeFilter" runat="server" />
          </div>

          <div class="field">
            <label for="DdlCriticalUniversityFilter">University</label>
            <asp:DropDownList ID="DdlCriticalUniversityFilter" runat="server" />
          </div>

          <div class="field field-buttons">
  <asp:Button
    ID="BtnCriticalApplyFilters"
    runat="server"
    Text="Apply filters"
    CssClass="btn btn-primary"
    OnClick="BtnCriticalApplyFilters_Click" />
  <asp:Button
    ID="BtnCriticalClearFilters"
    runat="server"
    Text="Clear"
    CssClass="btn btn-secondary"
    OnClick="BtnCriticalClearFilters_Click"
    CausesValidation="false" />
  <asp:Button
    ID="BtnCriticalExportCsv"
    runat="server"
    Text="Export CSV"
    CssClass="btn btn-secondary"
    OnClick="BtnCriticalExportCsv_Click"
    CausesValidation="false" />
</div>

        </div>

          <!-- Time range row for critical events -->
<div class="log-filters" style="margin-top:4px;">
  <div class="field">
    <label for="TxtCriticalFromTime">From (local time)</label>
    <asp:TextBox
      ID="TxtCriticalFromTime"
      runat="server"
      Placeholder="YYYY-MM-DDThh:mm" />
  </div>

  <div class="field">
    <label for="TxtCriticalToTime">To (local time)</label>
    <asp:TextBox
      ID="TxtCriticalToTime"
      runat="server"
      Placeholder="YYYY-MM-DDThh:mm" />
  </div>
</div>


        <!-- Empty state for critical events -->
        <asp:PlaceHolder ID="NoCriticalPlaceholder" runat="server" Visible="false">
          <div class="note" style="margin-top:8px;">
            No critical patterns detected in this time range. Adjust the date window above or try a different university.
          </div>
        </asp:PlaceHolder>

        <!-- Scrollable critical events table -->
        <div class="log-table" aria-label="Critical audit patterns">
          <table>
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>University</th>
                <th>Role</th>
                <th>Type</th>
                <th>Name</th>
                <th>Email</th>
                <th>Details</th>
              </tr>
            </thead>
            <tbody>
              <asp:Repeater ID="CriticalRepeater" runat="server">
                <ItemTemplate>
                  <tr>
                    <td>
                      <div><%# Eval("TimestampDate") %></div>
                      <div style="font-size:.8rem; color:var(--muted);">
                        <%# Eval("TimestampTime") %>
                      </div>
                    </td>
                    <td><%# Eval("University") %></td>
                    <td><span class="pill"><%# Eval("Role") %></span></td>
                    <td><%# Eval("Type") %></td>
                    <td><%# Eval("FirstName") %></td>
                    <td><%# Eval("Email") %></td>
                    <td class="details-cell"><%# Eval("Details") %></td>
                  </tr>
                </ItemTemplate>
              </asp:Repeater>
            </tbody>
          </table>
        </div>

        <!-- Pagination for critical events -->
        <div class="pagination">
          <asp:LinkButton
            ID="BtnCriticalPrevPage"
            runat="server"
            Text="‹ Previous"
            CssClass="page-link"
            OnClick="BtnCriticalPrevPage_Click" />
          <asp:Label
            ID="LblCriticalPageInfo"
            runat="server"
            CssClass="page-info" />
          <asp:LinkButton
            ID="BtnCriticalNextPage"
            runat="server"
            Text="Next ›"
            CssClass="page-link"
            OnClick="BtnCriticalNextPage_Click" />
        </div>
      </div>

    </div>
  </form>
</body>
</html>



