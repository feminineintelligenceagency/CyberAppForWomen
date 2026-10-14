<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ResetPassword.aspx.cs" Inherits="CyberApp_FIA.Account.ResetPassword" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Reset Password</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <!-- Don't send the reset link (with its token) to other sites -->
  <meta name="referrer" content="no-referrer" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --muted:#6b7280;
      --ring:rgba(42,153,219,.25);
    }
    *{ box-sizing:border-box }
    html,body{ height:100% }
    body{ margin:0; font-family:Lato,Arial,sans-serif; background:linear-gradient(135deg,#fff,#f9fbff); }
    .wrap{ min-height:100vh; display:grid; place-items:center; padding:24px; }
    .card{ width:min(560px,100%); background:#fff; border:1px solid #e8eef7; border-radius:20px;
           box-shadow:0 12px 36px rgba(42,153,219,.08); padding:24px; }
    .brand{ display:flex; align-items:center; gap:10px; margin-bottom:10px; }
    .badge{ width:40px; height:40px; border-radius:12px; background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
            display:grid; place-items:center; color:#fff; font-family:Poppins; }
    h1{ font-family:Poppins,sans-serif; font-size:1.5rem; margin:0; }
    p.sub{ color:var(--muted); margin:.25rem 0 1rem; }
    label{ font-weight:600; font-family:Poppins; font-size:.95rem; }
    input[type=password]{ width:100%; padding:12px 14px; border-radius:12px; border:1px solid #e5e7eb; }
    input:focus{ outline:0; box-shadow:0 0 0 5px var(--ring); border-color:var(--fia-blue); }
    .row{ margin-bottom:12px }
    .btnrow{ display:flex; gap:10px; margin-top:14px; flex-wrap:wrap; }
    .btn{ border:0; border-radius:12px; padding:12px 18px; font-weight:700; font-family:Poppins; cursor:pointer; }
    .primary{ color:#fff; background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal)) }
    .val{ color:#c21d1d; font-size:.9rem; margin-top:4px; }
    .err{ color:#c21d1d; }
    .ok{ color:#0a7a3c; }
    .foot{ margin-top:16px; font-size:.9rem; }
    a{ color:var(--fia-blue); text-decoration:none }
    a:hover{ text-decoration:underline }
  </style>
</head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="card">

        <div class="brand">
          <div class="badge">FIA</div>
          <h1>Reset your password</h1>
        </div>

        <!-- Shown when the link is valid -->
        <asp:Panel ID="FormPanel" runat="server">
          <p class="sub">Choose a new password. After you save it, you'll be signed out everywhere else.</p>

          <div class="row">
            <label for="NewPassword">New password</label>
            <asp:TextBox ID="NewPassword" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="NewPassword"
              runat="server"
              ErrorMessage="Please enter a new password."
              Display="Dynamic" />
          </div>

          <div class="row">
            <label for="ConfirmPassword">Confirm new password</label>
            <asp:TextBox ID="ConfirmPassword" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="ConfirmPassword"
              runat="server"
              ErrorMessage="Please confirm your new password."
              Display="Dynamic" />
            <asp:CompareValidator
              CssClass="val"
              ControlToValidate="ConfirmPassword"
              ControlToCompare="NewPassword"
              runat="server"
              ErrorMessage="Passwords don't match."
              Display="Dynamic" />
          </div>

          <div class="btnrow">
            <asp:Button
              ID="BtnReset"
              runat="server"
              Text="Save new password"
              CssClass="btn primary"
              OnClick="BtnReset_Click" />
          </div>

          <asp:Label ID="FormMessage" runat="server" CssClass="err" EnableViewState="false" />
        </asp:Panel>

        <!-- Shown when the link is invalid, expired, or already used -->
        <asp:Panel ID="InvalidPanel" runat="server" Visible="false">
          <p class="err">This reset link is invalid, has expired, or has already been used.</p>
          <div class="foot">
            <asp:HyperLink runat="server" NavigateUrl="~/Account/ForgotPassword.aspx" Text="Request a new link" />
          </div>
        </asp:Panel>

        <!-- Shown after a successful reset -->
        <asp:Panel ID="SuccessPanel" runat="server" Visible="false">
          <p class="ok">Your password has been changed. Any other sessions have been signed out.</p>
          <div class="foot">
            <asp:HyperLink runat="server" NavigateUrl="~/Account/Login.aspx" Text="Go to sign in" />
          </div>
        </asp:Panel>

      </div>
    </div>
  </form>
</body>
</html>