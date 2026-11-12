<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="HelperMessage.aspx.cs" Inherits="CyberApp_FIA.Participant.HelperMessage" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Message Your Helper</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />
  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1c1c1c;
      --muted:#6b7280;
      --ring:rgba(42,153,219,.25);
      --card-border:#e8eef7;
      --card-bg:#ffffff;
      --page-grad:linear-gradient(135deg,#ffffff,#f9fbff);
    }

    *{ box-sizing:border-box; }
    html,body{ height:100%; }
    body{
      margin:0;
      font-family:Lato, Arial, sans-serif;
      color:var(--ink);
      background:var(--page-grad);
    }

    .wrap{
      min-height:100vh;
      padding:24px;
      max-width:720px;
      margin:0 auto;
    }

    .brand{ display:flex; align-items:center; gap:10px; margin-bottom:18px; }
    .badge{
      width:42px; height:42px; border-radius:12px;
      background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue));
      display:grid; place-items:center; color:#fff; font-family:Poppins;
    }
    h1{ font-family:Poppins; margin:0 0 4px 0; font-size:1.35rem; }
    .sub{ color:var(--muted); margin:0; }

    a.back-link{
      text-decoration:none;
      font-size:.9rem;
      color:var(--fia-blue);
    }

    .card{
      background:var(--card-bg);
      border-radius:20px;
      border:1px solid var(--card-border);
      padding:20px 18px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);
    }

    .card h2{
      font-family:Poppins;
      margin:0 0 8px 0;
      font-size:1.15rem;
    }

    .helper-pill{
      display:inline-block;
      padding:6px 10px;
      border-radius:999px;
      background:linear-gradient(135deg,var(--fia-pink),#ff86bf);
      border:1px solid rgba(240,106,169,.55);
      color:#fff;
      font-family:Poppins;
      font-size:.9rem;
      margin-bottom:6px;
    }

    .helper-text{ color:var(--muted); font-size:.93rem; margin-bottom:14px; }

    .field{ margin-bottom:14px; display:flex; flex-direction:column; gap:6px; }
    .field label{
      font-size:.9rem;
      color:var(--fia-blue);
      font-weight:600;
      padding-left:4px;
    }

    .input, .textarea{
      width:100%;
      border-radius:12px;
      border:1px solid #d9e9f6;
      padding:9px 11px;
      font-size:.93rem;
      font-family:Lato, Arial, sans-serif;
      background:linear-gradient(180deg,#f0f7fd,#ffffff);
      color:var(--ink);
      outline:none;
      resize:vertical;
      min-height:36px;
    }
    .textarea{ min-height:120px; }
    .input:focus, .textarea:focus{
      border-color:#bfe6ff;
      box-shadow:0 0 0 4px var(--ring);
      background:#fff;
    }

    .helper-hint{
      font-size:.85rem;
      color:var(--muted);
      margin-top:-6px;
      margin-bottom:10px;
      padding-left:4px;
    }

    .cta-row{
      display:flex;
      gap:10px;
      margin-top:8px;
    }
    .btn{
      appearance:none;
      border:none;
      cursor:pointer;
      border-radius:12px;
      padding:10px 14px;
      font-family:Poppins;
      font-weight:600;
      box-shadow:0 2px 0 rgba(0,0,0,.04);
    }
    .btn-primary{
      background:linear-gradient(135deg,var(--fia-blue),#6bc1f1);
      color:#fff;
    }
    .btn-ghost{
      background:#fff;
      color:var(--fia-blue);
      border:1px solid #d9e9f6;
    }
    .btn:focus{
      outline:none;
      box-shadow:0 0 0 4px var(--ring);
    }

    .form-message{
      margin-top:14px;
      font-size:.9rem;
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="brand">
        <div class="badge">FIA</div>
        <div>
          <h1>Message your Helper</h1>
          <p class="sub">
            <a href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>" class="back-link">← Back to Your Cyberfair</a>
          </p>
        </div>
      </div>

      <div class="card">
        <span class="helper-pill">
          Helper: <asp:Literal ID="HelperName" runat="server" />
        </span>
        <p class="helper-text">
          Use this form to ask for one-on-one help with a cybersecurity topic you are struggling with.
          Your Helper will receive your message and can follow up with a time to meet.
        </p>

        <div class="helper-hint">
          Tip: Choose a clear topic title, describe exactly what you need help with, and share a few times you are available.
        </div>

        <div class="field">
          <label for="TopicText">Topic / short title</label>
          <asp:TextBox ID="TopicText" runat="server" CssClass="input" MaxLength="150" />
        </div>

        <div class="field">
          <label for="BodyText">Message</label>
          <asp:TextBox ID="BodyText" runat="server" CssClass="textarea" TextMode="MultiLine" Rows="6" />
        </div>

        <div class="cta-row">
          <asp:Button ID="SendButton"
                      runat="server"
                      CssClass="btn btn-primary"
                      Text="Send message"
                      OnClick="SendButton_Click" />
          <asp:Button ID="CancelButton"
                      runat="server"
                      CssClass="btn btn-ghost"
                      Text="Cancel"
                      CausesValidation="false"
                      OnClick="CancelButton_Click" />
        </div>

        <asp:Label ID="FormMessage" runat="server" CssClass="form-message" EnableViewState="false" />
      </div>
    </div>
  </form>
</body>
</html>

