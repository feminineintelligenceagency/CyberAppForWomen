<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="CyberApp_FIA.Account.ForgotPassword" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Forgot Password</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
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
    input[type=text], input[type=email]{ width:100%; padding:12px 14px; border-radius:12px; border:1px solid #e5e7eb; }
    input:focus{ outline:0; box-shadow:0 0 0 5px var(--ring); border-color:var(--fia-blue); }
    .row{ margin-bottom:12px }
    .btnrow{ display:flex; gap:10px; margin-top:14px; flex-wrap:wrap; }
    .btn{ border:0; border-radius:12px; padding:12px 18px; font-weight:700; font-family:Poppins; cursor:pointer; }
    .primary{ color:#fff; background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal)) }
    .val{ color:#c21d1d; font-size:.9rem; margin-top:4px; }
    .msg{ display:block; margin-top:14px; color:#27435c; }
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
          <h1>Forgot your password?</h1>
        </div>

        <asp:Panel ID="FormPanel" runat="server">
          <p class="sub">Enter the email you signed up with. We'll send you a link to reset your password.</p>

          <div class="row">
            <label for="Email">Email</label>
            <asp:TextBox ID="Email" runat="server" TextMode="Email" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="Email"
              runat="server"
              ErrorMessage="Email is required."
              Display="Dynamic" />
          </div>

          <div class="btnrow">
            <asp:Button
              ID="BtnSend"
              runat="server"
              Text="Send reset link"
              CssClass="btn primary"
              OnClick="BtnSend_Click" />
          </div>
        </asp:Panel>

        <asp:Label ID="FormMessage" runat="server" CssClass="msg" EnableViewState="false" />

        <div class="foot">
          <asp:HyperLink runat="server" NavigateUrl="~/Account/Login.aspx" Text="Back to sign in" />
        </div>

      </div>
    </div>
  </form>
</body>
</html>