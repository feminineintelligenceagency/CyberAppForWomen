<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Badges.aspx.cs" Inherits="CyberApp_FIA.Helper.Badges" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Helper Badges</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1f2933;
      --muted:#6b7280;
      --bg:#f3f5fb;
      --card-bg:#ffffff;
      --card-border:#e2e8f0;
    }

    *{ box-sizing:border-box; }

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
      max-width:1120px;
      margin:0 auto;
      padding:24px 16px 40px;
    }

    .hero{
      border-radius:24px;
      padding:20px 22px 22px;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.15), transparent 55%),
        radial-gradient(circle at 100% 0, rgba(69,195,179,0.20), transparent 55%),
        linear-gradient(120deg,#fbfbff,#f3f7ff);
      border:1px solid rgba(226,232,240,0.9);
      box-shadow:0 18px 40px rgba(15,23,42,0.10);
      margin-bottom:18px;
      display:flex;
      align-items:flex-start;
      justify-content:space-between;
      gap:18px;
    }

    .hero-main{ max-width:720px; }

    .hero-eyebrow{
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

    .hero-eyebrow-pill{
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

    .hero-title{
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.7rem;
      margin:0 0 6px 0;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-pink));
      -webkit-background-clip:text;
      color:transparent;
    }

    .hero-sub{
      margin:0;
      font-size:0.95rem;
      color:var(--muted);
    }

    .back-link{
      display:inline-flex;
      align-items:center;
      gap:6px;
      padding:7px 11px;
      border-radius:999px;
      border:1px solid rgba(148,163,184,0.6);
      background:#ffffff;
      font-size:.85rem;
      font-weight:600;
      color:#374151;
      text-decoration:none;
      white-space:nowrap;
      box-shadow:0 10px 24px rgba(15,23,42,0.12);
    }

    .grid{
      display:grid;
      grid-template-columns:repeat(auto-fit, minmax(320px, 1fr));
      gap:12px;
      margin-top:12px;
    }

    .card{
      background:var(--card-bg);
      border-radius:18px;
      border:1px solid var(--card-border);
      padding:14px 14px 14px;
      box-shadow:0 14px 30px rgba(15,23,42,0.05);
      display:flex;
      gap:14px;
      align-items:flex-start;
      overflow:hidden;
      position:relative;
    }

    .card::before{
      content:"";
      position:absolute;
      inset-inline-start:0;
      top:0;
      bottom:0;
      width:5px;
      background:linear-gradient(180deg,var(--fia-pink),var(--fia-blue));
      opacity:0.55;
    }

    .badge-img{
      width:74px;
      height:74px;
      border-radius:16px;
      border:1px solid #eef2f7;
      background:#fff;
      object-fit:contain;
      flex:0 0 auto;
      margin-left:4px;
    }

    .meta{ min-width:0; }

    .title{
      font-family:Poppins, system-ui, sans-serif;
      font-size:1.02rem;
      margin:0 0 6px 0;
      font-weight:600;
      line-height:1.25;
      overflow-wrap:anywhere;
    }

    .desc{
      margin:0;
      color:var(--muted);
      font-size:0.92rem;
      line-height:1.35;
      overflow-wrap:anywhere;
    }

    .row{
      margin-top:10px;
      display:flex;
      flex-wrap:wrap;
      gap:8px;
    }

    .pill{
      display:inline-flex;
      align-items:center;
      padding:5px 10px;
      border-radius:999px;
      font-size:0.82rem;
      font-weight:600;
      border:1px solid rgba(148,163,184,0.45);
      background:#ffffff;
      color:#374151;
      white-space:nowrap;
    }

    .pill-tier{
      border-color:rgba(240,106,169,0.30);
      background:linear-gradient(135deg, rgba(240,106,169,0.10), rgba(42,153,219,0.10));
    }

    .pill-progress{
      border-color:rgba(69,195,179,0.35);
      background:linear-gradient(135deg, rgba(69,195,179,0.12), #ffffff);
      color:#0a5b4e;
    }

    .empty{
      background:#ffffff;
      border:1px solid var(--card-border);
      border-radius:18px;
      padding:14px;
      color:var(--muted);
      box-shadow:0 14px 30px rgba(15,23,42,0.04);
    }

    @media (max-width:720px){
      .hero{ flex-direction:column; }
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <div class="hero">
        <div class="hero-main">
          <div class="hero-eyebrow">
            <span class="hero-eyebrow-pill">FIA</span>
            <span>Helper workspace</span>
          </div>
          <h1 class="hero-title">Your badges</h1>
          <p class="hero-sub">
            Badges are earned automatically from your teaching progress for each microcourse.<br />
            Tier1 is earned at 5 sessions taught, Tier2 at 10, and Tier3 at 20.
          </p>
        </div>

        <a href="<%: ResolveUrl("~/Account/Helper/Home.aspx") %>" class="back-link">
          ← Back to Helper Home
        </a>
      </div>

      <asp:PlaceHolder ID="EmptyPH" runat="server" Visible="false">
        <div class="empty">
          You don’t have any teaching badges yet. Teach more sessions to start earning Tier1, Tier2, and Tier3 badges.
        </div>
      </asp:PlaceHolder>

      <asp:Repeater ID="BadgesRepeater" runat="server">
        <HeaderTemplate><div class="grid"></HeaderTemplate>
        <ItemTemplate>
          <div class="card">
            <img class="badge-img" src="<%# Eval("ImageUrl") %>" alt="" />
            <div class="meta">
              <p class="title"><%# Eval("CourseTitle") %></p>
              <p class="desc"><%# Eval("Description") %></p>

              <div class="row">
                <span class="pill pill-tier"><%# Eval("TierLabel") %></span>
                <span class="pill pill-progress"><%# Eval("ProgressLabel") %></span>
              </div>
            </div>
          </div>
        </ItemTemplate>
        <FooterTemplate></div></FooterTemplate>
      </asp:Repeater>

    </div>
  </form>
</body>
</html>