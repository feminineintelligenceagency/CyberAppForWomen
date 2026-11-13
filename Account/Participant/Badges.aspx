<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Badges.aspx.cs" Inherits="CyberApp_FIA.Participant.Badges" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • My Badges</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --fia-teal:#45c3b3;
      --ink:#1c1c1c;
      --muted:#6b7280;
      --card-border:#e8eef7;
      --ring:rgba(42,153,219,.25);
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
      max-width:1100px;
      margin:0 auto;
    }

    .brand{ display:flex; align-items:center; gap:10px; margin-bottom:10px; }
    .badgeMark{
      width:42px; height:42px; border-radius:12px;
      background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue));
      display:grid; place-items:center; color:#fff; font-family:Poppins;
    }

    h1{ font-family:Poppins; margin:0; font-size:1.35rem; }
    .sub{ color:var(--muted); margin:6px 0 16px 0; }

    .topRow{ display:flex; align-items:center; gap:10px; flex-wrap:wrap; }
    .pill{
      display:inline-block;
      padding:6px 10px;
      border-radius:999px;
      background:#f6f7fb;
      border:1px solid var(--card-border);
      font-size:.9rem;
      text-decoration:none;
      color:inherit;
    }
    .pill-link{
      color:var(--fia-blue);
      border-color:#d9e9f6;
      background:#f0f7fd;
      font-weight:600;
    }

    .grid{
      display:grid;
      grid-template-columns:repeat(2, minmax(0, 1fr));
      gap:14px;
      margin-top:14px;
    }
    @media (max-width: 720px){
      .grid{ grid-template-columns:1fr; }
      .wrap{ padding:16px; }
    }

    .card{
      background:#fff;
      border:1px solid var(--card-border);
      border-radius:18px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);
      padding:14px;
      display:flex;
      gap:14px;
      align-items:flex-start;
      overflow:hidden;
    }

    .badgeImg{
      width:72px;
      height:72px;
      flex:0 0 auto;
      border-radius:14px;
      border:1px solid #eef3fb;
      background:#fff;
      object-fit:contain;
    }

    .meta{ min-width:0; }
    .title{
      font-family:Poppins;
      font-weight:600;
      margin:0 0 6px 0;
      font-size:1.05rem;
      line-height:1.25;
      overflow-wrap:anywhere;
    }
    .desc{
      margin:0;
      color:var(--muted);
      font-size:.95rem;
      line-height:1.35;
      overflow-wrap:anywhere;
    }
    .earned{
      margin-top:10px;
      display:inline-flex;
      gap:8px;
      align-items:center;
      padding:6px 10px;
      border-radius:999px;
      border:1px solid rgba(69,195,179,.35);
      background:linear-gradient(180deg,#e6fbf7,#ffffff);
      color:#0a5b4e;
      font-size:.85rem;
      font-weight:600;
      width:fit-content;
    }

    .empty{
      background:#fff;
      border:1px solid var(--card-border);
      border-radius:18px;
      padding:14px;
      color:var(--muted);
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <div class="brand">
        <div class="badgeMark">FIA</div>
        <div>
          <div class="topRow">
            <h1 style="margin-right:10px;">My Badges</h1>
            <a class="pill pill-link" href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>">Back to Home</a>
          </div>
          <p class="sub">Badges you’ve earned by completing FIA microcourses.</p>
        </div>
      </div>

      <asp:PlaceHolder ID="EmptyPH" runat="server" Visible="false">
        <div class="empty">
          You don’t have any badges yet. Complete a microcourse session to earn your first badge.
        </div>
      </asp:PlaceHolder>

      <asp:Repeater ID="BadgesRepeater" runat="server">
        <HeaderTemplate>
          <div class="grid">
        </HeaderTemplate>
        <ItemTemplate>
          <div class="card">
            <img class="badgeImg" src="<%# Eval("ImageUrl") %>" alt="" />
            <div class="meta">
              <p class="title"><%# Eval("Title") %></p>
              <p class="desc"><%# Eval("Description") %></p>
              <span class="earned">Earned • <%# Eval("EarnedOnLabel") %></span>
            </div>
          </div>
        </ItemTemplate>
        <FooterTemplate>
          </div>
        </FooterTemplate>
      </asp:Repeater>

    </div>
  </form>
</body>
</html>