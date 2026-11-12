<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="UniversityAdminAddHelper.aspx.cs"
    Inherits="CyberApp_FIA.Account.UniversityAdminAddHelper" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Add Helper</title>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style type="text/css">
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

    *{box-sizing:border-box;}

    body{
      margin:0;
      font-family:Lato, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.10), transparent 55%),
        radial-gradient(circle at 100% 100%, rgba(42,153,219,0.10), transparent 55%),
        var(--bg);
      color:var(--ink);
    }

    .fia-shell{
      max-width:760px;
      margin:0 auto;
      padding:24px 16px 48px;
    }

    /* Header */
    .fia-header{
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

    .fia-header-main{
      display:flex;
      gap:10px;
      align-items:flex-start;
    }

    .fia-badge-logo{
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

    .fia-header-text h1{
      font-family:Poppins, system-ui, sans-serif;
      margin:0;
      font-size:1.4rem;
    }

    .fia-subtitle{
      font-size:.9rem;
      color:var(--muted);
      margin-top:4px;
    }

    .fia-subtitle-pill{
      display:inline-block;
      padding:4px 10px;
      border-radius:999px;
      background:#f9fafb;
      border:1px solid #e5e7eb;
      font-size:.8rem;
      margin-top:6px;
    }

    .fia-header-side{
      display:flex;
      flex-direction:column;
      gap:10px;
      align-items:flex-end;
    }

    .fia-chip{
      padding:6px 14px;
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

    @media(max-width:720px){
      .fia-header{
        flex-direction:column;
        align-items:flex-start;
      }
      .fia-header-side{
        align-items:flex-start;
      }
      .fia-chip{
        text-align:left;
      }
    }

        /* Helpers table – add colored header background row */
    .helper-table{
      width:100%;
      border-collapse:collapse;
      margin-top:16px;
      font-size:.9rem;
      background:#ffffff;
      border-radius:16px;
      overflow:hidden;
      border:1px solid var(--card-border);
    }

    .helper-table th,
    .helper-table td{
      padding:8px 10px;
      text-align:left;
    }

    .helper-table thead th{
      background:linear-gradient(120deg,
        rgba(42,153,219,0.12),
        rgba(240,106,169,0.12));
      color:#111827;
      border-bottom:1px solid #e5e7eb;
      font-weight:600;
    }


    /* Card */
    .fia-card{
      background:var(--card-bg);
      border-radius:20px;
      padding:18px 18px 20px;
      box-shadow:0 12px 30px rgba(15,23,42,0.06);
      border:1px solid var(--card-border);
    }

    .fia-card-title{
      font-size:1.1rem;
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      color:#1e2640;
      margin-bottom:4px;
    }

    .fia-card-body{
      font-size:.9rem;
      color:var(--muted);
      margin-bottom:12px;
    }

    /* Form */
    .fia-form-row{margin-bottom:10px;}

    .fia-label{
      display:block;
      font-size:.9rem;
      font-weight:600;
      color:#4b5563;
      font-family:Poppins, system-ui, sans-serif;
      margin-bottom:3px;
    }

    .fia-input{
      width:100%;
      box-sizing:border-box;
      border-radius:12px;
      border:1px solid #d0d5e6;
      padding:9px 11px;
      font-size:.9rem;
      font-family:inherit;
    }

    .fia-input:focus{
      outline:0;
      border-color:var(--fia-blue);
      box-shadow:0 0 0 3px var(--ring);
    }

    .fia-btn-row{
      margin-top:14px;
      display:flex;
      gap:8px;
      align-items:center;
      flex-wrap:wrap;
    }

    .fia-btn{
      border:none;
      border-radius:999px;
      padding:9px 16px;
      font-size:.88rem;
      cursor:pointer;
      font-weight:700;
      font-family:Poppins, system-ui, sans-serif;
      display:inline-flex;
      align-items:center;
      justify-content:center;
      white-space:nowrap;
    }

    .fia-btn-primary{
      background:linear-gradient(135deg,#2a99db,#45c3b3);
      color:#fff;
      box-shadow:0 10px 26px rgba(37,99,235,0.18);
    }

    .fia-btn-secondary{
      border:1px solid #d0d5e6;
      background:#ffffff;
      color:#1e2640;
      box-shadow:0 8px 18px rgba(15,23,42,0.06);
    }

    .fia-message{
      font-size:.85rem;
      margin-top:8px;
      color:var(--muted);
    }

    .fia-validation{
      color:#b91c1c;
      font-size:.8rem;
      margin-top:4px;
      display:block;
    }

    .fia-validation-summary{
      margin-bottom:8px;
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <asp:HiddenField ID="UniversityValue" runat="server" />

    <div class="fia-shell">
      <!-- Header -->
      <div class="fia-header">
        <div class="fia-header-main">
          <div class="fia-badge-logo">FIA</div>
          <div class="fia-header-text">
            <h1>Add Helper account</h1>
            <div class="fia-subtitle">
              Create a new Helper linked to your university. The account will start with privacy turned on.
            </div>
            <div class="fia-subtitle-pill">
              University: <asp:Literal ID="UniversityDisplay" runat="server" />
            </div>
          </div>
        </div>

        <div class="fia-header-side">
          <div class="fia-chip">
            Helpers can be added in advance so they’re ready to be assigned to sessions later.
          </div>
          <asp:Button ID="BtnBack" runat="server"
            Text="Back to Admin Home"
            CssClass="fia-btn fia-btn-secondary"
            OnClick="BtnBack_Click"
            CausesValidation="false" />
        </div>
      </div>

      <!-- Card -->
      <div class="fia-card">
        <div class="fia-card-title">Helper details</div>
        <div class="fia-card-body">
          Enter the helper’s information. The account will be created with the
          <strong>Helper</strong> role and tied to this university.
        </div>

        <asp:ValidationSummary ID="ValidationSummary1" runat="server"
          CssClass="fia-validation fia-validation-summary"
          HeaderText="Please fix the following:" />

        <div class="fia-form-row">
          <label class="fia-label" for="NewHelperEmail">Helper email</label>
          <asp:TextBox ID="NewHelperEmail" runat="server"
                       CssClass="fia-input"
                       Placeholder="helper@example.edu"></asp:TextBox>
          <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
            ControlToValidate="NewHelperEmail"
            ErrorMessage="Email is required."
            Display="Dynamic"
            CssClass="fia-validation" />
        </div>

        <div class="fia-form-row">
          <label class="fia-label" for="NewHelperFirstName">First name</label>
          <asp:TextBox ID="NewHelperFirstName" runat="server"
                       CssClass="fia-input"
                       Placeholder="First name"></asp:TextBox>
          <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server"
            ControlToValidate="NewHelperFirstName"
            ErrorMessage="First name is required."
            Display="Dynamic"
            CssClass="fia-validation" />
        </div>

        <div class="fia-form-row">
          <label class="fia-label" for="NewHelperLastName">Last name</label>
          <asp:TextBox ID="NewHelperLastName" runat="server"
                       CssClass="fia-input"
                       Placeholder="Last name"></asp:TextBox>
          <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server"
            ControlToValidate="NewHelperLastName"
            ErrorMessage="Last name is required."
            Display="Dynamic"
            CssClass="fia-validation" />
        </div>

        <div class="fia-form-row">
          <label class="fia-label" for="NewHelperPassword">Temporary password</label>
          <asp:TextBox ID="NewHelperPassword" runat="server"
                       CssClass="fia-input"
                       TextMode="Password"
                       Placeholder="Temporary password for first sign-in"></asp:TextBox>
          <asp:RequiredFieldValidator ID="RequiredFieldValidator4" runat="server"
            ControlToValidate="NewHelperPassword"
            ErrorMessage="Password is required."
            Display="Dynamic"
            CssClass="fia-validation" />
        </div>

        <div class="fia-btn-row">
          <asp:Button ID="BtnCreateHelper" runat="server"
            Text="Create helper"
            CssClass="fia-btn fia-btn-primary"
            OnClick="BtnCreateHelper_Click" />
        </div>

        <div class="fia-message">
          <asp:Literal ID="HelperFormMessage" runat="server" />
        </div>
      </div>
    </div>
  </form>
</body>
</html>


