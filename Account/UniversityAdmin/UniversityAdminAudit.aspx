<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="UniversityAdminAudit.aspx.cs" Inherits="CyberApp_FIA.Account.UniversityAdminAudit" MaintainScrollPositionOnPostBack="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • University Audit Log</title>
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

    *{ box-sizing:border-box }
    html,body{ height:100% }

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
      display:flex;
      gap:10px;
      align-items:flex-start;
      max-width:640px;
    }

    .badge{
      width:42px;
      height:42px;
      border-radius:12px;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      display:grid;
      place-items:center;
      color:#fff;
      font-family:Poppins,system-ui,sans-serif;
      font-weight:700;
      font-size:.9rem;
      flex-shrink:0;
    }

    .page-header-text h1{
      font-family:Poppins, system-ui, sans-serif;
      margin:0;
      font-size:1.5rem;
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

    .page-chip{
      padding:8px 14px;
      border-radius:999px;
      border:1px solid rgba(42,153,219,0.55);
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.18), transparent 60%),
        radial-gradient(circle at 100% 100%, rgba(69,195,179,0.22), transparent 55%),
        #f0f9ff;
      font-size:.8rem;
      color:#0f172a;
      max-width:260px;
      text-align:center;
    }

    .header-btn-row{
      display:flex;
      gap:8px;
      flex-wrap:wrap;
      justify-content:flex-end;
    }

    @media (max-width:800px){
      .page-header{
        flex-direction:column;
        align-items:flex-start;
      }
      .page-header-main{
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
      .page-chip{
        text-align:left;
      }
    }

    /* ---------- Cards ---------- */
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
      margin:0 0 8px 0;
      font-size:1.15rem;
    }
    .sub{
      color:var(--muted);
      margin:0 0 12px 0;
      font-size:.9rem;
    }

    label{
      font-weight:600;
      font-family:Poppins;
      font-size:.9rem;
      display:block;
      margin-bottom:4px;
    }
    input[type=text], select{
      width:100%;
      padding:10px 12px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-family:inherit;
      font-size:.9rem;
    }
    input:focus, select:focus{
      outline:0;
      box-shadow:0 0 0 5px var(--ring);
      border-color:var(--fia-blue);
    }

    .btn{
      border-radius:12px;
      padding:10px 16px;
      font-weight:700;
      font-family:Poppins;
      cursor:pointer;
      font-size:.85rem;
      text-decoration:none;
      display:inline-flex;
      align-items:center;
      justify-content:center;
      white-space:nowrap;
      border:0;
    }
    .primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
      box-shadow:0 10px 26px rgba(37,99,235,0.18);
    }
    .link{
      background:#fff;
      border:2px solid var(--fia-blue);
      color:var(--fia-blue);
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    .note{
      background:#f6f7fb;
      border:1px solid #e8eef7;
      border-radius:12px;
      padding:12px;
      color:var(--muted);
      font-size:.9rem;
    }

    .helper-list{
      display:flex;
      flex-direction:column;
      gap:8px;
      margin-top:8px;
    }
    .helper-row{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:10px;
      padding:10px 12px;
      border-radius:12px;
      border:1px solid #e8eef7;
      background:#f9fafb;
    }
    .helper-main{
      display:flex;
      flex-direction:column;
    }
        /* Sprint 4 (#15): stack the row and let the long button wrap on phones */
    @media (max-width:640px){
      .helper-row{ flex-wrap:wrap; }
      .helper-main{ min-width:0; }
      .helper-row .btn{ white-space:normal; text-align:center; }
    }
    .helper-name{
      font-weight:600;
      font-family:Poppins;
      font-size:.95rem;
    }
    .helper-email{
      font-size:.85rem;
      color:var(--muted);
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

    .log-filters{
      display:flex;
      flex-wrap:wrap;
      gap:12px;
      margin:12px 0;
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

    /* --- Audit table --- */
    .log-table{
      border-radius:16px;
      border:1px solid #e8eef7;
      background:#fff;
      max-height:320px;
      overflow-y:auto;      /* vertical scroll only */
      overflow-x:hidden;    /* no horizontal scroll */
    }

    .log-table table{
      width:100%;
      border-collapse:collapse;
      font-size:.85rem;
      table-layout:fixed;   /* consistent column widths */
    }

    .log-table th,
    .log-table td{
      padding:8px 10px;
      border-bottom:1px solid #eef1f8;
      text-align:left;
      vertical-align:top;
    }

    /* sticky header row */
    .log-table thead th{
      position:sticky;
      top:0;
      z-index:1;
      background:#f3f5fb;
    }

    /* Column sizing: give Details more room */
    .log-table th:nth-child(1),
    .log-table td:nth-child(1){ width:110px; }  /* Timestamp */
    .log-table th:nth-child(2),
    .log-table td:nth-child(2){ width:140px; }   /* Role */
    .log-table th:nth-child(3),
    .log-table td:nth-child(3){ width:130px; }  /* Type */
    .log-table th:nth-child(4),
    .log-table td:nth-child(4){ width:120px; }  /* Name */
    .log-table th:nth-child(5),
    .log-table td:nth-child(5){ width:200px; }  /* Email */
    .log-table th:nth-child(6),
    .log-table td:nth-child(6){ width:auto; }   /* Details (flex) */

    /* Wrap long details cleanly */
    .log-table td.details-cell{
      white-space:normal;
      word-break:break-word;
      overflow-wrap:anywhere;
    }

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
  </style>

  <script type="text/javascript">
      // Make From/To act like datetime-local inputs for audit filters
      document.addEventListener('DOMContentLoaded', function () {
          ['<%=TxtFromTime.ClientID%>', '<%=TxtToTime.ClientID%>'].forEach(function (id) {
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
          <div class="badge">FIA</div>
          <div class="page-header-text">
            <h1>University Audit &amp; Activity</h1>
            <p class="page-sub">
              Review important actions under your university: sign-ins, enrollments, quiz completions,
              and admin updates so you can monitor safety and verify helper progress.
            </p>
            <div class="page-admin-line">
              Signed in as <strong><asp:Literal ID="WelcomeName" runat="server" /></strong>
            </div>
          </div>
        </div>

        <div class="page-header-side">
          <div class="page-chip">
            Shows audit entries only for your university, across Participants, Helpers, and Admins.
          </div>
          <div class="header-btn-row">
            <asp:Button
              ID="BtnBackHome"
              runat="server"
              Text="Back to admin home"
              CssClass="btn link"
              OnClick="BtnBackHome_Click"
              CausesValidation="false" />
          </div>
        </div>
      </div>

      <!-- Helper certification / actions -->
      <div class="card">
        <h2>Helper certification &amp; actions</h2>
        <p class="sub">
          Use this list to jump into helper activity when you’re verifying progress for certification
          or following up on something unusual in the audit log.
        </p>

        <div class="note">
          This section lists helpers at your university. For each helper, you’ll eventually be able to open
          a detailed timeline of their actions (sessions taught, 1:1 help, quiz completions) and approve
          or question their certification status. For now, the button is just a visual placeholder.
        </div>

        <asp:PlaceHolder ID="NoHelpersPlaceholder" runat="server" Visible="false">
          <div class="note" style="margin-top:10px;">
            No helpers are currently associated with your university yet. Once helpers sign up and are linked
            to this campus, they will appear here.
          </div>
        </asp:PlaceHolder>

        <asp:Repeater ID="HelpersRepeater" runat="server" OnItemCommand="HelpersRepeater_ItemCommand">
          <HeaderTemplate>
            <div class="helper-list">
          </HeaderTemplate>

          <ItemTemplate>
            <div class="helper-row">
              <div class="helper-main">
                <span class="helper-name"><%# Eval("DisplayName") %></span>
                <span class="helper-email"><%# Eval("Email") %></span>
              </div>

              <asp:Button
                ID="BtnViewHelper"
                runat="server"
                Text="View/Approve Helper Actions"
                CssClass="btn link"
                CommandName="viewHelper"
                CommandArgument='<%# Eval("Email") %>' />
            </div>
          </ItemTemplate>

          <FooterTemplate>
            </div>
          </FooterTemplate>
        </asp:Repeater>

        <asp:HiddenField ID="UniversityValue" runat="server" />
      </div>

      <!-- Audit log -->
      <div class="card">
        <h2>Audit log for your university</h2>
        <p class="sub">
          Search and filter important actions happening under your university, including sign-ins,
          enrollments, quiz completions, and admin actions. Use this to monitor safety, troubleshoot issues,
          and verify helper progress.
        </p>

        <div class="note">
          Each entry is tagged behind the scenes with the originating university, role (Participant, Helper,
          University Admin, Super Admin), log type (Sign In, Participant Enroll, Helper Quiz Completion, etc.),
          plus timestamp and key identifiers like first name and account email.
        </div>

        <!-- Filters row: search/role/type + actions -->
        <div class="log-filters">
          <div class="field">
            <label for="TxtSearch">Search</label>
            <asp:TextBox
              ID="TxtSearch"
              runat="server"
              Placeholder="Search by email, name, type, or details..." />
          </div>

          <div class="field">
            <label for="DdlRoleFilter">Role</label>
            <asp:DropDownList ID="DdlRoleFilter" runat="server" />
          </div>

          <div class="field">
            <label for="DdlTypeFilter">Log type</label>
            <asp:DropDownList ID="DdlTypeFilter" runat="server" />
          </div>

          <div class="field field-buttons">
            <asp:Button
              ID="BtnApplyFilters"
              runat="server"
              Text="Apply filters"
              CssClass="btn primary"
              OnClick="BtnApplyFilters_Click" />
            <asp:Button
              ID="BtnClearFilters"
              runat="server"
              Text="Clear"
              CssClass="btn link"
              OnClick="BtnClearFilters_Click"
              CausesValidation="false" />
            <asp:Button
              ID="BtnExportCsv"
              runat="server"
              Text="Export CSV"
              CssClass="btn link"
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
            No audit entries yet for this university. As participants, helpers, and admins interact with the
            system, their actions will appear here.
          </div>
        </asp:PlaceHolder>

        <!-- Scrollable audit table -->
        <div class="log-table" aria-label="Audit log entries">
          <table>
            <thead>
              <tr>
                <th>Timestamp</th>
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

    </div>
  </form>
</body>
</html>
