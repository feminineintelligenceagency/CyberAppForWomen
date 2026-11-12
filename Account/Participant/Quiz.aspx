<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Quiz.aspx.cs" Inherits="CyberApp_FIA.Account.Participant.Quiz" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Cybersecurity Quiz</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@500;600&display=swap" rel="stylesheet" />
  <style>
    /* ---------- FIA tokens ---------- */
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1c1c1c;
      --muted:#6b7280;
      --ring:rgba(42,153,219,.25);
      --bg:#f9fbff;
      --card:#ffffff;
      --surface:#f5f8fc;
    }

    /* ---------- Page ---------- */
    *{box-sizing:border-box}
    body{
      margin:0;
      background:
        radial-gradient(1400px 600px at 10% -20%, rgba(240,106,169,.12), transparent 50%),
        radial-gradient(1200px 600px at 110% 0%, rgba(69,195,179,.12), transparent 50%),
        linear-gradient(135deg,#fff,var(--bg));
      font-family:Lato,Arial,sans-serif;
      color:var(--ink);
    }
    .wrap{max-width:960px;margin:24px auto;padding:20px}

    /* ---------- Card ---------- */
    .card{
      background:var(--card);
      border-radius:20px;
      box-shadow:0 16px 40px rgba(0,0,0,.08);
      padding:24px;
      border:1px solid #eaf1fb;
    }

    /* ---------- Header / brand ---------- */
    .brand{
      display:flex;align-items:center;gap:10px;margin-bottom:12px
    }
    h1{
      font-family:Poppins,Arial,sans-serif;font-weight:600;margin:0;
      letter-spacing:.2px
    }
    .eyebrow{
      display:inline-block;
      font-size:.8rem;color:#fff;background:
        linear-gradient(90deg,var(--fia-pink),var(--fia-blue));
      padding:4px 10px;border-radius:999px;margin-bottom:8px
    }
    .sub{color:var(--muted);margin:6px 0 18px}
    .badge{
      display:inline-block;padding:4px 10px;border-radius:999px;
      background:var(--fia-teal);color:#fff;font-size:.8rem;margin-left:auto
    }

    /* ---------- Progress ---------- */
    .progress{
      height:12px;background:#e9eef7;border-radius:999px;overflow:hidden;margin:8px 0 20px;
      position:relative
    }
    .bar{
      height:100%;width:0%;
      background:linear-gradient(90deg,var(--fia-blue),var(--fia-pink));
      transition:width .25s ease;
    }

    /* ---------- Question ---------- */
    .q{font-size:1.15rem;margin:6px 0 14px}
    .meta{display:flex;align-items:center;gap:10px;margin-bottom:8px}
    .kicker{
      color:#fff;background:var(--fia-blue);
      font-size:.75rem;padding:2px 8px;border-radius:999px
    }

    /* ---------- Options (tile radios) ---------- */
    .opts{display:block;margin:0;padding:0}
    .opts input[type="radio"]{
      position:absolute;opacity:0;pointer-events:none
    }
    .opts label{
      display:block;margin:10px 0;padding:14px 14px;
      border:1px solid #e5e7eb;border-radius:14px;cursor:pointer;
      background:linear-gradient(0deg,#fff,#fff);
      transition:box-shadow .15s ease,border-color .15s ease,transform .05s ease;
      line-height:1.35
    }
    .opts label .tag{
      display:inline-block;min-width:26px;text-align:center;
      font-weight:600;margin-right:8px;color:var(--fia-blue)
    }
    .opts input[type="radio"]:focus + label{box-shadow:0 0 0 4px var(--ring)}
    .opts label:hover{border-color:var(--fia-blue);box-shadow:0 0 0 4px var(--ring)}
    .opts input[type="radio"]:checked + label{
      border-color:transparent;
      background:linear-gradient(90deg,rgba(42,153,219,.10),rgba(240,106,169,.10));
      box-shadow:0 6px 22px rgba(42,153,219,.18);
      transform:translateY(-1px)
    }

    /* ---------- Summary / alerts ---------- */
    .summary{
      margin-top:12px;padding:14px;border-radius:12px;background:#f7fbff;border:1px solid #e3f0fb
    }
    .alert{
      margin-top:8px;padding:12px 14px;border-radius:12px;border:1px solid;
      display:flex;gap:10px;align-items:flex-start
    }
    .alert.warn{border-color:#fde3ea;background:#fff3f7}
    .alert.warn .dot{width:10px;height:10px;border-radius:50%;background:var(--fia-pink);margin-top:6px}

    /* ---------- Controls ---------- */
    .controls{display:flex;gap:12px;margin-top:16px;flex-wrap:wrap}
    .btn{
      padding:10px 16px;border-radius:12px;border:0;background:var(--fia-blue);color:#fff;cursor:pointer;
      font-weight:600
    }
    .btn.secondary{background:#eef2f7;color:#111}
    .btn.pink{background:var(--fia-pink);color:#fff}
    .btn[disabled]{opacity:.55;cursor:not-allowed}

    /* ---------- Completion styles ---------- */
    .hr{height:1px;background:#e8eef7;border:0;margin:12px 0}
    .list{margin:6px 0 0 18px}

    @media (max-width:600px){
      .wrap{padding:14px}
      .card{padding:18px}
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
    <form id="form1" runat="server" enableviewstate="true">
  <asp:ScriptManager runat="server" />
  <div class="wrap">
    <div class="card">
      <span class="eyebrow">FIA Cyberfair</span>
      <div class="brand">
        <h1>Cybersecurity Check-In</h1>
        <span class="badge" id="BadgeSaved" runat="server" visible="false">Saved</span>
      </div>
      <div class="sub">Short, plain-language quiz to tailor your experience. You can exit anytime — your progress is saved.</div>
      <div class="progress"><div id="ProgressBar" runat="server" class="bar"></div></div>

      <asp:UpdatePanel runat="server">
      <ContentTemplate>
      <asp:Panel ID="PanelQuestion" runat="server">
        <div class="meta">
          <span id="LblQuestionNumber" runat="server" class="kicker"></span>
        </div>
        <div class="q"><asp:Label ID="LblQuestionText" runat="server" /></div>

        <!-- Tile radio list -->
        <asp:RadioButtonList ID="Options" runat="server"
          AutoPostBack="true" RepeatDirection="Vertical" RepeatLayout="Flow" CssClass="opts"
          OnSelectedIndexChanged="Options_SelectedIndexChanged">
          <Items>
            <asp:ListItem Value="A" />
            <asp:ListItem Value="B" />
            <asp:ListItem Value="C" />
            <asp:ListItem Value="D" />
          </Items>
        </asp:RadioButtonList>

        <!-- Labels for the four items we style via adjacent label -->
        <!-- WebForms renders: input#Options_0 + label[for=Options_0] etc., which our CSS targets -->

        <div id="WarnBox" runat="server" visible="false" class="alert warn">
          <div class="dot"></div>
          <div>Please select an answer to continue.</div>
        </div>

        <div class="summary">
          <span class="disclaimer" style="color:var(--muted)">Your answers are private by default.</span>
        </div>

        <div class="controls">
          <asp:Button ID="BtnPrev" runat="server" Text="Back" CssClass="btn secondary" OnClick="BtnPrev_Click" />
          <asp:Button ID="BtnNext" runat="server" Text="Next" CssClass="btn" OnClick="BtnNext_Click" />
          <asp:Button ID="BtnFinish" runat="server" Text="Finish" CssClass="btn pink" OnClick="BtnFinish_Click" Visible="false" />
        </div>
      </asp:Panel>
    

      <asp:Panel ID="PanelComplete" runat="server" Visible="false">
        <h1>Your Score</h1>
        <div class="summary">
          <asp:Label ID="LblOverall" runat="server"></asp:Label>
          <div class="hr"></div>
          <asp:Repeater ID="RptDomains" runat="server">
            <ItemTemplate>
              • <%# Eval("Key") %>: <strong><%# Eval("Value") %></strong><br />
            </ItemTemplate>
          </asp:Repeater>
          <div class="hr"></div>
          <div><strong>Top 3 factors</strong>
            <asp:BulletedList ID="BlFactors" runat="server" CssClass="list"></asp:BulletedList>
          </div>
          <div style="margin-top:8px"><strong>Quick wins</strong>
            <asp:BulletedList ID="BlWins" runat="server" CssClass="list"></asp:BulletedList>
          </div>
        </div>

        <div class="summary">
          <div class="controls" style="margin-top:10px">
            <asp:Button ID="BtnSaveShare" runat="server" Text="Save &amp; Go to My Home" CssClass="btn" OnClick="BtnSaveShare_Click" />
          </div>
        </div>
     </asp:Panel>
      </ContentTemplate>
      </asp:UpdatePanel>
    </div>
  </div>
</form>
</body>
</html>



