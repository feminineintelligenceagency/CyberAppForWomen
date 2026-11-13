<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SelectEvent.aspx.cs" Inherits="CyberApp_FIA.Participant.SelectEvent" %> 

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Choose your Cyberfair</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600;700&display=swap" rel="stylesheet" />

  <style>
    :root{
      /* Brand */
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;

      /* Neutrals */
      --ink:#111827;
      --muted:#6b7280;
      --bg1:#ffffff;
      --bg2:#f7fbff;

      /* Surfaces */
      --card:#ffffff;
      --border:#e7eef8;
      --soft:#f6f7fb;

      /* Effects */
      --ring:rgba(42,153,219,.28);
      --shadow:0 16px 44px rgba(42,153,219,.12);

      /* Rounded */
      --r12:12px;
      --r16:16px;
      --r20:20px;
      --r24:24px;
    }

    *{ box-sizing:border-box; }
    html,body{ height:100%; }
    body{
      margin:0;
      font-family:"Lato",system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;
      color:var(--ink);
      background:
        radial-gradient(900px 320px at 12% 10%, rgba(240,106,169,.16), transparent 60%),
        radial-gradient(900px 320px at 88% 18%, rgba(42,153,219,.16), transparent 60%),
        radial-gradient(900px 320px at 55% 95%, rgba(69,195,179,.14), transparent 60%),
        linear-gradient(135deg,var(--bg1),var(--bg2));
    }

    /* Centered layout */
    .wrap{
      min-height:100vh;
      display:grid;
      place-items:center;
      padding:24px;
    }

    /* Card */
    .card{
      width:min(720px,100%);
      background:var(--card);
      border:1px solid var(--border);
      border-radius:var(--r24);
      box-shadow:var(--shadow);
      overflow:hidden;
    }

    /* Top brand band */
    .topbar{
      padding:20px 22px;
      background:
        linear-gradient(135deg, rgba(240,106,169,.12), rgba(42,153,219,.10) 55%, rgba(69,195,179,.10));
      border-bottom:1px solid var(--border);
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:16px;
    }

    .brand{
      display:flex;
      align-items:center;
      gap:12px;
      min-width: 220px;
    }
    .badge{
      width:44px; height:44px;
      border-radius:14px;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      display:grid;
      place-items:center;
      color:#fff;
      font-family:"Poppins",sans-serif;
      font-weight:700;
      letter-spacing:.6px;
      box-shadow:0 12px 26px rgba(240,106,169,.18);
    }

    .titles h1{
      font-family:"Poppins",sans-serif;
      margin:0;
      font-size:1.3rem;
      line-height:1.15;
      font-weight:700;
    }
    .titles p{
      margin:4px 0 0 0;
      color:var(--muted);
      font-size:.95rem;
      line-height:1.4;
    }

    /* Decorative dots (subtle FIA feel) */
    .dots{
      display:flex;
      gap:8px;
      align-items:center;
    }
    .dot{
      width:10px; height:10px;
      border-radius:999px;
      opacity:.85;
    }
    .d1{ background:var(--fia-blue); }
    .d2{ background:var(--fia-teal); }
    .d3{ background:var(--fia-pink); }

    /* Content area */
    .content{
      padding:22px;
    }

    /* Form block */
    .block{
      background:linear-gradient(180deg,#ffffff, #fbfdff);
      border:1px solid var(--border);
      border-radius:var(--r20);
      padding:18px;
    }

    .row{ margin-bottom:14px; }

    label{
      display:block;
      margin-bottom:8px;
      font-family:"Poppins",sans-serif;
      font-weight:700;
      font-size:.95rem;
      color:#243046;
    }

    /* Make DropDownList render consistently */
    select,
    .ddl{
      width:100%;
      padding:12px 14px;
      border-radius:var(--r16);
      border:1px solid #e5e7eb;
      background:#fff;
      color:var(--ink);
      font-size:1rem;
      box-shadow:0 6px 18px rgba(0,0,0,.03);
      transition:.15s ease border-color, box-shadow, transform;
    }
    select:focus,
    .ddl:focus{
      outline:0;
      border-color:var(--fia-blue);
      box-shadow:0 0 0 6px var(--ring);
    }

    /* Validation messages */
    .val{
      color:#b42318;
      font-size:.92rem;
      margin-top:6px;
      line-height:1.35;
      font-weight:600;
    }

    /* Empty-state helper messages under dropdown boxes */
/* Keep empty-state messages inside the dropdown only */
.availability-msg{
  display:none !important;
}

.availability-msg.warning{
  display:none !important;
}

    /* Note */
    .note{
      margin-top:14px;
      background:
        radial-gradient(700px 160px at 15% 20%, rgba(42,153,219,.08), transparent 60%),
        radial-gradient(700px 160px at 90% 60%, rgba(240,106,169,.08), transparent 60%),
        var(--soft);
      border:1px solid var(--border);
      border-radius:var(--r20);
      padding:14px 14px;
      color:var(--muted);
      font-size:.95rem;
      line-height:1.55;
    }
    .note strong{
      color:#27324a;
      font-family:"Poppins",sans-serif;
      font-weight:700;
    }

    /* Buttons */
    .btnrow{
      display:flex;
      gap:12px;
      margin-top:16px;
      flex-wrap:wrap;
      align-items:center;
    }

    .btn{
      border:0;
      border-radius:var(--r16);
      padding:12px 18px;
      font-weight:700;
      font-family:"Poppins",sans-serif;
      cursor:pointer;
      text-decoration:none;
      display:inline-flex;
      align-items:center;
      justify-content:center;
      transition:.15s ease transform, box-shadow, background;
      outline:2px solid transparent;
      outline-offset:2px;
      min-height:44px;
    }
    .btn:focus{
      box-shadow:0 0 0 6px var(--ring);
    }
    .btn:active{ transform:translateY(0); }
    .btn:hover{ transform:translateY(-1px); }

    .primary{
      color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
      box-shadow:0 12px 28px rgba(42,153,219,.20);
    }
    .primary:hover{
      box-shadow:0 14px 30px rgba(42,153,219,.25);
    }

    .link{
      background:#fff;
      border:2px solid rgba(42,153,219,.55);
      color:var(--fia-blue);
    }
    .link:hover{
      border-color:var(--fia-blue);
      box-shadow:0 10px 22px rgba(42,153,219,.10);
    }

    /* Message area */
    .msg{
      margin-top:14px;
      padding:12px 14px;
      border-radius:var(--r16);
      border:1px dashed rgba(42,153,219,.35);
      background:rgba(42,153,219,.06);
      color:#27435c;
      font-size:.95rem;
      line-height:1.5;
    }
    /* If the label is empty, it should not take visible space */
    .msg:empty{ display:none; }

    /* Responsive padding */
    @media (max-width:520px){
      .topbar{ padding:18px 16px; }
      .content{ padding:16px; }
      .block{ padding:14px; }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="card">

        <!-- Brand header -->
        <div class="topbar">
          <div class="brand">
            <div class="badge">FIA</div>
            <div class="titles">
              <h1>Choose your Cyberfair</h1>
              <p>Pick your university and active event to get started.</p>
            </div>
          </div>
          <div class="dots" aria-hidden="true">
            <div class="dot d1"></div>
            <div class="dot d2"></div>
            <div class="dot d3"></div>
          </div>
        </div>

        <div class="content">
          <div class="block">

            <!-- ======================= University selection ======================= -->
            <div class="row">
              <label for="UniversitySelect">University</label>

              <asp:DropDownList
  ID="UniversitySelect"
  runat="server"
  CssClass="ddl"
  AutoPostBack="true"
  OnSelectedIndexChanged="UniversitySelect_SelectedIndexChanged" />

<asp:Label
  ID="UniversityAvailabilityMessage"
  runat="server"
  CssClass="availability-msg warning"
  Visible="false" />

<asp:CustomValidator
  ID="UniRequired"
  runat="server"
  CssClass="val"
  OnServerValidate="UniRequired_ServerValidate"
  ErrorMessage="Please select a university."
  Display="Dynamic" />

            <!-- ========================= Event selection ========================= -->
            <div class="row">
              <label for="EventSelect">Active Cyberfair event</label>

           <asp:DropDownList
  ID="EventSelect"
  runat="server"
  CssClass="ddl" />

<asp:Label
  ID="EventAvailabilityMessage"
  runat="server"
  CssClass="availability-msg"
  Visible="false" />

<asp:CustomValidator
  ID="EventRequired"
  runat="server"
  CssClass="val"
  OnServerValidate="EventRequired_ServerValidate"
  ErrorMessage="Please select an event."
  Display="Dynamic" />
           

            <!-- Privacy notice -->
            <div class="note">
              <strong>Privacy:</strong>
              Your learning results are private to you and the FIA team. Your university sees only anonymized or aggregated insights—never your personal answers.
            </div>

            <!-- Actions -->
            <div class="btnrow">
              <asp:Button
                ID="BtnContinue"
                runat="server"
                Text="Continue"
                CssClass="btn primary"
                OnClick="BtnContinue_Click" />

              <a class="btn link" href="<%: ResolveUrl("~/Welcome_Page.aspx") %>">Back to home</a>
            </div>

            <!-- General message area (status/errors), viewstate disabled to avoid stale text -->
            <asp:Label ID="FormMessage" runat="server" EnableViewState="false" CssClass="msg" />

          </div>
        </div>

      </div>
    </div>
  </form>
</body>
</html>

