<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="CyberApp_FIA.Helper.Home" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Helper Workspace</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <!-- Fonts -->
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    /* ---------- Design tokens (brand) ---------- */
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1c1c1c;
      --muted:#6b7280;
      --bg:#f3f5fb;
      --card-bg:#ffffff;
      --card-border:#e2e8f0;
      --pill-bg:#edf2ff;
      --status-green:#16a34a;
      --status-blue:#2563eb;
    }

    *{
      box-sizing:border-box;
    }

    body{
      margin:0;
      font-family:Lato, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
      background:var(--bg);
      color:var(--ink);
    }

    .wrap{
      max-width:1120px;
      margin:0 auto;
      padding:24px 16px 40px;
    }

    /* ---------- Top hero / identity panel ---------- */
    .helper-hero{
      position:relative;
      border-radius:24px;
      padding:24px 24px 26px;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.12), transparent 55%),
        radial-gradient(circle at 100% 0, rgba(69,195,179,0.16), transparent 55%),
        linear-gradient(120deg, #fdfbff, #f3f7ff);
      border:1px solid rgba(226,232,240,0.8);
      box-shadow:0 18px 40px rgba(15,23,42,0.08);
      overflow:hidden;
    }

    .helper-hero-main{
      display:flex;
      align-items:center;
      gap:18px;
    }

    .helper-badge{
      width:52px;
      height:52px;
      border-radius:18px;
      background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue));
      display:grid;
      place-items:center;
      color:#fff;
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.1rem;
      letter-spacing:0.08em;
    }

    .helper-heading{
      font-family:Poppins, system-ui, sans-serif;
      margin:0 0 4px 0;
      font-size:1.55rem;
    }

    .helper-heading span.static{
      opacity:0.8;
      font-weight:600;
    }

    .helper-name{
      font-weight:600;
    }

    .helper-sub{
      margin:0;
      color:var(--muted);
      font-size:0.95rem;
      max-width:480px;
    }

    .helper-chips{
      margin-top:16px;
      display:flex;
      flex-wrap:wrap;
      gap:8px 10px;
    }

    .pill{
      display:inline-flex;
      align-items:center;
      gap:4px;
      padding:6px 11px;
      border-radius:999px;
      background:var(--pill-bg);
      border:1px solid rgba(148,163,184,0.35);
      font-size:0.85rem;
      line-height:1.3;
      white-space:nowrap;
    }

    .pill-label{
      font-weight:600;
      color:#4b5563;
    }

    .pill-value{
      color:#111827;
    }

    .pill-soft{
      background:linear-gradient(135deg, rgba(42,153,219,0.08), rgba(240,106,169,0.08));
      border-color:rgba(129,140,248,0.3);
    }

    .helper-signout{
      position:absolute;
      top:18px;
      right:18px;
      border:none;
      background:#ffffff;
      padding:6px 12px;
      border-radius:999px;
      font-size:0.85rem;
      font-weight:600;
      font-family:Lato, system-ui, sans-serif;
      color:#b91c1c;
      box-shadow:0 10px 24px rgba(15,23,42,0.10);
      cursor:pointer;
      display:inline-flex;
      align-items:center;
      gap:6px;
      text-decoration:none;
    }

    .helper-signout::before{
      content:"⟲";
      font-size:0.9rem;
    }

    .helper-signout:hover{
      filter:brightness(0.98);
      transform:translateY(-0.5px);
    }

    /* ---------- Main below the hero ---------- */
    .helper-main{
      margin-top:22px;
      display:grid;
      grid-template-columns:minmax(0, 2fr);
      gap:18px;
    }

    .helper-card{
      background:var(--card-bg);
      border-radius:18px;
      border:1px solid var(--card-border);
      padding:18px 18px 20px;
      box-shadow:0 14px 30px rgba(15,23,42,0.04);
    }

    .helper-card + .helper-card{
      margin-top:6px;
    }

    .helper-card-title{
      margin:0 0 4px 0;
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.1rem;
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:10px;
    }

    .helper-card-title span.badge-mini{
      border-radius:999px;
      padding:4px 9px;
      font-size:0.75rem;
      text-transform:uppercase;
      letter-spacing:0.06em;
      background:linear-gradient(135deg, rgba(240,106,169,0.12), rgba(42,153,219,0.12));
      color:#374151;
    }

    .helper-card-body{
      margin:0;
      color:var(--muted);
      font-size:0.95rem;
      line-height:1.5;
    }

    .helper-card-actions{
      margin-top:12px;
      display:flex;
      flex-wrap:wrap;
      gap:10px;
    }

    .helper-link-btn{
      display:inline-flex;
      align-items:center;
      gap:6px;
      border-radius:999px;
      padding:9px 14px;
      font-size:0.9rem;
      font-weight:600;
      font-family:Poppins, system-ui, sans-serif;
      border:0;
      cursor:pointer;
      text-decoration:none;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
      color:#fff;
      box-shadow:0 10px 26px rgba(37,99,235,0.18);
    }

    .helper-link-btn span.icon{
      font-size:0.95rem;
    }

    .helper-link-secondary{
      background:#ffffff;
      color:var(--status-blue);
      border:1px solid rgba(148,163,184,0.5);
      box-shadow:none;
    }

    @media (max-width: 720px){
      .helper-hero{
        padding:18px 16px 18px;
        border-radius:20px;
      }
      .helper-hero-main{
        align-items:flex-start;
      }
      .helper-badge{
        width:46px;
        height:46px;
        border-radius:16px;
        font-size:1rem;
      }
      .helper-heading{
        font-size:1.3rem;
      }
      .helper-signout{
        top:14px;
        right:14px;
        padding:5px 10px;
      }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <!-- Top helper identity panel -->
      <div class="helper-hero">

        <asp:LinkButton ID="BtnLogout"
                        runat="server"
                        CssClass="helper-signout"
                        OnClick="BtnLogout_Click"
                        CausesValidation="false">
          Sign out
        </asp:LinkButton>

        <div class="helper-hero-main">
          <div class="helper-badge">FIA</div>
          <div>
            <h1 class="helper-heading">
              <span class="static">Welcome, </span>
              <span class="helper-name">
                <asp:Literal ID="HelperName" runat="server" />
              </span>
            </h1>
            <p class="helper-sub">
              Thank you for showing up as a peer helper. This workspace keeps your sessions,
              prep, and 1:1 support in one calm place.
            </p>
          </div>
        </div>

        <div class="helper-chips">
          <span class="pill">
            <span class="pill-label">University:</span>
            <span class="pill-value">
              <asp:Literal ID="University" runat="server" />
            </span>
          </span>

          <span class="pill pill-soft">
            <span class="pill-label">Role:</span>
            <span class="pill-value">
              <asp:Literal ID="RoleLiteral" runat="server" />
            </span>
          </span>
        </div>
      </div>

      <!-- Helper workspace content -->
      <div class="helper-main">

        <!-- Certification progress card -->
        <div class="helper-card">
          <h2 class="helper-card-title">
            Cybersecurity Microcourse Certification
            <span class="badge-mini">Progress</span>
          </h2>
          <p class="helper-card-body">
            See how close you are to being certified in each FIA cybersecurity microcourse.
            On the next page you can view your module-by-module requirements, track quiz
            scores, teaching and 1:1 help progress, and open each microcourse’s videos,
            slides, and quizzes.
          </p>
          <div class="helper-card-actions">
            <a href="<%: ResolveUrl("~/Account/Helper/CertificationProgress.aspx") %>" class="helper-link-btn">
              <span class="icon">🛡️</span>
              <span>Open certification progress</span>
            </a>
          </div>
        </div>

        <!-- Schedule card -->
        <div class="helper-card">
          <h2 class="helper-card-title">
            Your upcoming sessions
            <span class="badge-mini">Schedule</span>
          </h2>
          <p class="helper-card-body">
            Jump to your schedule to see every session you’re leading or supporting.
            You’ll be able to review dates, times, rooms or links, and make sure you
            have enough space between sessions to stay present with each group.
          </p>
          <div class="helper-card-actions">
            <a href="<%: ResolveUrl("~/Account/Helper/Schedule.aspx") %>" class="helper-link-btn">
              <span class="icon">📅</span>
              <span>Open your schedule</span>
            </a>
          </div>
        </div>

        <!-- 1:1 Help card -->
        <div class="helper-card">
          <h2 class="helper-card-title">
            1:1 Help Sessions
            <span class="badge-mini">Support</span>
          </h2>
          <p class="helper-card-body">
            One-on-one help sessions are where you sit with a participant to walk through
            real tasks like setting up two-factor authentication, updating privacy settings,
            or installing password managers. This view shows all of the participants who are
            assigned to you so you always know who you are supporting.
          </p>
          <div class="helper-card-actions">
            <a href="<%: ResolveUrl("~/Account/Helper/OneOnOneHelp.aspx") %>" class="helper-link-btn">
              <span class="icon">🤝</span>
              <span>See assigned participants</span>
            </a>
          </div>
        </div>

          <!-- Badges card -->
<div class="helper-card">
  <h2 class="helper-card-title">
    Your badges
    <span class="badge-mini">Badges</span>
  </h2>
  <p class="helper-card-body">
    Badges are awarded automatically as you teach more sessions for each microcourse.
    Bronze is earned at 5 sessions taught, Silver at 10, and Gold at 20.
  </p>
  <div class="helper-card-actions">
    <a href="<%: ResolveUrl("~/Account/Helper/Badges.aspx") %>" class="helper-link-btn">
      <span class="icon">🏅</span>
      <span>View your badges</span>
    </a>
  </div>
</div>

      </div>

    </div>
  </form>
</body>
</html>

