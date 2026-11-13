<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CreateUniversityAdmin.aspx.cs" Inherits="CyberApp_FIA.Account.CreateUniversityAdmin" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Create University Admin</title>
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
      max-width:960px;
      margin:0 auto;
      padding:24px 16px 40px;
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
      max-width:560px;
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

    .nav-chip{
      padding:5px 10px;
      border-radius:999px;
      border:1px solid rgba(148,163,184,0.6);
      background:#ffffff;
      font-size:.8rem;
      color:#4b5563;
      display:inline-flex;
      align-items:center;
      gap:6px;
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

    .btn-danger{
      background:#ffffff;
      border:1px solid #fecaca;
      color:#b91c1c;
      box-shadow:0 8px 18px rgba(248,113,113,0.18);
    }

    .header-btn-row{
      display:flex;
      gap:8px;
      flex-wrap:wrap;
    }

    /* ---------- Card ---------- */
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

    /* ---------- Form / fields ---------- */
    label{
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      font-size:.9rem;
      display:block;
      margin-bottom:4px;
      color:#111827;
    }

    input[type=text],
    input[type=password]{
      width:100%;
      padding:9px 11px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-family:inherit;
      font-size:.9rem;
    }

    input[type=text]:focus,
    input[type=password]:focus{
      outline:0;
      box-shadow:0 0 0 3px var(--ring);
      border-color:var(--fia-blue);
    }

    .grid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:14px;
    }

    .grid-full{
      grid-column:1/-1;
    }

    .btnrow{
      display:flex;
      gap:10px;
      flex-wrap:wrap;
      margin-top:14px;
    }

    /* ---------- Validation / messages ---------- */
    .val{
      color:#b91c1c;
      font-size:.82rem;
      margin-top:4px;
      display:block;
    }

    .msg{
      display:block;
      margin-top:10px;
      font-size:.9rem;
      color:var(--muted);
    }

    @media (max-width:800px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }
      .page-header-side{
        align-items:flex-start;
      }
      .grid{
        grid-template-columns:1fr;
      }
      .grid-full{
        grid-column:1;
      }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <!-- Page header / hero -->
      <div class="page-header">
        <div class="page-header-main">
          <div class="page-eyebrow">
            <span class="page-eyebrow-pill">FIA</span>
            <span>Super Admin workspace</span>
          </div>
          <h1 class="page-title">Create a University Admin</h1>
          <p class="page-sub">
            Add a new University Admin account linked to a campus so they can manage events, helpers,
            and participants for their university.
          </p>
          <div class="page-admin-line">
            Signed in as <strong><asp:Literal ID="WelcomeName" runat="server" /></strong>
          </div>
        </div>

        <div class="page-header-side">
          <div class="nav-chip">
            <span>Tip:</span>
            <span>Use a unique email for each University Admin.</span>
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

      <!-- Main card -->
      <div class="card">
        <div class="card-title">
          Add a University Admin account
          <span class="card-pill">Account creation</span>
        </div>
        <p class="card-sub">
          This creates a new login with role <strong>UniversityAdmin</strong> and associates it with a university.
          You can reuse the same university name across multiple admins if needed.
        </p>

        <div class="note">
          Universities do not need to appear in events yet. This step just establishes the admin’s
          account and university label for future scheduling and reporting.
        </div>

        <asp:ValidationSummary
          ID="ValidationSummary1"
          runat="server"
          CssClass="val"
          HeaderText="Please fix the following:"
          DisplayMode="BulletList" />

        <div class="grid" style="margin-top:12px;">
          <div>
            <label for="FirstName">First name</label>
            <asp:TextBox ID="FirstName" runat="server" />
            <asp:RequiredFieldValidator
              ID="ReqFirstName"
              runat="server"
              ControlToValidate="FirstName"
              CssClass="val"
              ErrorMessage="First name is required."
              Display="Dynamic" />
          </div>

          <div>
            <label for="LastName">Last name</label>
            <asp:TextBox ID="LastName" runat="server" />
            <asp:RequiredFieldValidator
              ID="ReqLastName"
              runat="server"
              ControlToValidate="LastName"
              CssClass="val"
              ErrorMessage="Last name is required."
              Display="Dynamic" />
          </div>

          <div class="grid-full">
            <label for="Email">Email</label>
            <asp:TextBox ID="Email" runat="server" />
            <asp:RequiredFieldValidator
              ID="ReqEmail"
              runat="server"
              ControlToValidate="Email"
              CssClass="val"
              ErrorMessage="Email is required."
              Display="Dynamic" />
            <asp:RegularExpressionValidator
              ID="ValEmailFormat"
              runat="server"
              ControlToValidate="Email"
              CssClass="val"
              ErrorMessage="Please enter a valid email address."
              ValidationExpression="^\s*[^@\s]+@[^@\s]+\.[^@\s]+\s*$"
              Display="Dynamic" />
          </div>

          <div class="grid-full">
            <label for="University">University</label>
            <asp:TextBox ID="University" runat="server" />
            <asp:RequiredFieldValidator
              ID="ReqUniversity"
              runat="server"
              ControlToValidate="University"
              CssClass="val"
              ErrorMessage="University is required."
              Display="Dynamic" />
          </div>

          <div class="grid-full">
            <label for="Password">Password</label>
            <asp:TextBox ID="Password" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator
              ID="ReqPassword"
              runat="server"
              ControlToValidate="Password"
              CssClass="val"
              ErrorMessage="Password is required."
              Display="Dynamic" />
          </div>
        </div>

        <div class="btnrow">
          <asp:Button
            ID="BtnCreate"
            runat="server"
            Text="Create account"
            CssClass="btn btn-primary"
            OnClick="BtnCreate_Click" />
        </div>

        <asp:Label ID="FormMessage" runat="server" CssClass="msg" EnableViewState="false" />
      </div>

    </div>
  </form>
</body>
</html>

