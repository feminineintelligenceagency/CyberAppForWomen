<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="VerifyCertificate.aspx.cs" Inherits="CyberApp_FIA.VerifyCertificate" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
  <title>FIA • Verify a Certificate</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <meta name="robots" content="noindex" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400;700&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{--fia-pink:#f06aa9;--fia-blue:#2a99db;--fia-teal:#45c3b3;--ink:#1c1c1c;--muted:#6b7280;--ring:rgba(42,153,219,.25)}
    *{box-sizing:border-box}
    body{margin:0;font-family:Lato,Arial,sans-serif;color:var(--ink);background:linear-gradient(135deg,#fff,#f9fbff)}
    .wrap{min-height:100vh;display:grid;place-items:center;padding:24px 16px}
    .card{
      width:min(620px,100%);
      background:#fff;border:1px solid #e8eef7;border-radius:20px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);padding:26px
    }
    .brand{display:flex;align-items:center;gap:10px;margin-bottom:6px}
    .badge{
      width:42px;height:42px;border-radius:12px;flex:0 0 auto;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      display:grid;place-items:center;color:#fff;font-family:Poppins
    }
    h1{font-family:Poppins,sans-serif;font-size:1.45rem;margin:0}
    .sub{color:var(--muted);margin:.4rem 0 1.2rem;line-height:1.5}

    label{display:block;font-family:Poppins,sans-serif;font-size:.95rem;margin-bottom:6px}
    .row{display:flex;gap:10px;flex-wrap:wrap}
    .code-input{
      flex:1 1 260px;min-width:0;padding:12px 14px;border-radius:12px;border:1px solid #e5e7eb;
      font-size:16px;font-family:Consolas,monospace;letter-spacing:.06em;text-transform:uppercase
    }
    .code-input:focus{outline:0;box-shadow:0 0 0 5px var(--ring);border-color:var(--fia-blue)}
    .btn{
      border:0;border-radius:12px;padding:12px 20px;min-height:44px;cursor:pointer;
      font-weight:700;font-family:Poppins,sans-serif;color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal))
    }
    .hint{color:var(--muted);font-size:.85rem;margin-top:6px}

    .result{margin-top:20px;border-radius:14px;padding:16px 18px}
    .result h2{font-family:Poppins,sans-serif;font-size:1.1rem;margin:0 0 10px}
    .valid{background:#ecfbf7;border:1px solid rgba(69,195,179,.45)}
    .valid h2{color:#0a5b4e}
    .invalid{background:#fff4f4;border:1px solid #f4c7c7}
    .invalid h2{color:#b42318}
    .invalid p{margin:0;color:#7a271a;line-height:1.5}

    dl{display:grid;grid-template-columns:max-content 1fr;gap:8px 16px;margin:0}
    dt{color:var(--muted);font-size:.9rem}
    dd{margin:0;font-weight:700;overflow-wrap:anywhere}
    .mono{font-family:Consolas,monospace}
    @media (max-width:480px){dl{grid-template-columns:1fr}dd{margin-bottom:6px}}

    .foot{margin-top:18px;color:var(--muted);font-size:.82rem;line-height:1.5}
  </style>
</head>
<body>
  <form id="form1" runat="server" defaultbutton="BtnVerify">
    <div class="wrap">
      <main class="card">
        <div class="brand">
          <div class="badge">FIA</div>
          <h1>Verify a Certificate</h1>
        </div>
        <p class="sub">
          Enter the verification number printed on an FIA certificate of completion to confirm it is genuine.
        </p>

        <label for="<%= CodeInput.ClientID %>">Verification number</label>
        <div class="row">
          <asp:TextBox ID="CodeInput" runat="server" CssClass="code-input" MaxLength="40"
            placeholder="FIA-XXXX-XXXX-XXXX" autocomplete="off" spellcheck="false" />
          <asp:Button ID="BtnVerify" runat="server" Text="Verify" CssClass="btn" OnClick="BtnVerify_Click" />
        </div>
        <div class="hint">Capital letters, spaces and dashes don't matter.</div>

        <!-- Valid certificate -->
        <asp:PlaceHolder ID="ValidPH" runat="server" Visible="false">
          <div class="result valid" role="status">
            <h2>&#10004; Valid certificate</h2>
            <dl>
              <dt>Awarded to</dt>
              <dd><asp:Literal ID="NameLit" runat="server" Mode="Encode" /></dd>
              <dt>Completed</dt>
              <dd><asp:Literal ID="CourseLit" runat="server" Mode="Encode" /></dd>
              <dt>Date issued</dt>
              <dd><asp:Literal ID="DateLit" runat="server" Mode="Encode" /></dd>
              <dt>Verification no.</dt>
              <dd class="mono"><asp:Literal ID="CodeLit" runat="server" Mode="Encode" /></dd>
            </dl>
          </div>
        </asp:PlaceHolder>

        <!-- Not found / error / too many attempts -->
        <asp:PlaceHolder ID="InvalidPH" runat="server" Visible="false">
          <div class="result invalid" role="alert">
            <h2><asp:Literal ID="InvalidTitleLit" runat="server" Mode="Encode" /></h2>
            <p><asp:Literal ID="InvalidTextLit" runat="server" Mode="Encode" /></p>
          </div>
        </asp:PlaceHolder>

        <p class="foot">
          This page only confirms that a certificate was issued by the Feminine Intelligence Agency.
          It does not show any other information about the certificate holder.
        </p>
      </main>
    </div>
  </form>
</body>
</html>
