<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SuperAdminMicrocourses.aspx.cs" Inherits="CyberApp_FIA.Account.SuperAdminMicrocourses" MaintainScrollPositionOnPostBack="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Manage Microcourses</title>
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

    *{ box-sizing:border-box; }

    html,body{ height:100%; }

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

    /* UPDATED: centered text + FIA gradient like Super Admin Home */
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
      text-align:center;
    }

    .header-actions{
      display:flex;
      gap:8px;
      flex-wrap:wrap;
      justify-content:flex-end;
    }

    @media (max-width:900px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
      .header-actions{
        justify-content:flex-start;
      }
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

    .primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
      box-shadow:0 10px 26px rgba(37,99,235,0.18);
    }

    .link{
      background:#ffffff;
      border:1px solid rgba(148,163,184,0.7);
      color:#1f2933;
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    .danger{
      background:#ffffff;
      border:2px solid #b91c1c;
      color:#b91c1c;
      box-shadow:0 8px 18px rgba(185,28,28,0.10);
    }

    .btnrow{
      display:flex;
      gap:10px;
      flex-wrap:wrap;
      margin-top:14px;
    }

    /* ---------- Cards & text ---------- */
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

    .sub{
      color:var(--muted);
      margin:0 0 12px 0;
      font-size:.9rem;
    }

    .note{
      background:#f9fafb;
      border:1px solid #e5e7eb;
      border-radius:12px;
      padding:12px;
      color:var(--muted);
      font-size:.9rem;
    }

    .val{
      color:#c21d1d;
      font-size:.9rem;
      margin-top:4px;
      display:block;
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

    .grid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:14px;
    }

    @media (max-width:800px){
      .grid{
        grid-template-columns:1fr;
      }
    }

    .fieldset{
      border:1px dashed #e5e7eb;
      border-radius:14px;
      padding:12px;
    }

    /* ---------- Microcourse table ---------- */
    .mc-table{
      width:100%;
      border-collapse:collapse;
      font-size:.85rem;
    }

    .mc-table th,
    .mc-table td{
      padding:8px 10px;
      border-bottom:1px solid #eef1f8;
      text-align:left;
      vertical-align:top;
    }

    .mc-table thead th{
      background:#f3f5fb;
      font-family:Poppins, system-ui, sans-serif;
      font-weight:600;
    }

    .status-pill{
      display:inline-block;
      padding:2px 10px;
      border-radius:999px;
      font-size:.75rem;
      font-weight:600;
      background:rgba(42,153,219,.08);
      color:#2563eb;
    }

    /* Stacked one-per-line CheckBoxList for prerequisites */
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
          <h1 class="page-title">Manage microcourses</h1>
          <p class="page-sub">
            Review and update every microcourse in the FIA catalog, including status, tags, certification rules,
            and prerequisites across universities.
          </p>
          <div class="page-admin-line">
            Signed in as <strong><asp:Literal ID="WelcomeName" runat="server" /></strong>
          </div>
        </div>

        <div class="page-header-side">
          <div class="page-chip">
            Use this view to keep the microcourse catalog clean, up to date, and ready for University Admins.
          </div>
          <div class="header-actions">
            <asp:Button
              ID="BtnBackHome"
              runat="server"
              Text="Back to Super Admin home"
              CssClass="btn link"
              OnClick="BtnBackHome_Click"
              CausesValidation="false" />
            <!-- Sign out button removed as requested -->
          </div>
        </div>
      </div>

      <!-- Existing microcourses list -->
      <div class="card">
        <div class="card-title">
          Existing microcourses
          <span class="card-pill">Catalog</span>
        </div>
        <p class="sub">
          Select a microcourse to view or edit its details. Changes are saved back into
          <code>microcourses.xml</code> and reflected across all universities.
        </p>

        <asp:PlaceHolder ID="NoCoursesPlaceholder" runat="server" Visible="false">
          <div class="note" style="margin-top:8px;">
            No microcourses have been created yet. Use the Super Admin home page to add your first microcourse.
          </div>
        </asp:PlaceHolder>

        <asp:Repeater ID="CoursesRepeater" runat="server" OnItemCommand="CoursesRepeater_ItemCommand">
          <HeaderTemplate>
            <table class="mc-table">
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Status</th>
                  <th>Created</th>
                  <th>Created by</th>
                  <th style="width:120px;">Actions</th>
                </tr>
              </thead>
              <tbody>
          </HeaderTemplate>

          <ItemTemplate>
            <tr>
              <td><%# Eval("Title") %></td>
              <td><span class="status-pill"><%# Eval("Status") %></span></td>
              <td><%# Eval("CreatedAtDisplay") %></td>
              <td><%# Eval("CreatedBy") %></td>
              <td>
                <asp:Button
                  ID="BtnEditCourse"
                  runat="server"
                  Text="Edit"
                  CssClass="btn link"
                  CommandName="editCourse"
                  CommandArgument='<%# Eval("Id") %>'
                  CausesValidation="false" />
              </td>
            </tr>
          </ItemTemplate>

          <FooterTemplate>
              </tbody>
            </table>
          </FooterTemplate>
        </asp:Repeater>
      </div>

      <!-- Edit microcourse -->
      <div class="card">
        <div class="card-title">
          Edit microcourse
          <span class="card-pill">Details</span>
        </div>
        <p class="sub">
          Select a microcourse above to load its details here. You can update fields and save, or delete the microcourse
          entirely if it should no longer appear in the catalog.
        </p>

        <asp:HiddenField ID="CurrentCourseId" runat="server" />

        <asp:PlaceHolder ID="NoCourseSelectedPlaceholder" runat="server" Visible="true">
          <div class="note">
            No microcourse selected yet. Click <strong>Edit</strong> next to a microcourse in the list above to load it here.
          </div>
        </asp:PlaceHolder>

        <asp:PlaceHolder ID="EditorPlaceholder" runat="server" Visible="false">
          <div class="note" style="margin-bottom:12px;">
            Currently editing:
            <strong><asp:Literal ID="CurrentCourseTitle" runat="server" /></strong>
          </div>

          <div class="grid">
            <!-- Title -->
            <div>
              <label for="TxtTitle">Title</label>
              <asp:TextBox ID="TxtTitle" runat="server" />
              <asp:RequiredFieldValidator
                runat="server"
                ControlToValidate="TxtTitle"
                CssClass="val"
                ErrorMessage="Title is required."
                Display="Dynamic" />
            </div>

            <!-- Duration -->
            <div>
              <label for="TxtDuration">Duration</label>
              <asp:TextBox ID="TxtDuration" runat="server" />
              <asp:RequiredFieldValidator
                runat="server"
                ControlToValidate="TxtDuration"
                CssClass="val"
                ErrorMessage="Duration is required."
                Display="Dynamic" />
            </div>

            <!-- Summary -->
            <div style="grid-column:1/-1">
              <label for="TxtSummary">Summary</label>
              <asp:TextBox ID="TxtSummary" runat="server" TextMode="MultiLine" />
              <asp:RequiredFieldValidator
                runat="server"
                ControlToValidate="TxtSummary"
                CssClass="val"
                ErrorMessage="Summary is required."
                Display="Dynamic" />
            </div>

            <!-- External link -->
            <div style="grid-column:1/-1">
              <label for="TxtExternalLink">External link (slides / video / PDF)</label>
              <asp:TextBox ID="TxtExternalLink" runat="server" TextMode="Url" />
            </div>

            <!-- Tags -->
            <div>
              <label for="TxtTags">Tags (comma-separated)</label>
              <asp:TextBox ID="TxtTags" runat="server" />
            </div>

            <!-- Status -->
            <div>
              <label for="DdlStatus">Status</label>
              <asp:DropDownList ID="DdlStatus" runat="server">
                <asp:ListItem Text="Draft" Value="Draft" />
                <asp:ListItem Text="Published" Value="Published" />
                <asp:ListItem Text="Deprecated" Value="Deprecated" />
              </asp:DropDownList>
            </div>

            <!-- Certification rules -->
            <div style="grid-column:1/-1">
              <label>Certification rules required (multi-select)</label>
              <div class="fieldset">
                <asp:CheckBoxList
                  ID="RulesList"
                  runat="server"
                  RepeatLayout="Flow" />
                <div class="note" style="margin-top:8px;">
                  Selected rules become prerequisites Helpers must satisfy to complete this microcourse.
                </div>
              </div>
            </div>

            <!-- Prerequisites -->
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
                  Choose any microcourses learners must complete before starting this one.
                </div>
              </div>
            </div>
          </div>

          <div class="btnrow">
            <asp:Button
              ID="BtnSaveChanges"
              runat="server"
              Text="Save changes"
              CssClass="btn primary"
              OnClick="BtnSaveChanges_Click" />
            <asp:Button
              ID="BtnClearEditor"
              runat="server"
              Text="Clear selection"
              CssClass="btn link"
              OnClick="BtnClearEditor_Click"
              CausesValidation="false" />
            <asp:Button
              ID="BtnDeleteCourse"
              runat="server"
              Text="Delete microcourse"
              CssClass="btn danger"
              OnClick="BtnDeleteCourse_Click"
              CausesValidation="false"
              OnClientClick="return confirm('Are you sure you want to delete this microcourse? This cannot be undone.');" />
          </div>

          <asp:Label ID="FormMessage" runat="server" EnableViewState="false" />
        </asp:PlaceHolder>
      </div>

    </div>
  </form>
</body>
</html>
