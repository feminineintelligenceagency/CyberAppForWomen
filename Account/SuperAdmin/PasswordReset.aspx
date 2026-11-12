<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PasswordReset.aspx.cs" Inherits="CyberApp_FIA.Account.SuperAdmin.PasswordReset" MaintainScrollPositionOnPostBack="true" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Password Reset</title>
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
      max-width:900px;
      margin:0 auto;
    }

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
      padding:12px 14px;
      color:var(--muted);
      font-size:.92rem;
      margin-bottom:16px;
      line-height:1.45;
    }

    .steps{
      margin:8px 0 0 0;
      padding-left:20px;
    }

    .steps li{
      margin-bottom:4px;
    }

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
      padding:10px 12px;
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

    .form-grid{
      display:grid;
      grid-template-columns:1fr 1fr;
      gap:14px;
    }

    .full{
      grid-column:1/-1;
    }

    @media (max-width:800px){
      .page-header{
        flex-direction:column-reverse;
        align-items:flex-start;
      }

      .page-header-side{
        align-items:flex-start;
      }

      .form-grid{
        grid-template-columns:1fr;
      }
    }

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

    .message{
      margin-top:12px;
      font-size:.92rem;
      font-weight:600;
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <div class="page-header">
        <div class="page-header-main">
          <div class="page-eyebrow">
            <span class="page-eyebrow-pill">FIA</span>
            <span>Super Admin security tool</span>
          </div>
          <h1 class="page-title">Password Reset</h1>
          <p class="page-sub">
            Reset an existing FIA account password by entering the user’s email address and the new password.
          </p>
          <div class="page-admin-line">
            Signed in as <strong><asp:Literal ID="WelcomeName" runat="server" /></strong>
          </div>
        </div>

        <div class="page-header-side">
          <div class="page-chip">
            Updates passwordHash and passwordSalt in users.xml.
          </div>
          <div>
            <a class="btn btn-secondary" href="<%: ResolveUrl("~/Account/SuperAdmin/SuperAdminHome.aspx") %>">
              Back to Super Admin Home
            </a>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-title">
          Reset account password
          <span class="card-pill">Access</span>
        </div>

        <p class="card-sub">
          Use this page when a participant, helper, University Admin, or Super Admin needs a password replaced.
        </p>

        <div class="note">
          <strong>What to do:</strong>
          <ol class="steps">
            <li>Enter the user’s email address exactly as it appears on their FIA account.</li>
            <li>Enter the new password you want the user to use.</li>
            <li>Confirm the new password and click <strong>Reset password</strong>.</li>
          </ol>
        </div>

        <div class="form-grid">
          <div class="full">
            <label for="EmailAddress">Email Address</label>
            <asp:TextBox ID="EmailAddress" runat="server" />
            <asp:RequiredFieldValidator
              runat="server"
              ControlToValidate="EmailAddress"
              CssClass="val"
              ErrorMessage="Email address is required."
              Display="Dynamic"
              ValidationGroup="ResetPassword" />
          </div>

          <div>
  <label for="NewPassword">New Password</label>
  <asp:TextBox ID="NewPassword" runat="server" TextMode="Password" />
  <asp:RequiredFieldValidator
    runat="server"
    ControlToValidate="NewPassword"
    CssClass="val"
    ErrorMessage="New password is required."
    Display="Dynamic"
    ValidationGroup="ResetPassword" />
</div>

          <div>
            <label for="ConfirmPassword">Confirm New Password</label>
            <asp:TextBox ID="ConfirmPassword" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator
              runat="server"
              ControlToValidate="ConfirmPassword"
              CssClass="val"
              ErrorMessage="Please confirm the new password."
              Display="Dynamic"
              ValidationGroup="ResetPassword" />
            <asp:CompareValidator
              runat="server"
              ControlToValidate="ConfirmPassword"
              ControlToCompare="NewPassword"
              CssClass="val"
              ErrorMessage="Passwords must match."
              Display="Dynamic"
              ValidationGroup="ResetPassword" />
          </div>
        </div>

        <div class="btnrow">
          <asp:Button
            ID="BtnResetPassword"
            runat="server"
            Text="Reset password"
            CssClass="btn btn-primary"
            OnClick="BtnResetPassword_Click"
            ValidationGroup="ResetPassword" />

          <asp:Button
            ID="BtnClear"
            runat="server"
            Text="Clear"
            CssClass="btn btn-secondary"
            OnClick="BtnClear_Click"
            CausesValidation="false" />
        </div>

        <div class="message">
          <asp:Label ID="ResetMessage" runat="server" EnableViewState="false" />
        </div>
      </div>

    </div>
  </form>
</body>
</html>