<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="Verify2FA.aspx.cs"
    Inherits="CyberApp_FIA.Account.Verify2FA" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Two-Factor Authentication</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap"
        rel="stylesheet" />

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

    body{
      margin:0;
      font-family:Lato,Arial,sans-serif;
      background:linear-gradient(135deg,#fff,#f9fbff);
    }

    .wrap{
      min-height:100vh;
      display:grid;
      place-items:center;
      padding:24px;
    }

    .card{
      width:min(560px,100%);
      background:#fff;
      border:1px solid #e8eef7;
      border-radius:20px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);
      padding:24px;
    }

    .brand{
      display:flex;
      align-items:center;
      gap:10px;
      margin-bottom:10px;
    }

    .badge{
      width:40px;
      height:40px;
      border-radius:12px;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      display:grid;
      place-items:center;
      color:#fff;
      font-family:Poppins;
    }

    h1{
      font-family:Poppins,sans-serif;
      font-size:1.5rem;
      margin:0;
    }

    p.sub{
      color:var(--muted);
      margin:.25rem 0 1rem;
    }

    label{
      font-weight:600;
      font-family:Poppins;
      font-size:.95rem;
    }

    input[type=text]{
      width:100%;
      padding:12px 14px;
      border-radius:12px;
      border:1px solid #e5e7eb;
      font-size:1rem;
    }

    input:focus{
      outline:0;
      box-shadow:0 0 0 5px var(--ring);
      border-color:var(--fia-blue);
    }

    .row{
      margin-bottom:12px;
    }

    .btnrow{
      display:flex;
      gap:10px;
      margin-top:14px;
    }

    .btn{
      border:0;
      border-radius:12px;
      padding:12px 18px;
      font-weight:700;
      font-family:Poppins;
      cursor:pointer;
    }

    .primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
    }

    .message{
      color:#c21d1d;
      font-size:.9rem;
      margin-top:12px;
    }
  </style>
</head>

<body>
  <div class="wrap">
    <div class="card">

      <div class="brand">
        <div class="badge">FIA</div>
        <h1>Two-Factor Authentication</h1>
      </div>

      <p class="sub">
        Enter the 6-digit code from your authenticator app to finish signing in.
      </p>

      <form method="post">

        <div class="row">
          <label for="totpCode">Authentication code</label>

          <input
            type="text"
            name="totpCode"
            maxlength="19"
            autocomplete="one-time-code"
            required />
        </div>

        <div class="btnrow">
          <button type="submit" class="btn primary">
            Verify
          </button>
        </div>

      </form>

      <p class="message"><%= VerificationMessage %></p>

    </div>
  </div>
</body>
</html>