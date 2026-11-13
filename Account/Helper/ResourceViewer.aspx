<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ResourceViewer.aspx.cs" Inherits="CyberApp_FIA.Helper.ResourceViewer" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Microcourse Resources</title>
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
      padding:20px 16px 32px;
    }

    .rv-hero{
      border-radius:24px;
      padding:18px 20px 18px;
      background:
        radial-gradient(circle at 0 0, rgba(240,106,169,0.18), transparent 55%),
        radial-gradient(circle at 100% 0, rgba(69,195,179,0.18), transparent 55%),
        linear-gradient(120deg,#fbfbff,#f3f7ff);
      border:1px solid rgba(226,232,240,0.95);
      box-shadow:0 16px 36px rgba(15,23,42,0.10);
      margin-bottom:14px;
      display:flex;
      justify-content:space-between;
      gap:16px;
    }

    .rv-hero-main{
      max-width:640px;
    }

    .rv-eyebrow{
      font-size:0.75rem;
      letter-spacing:0.18em;
      text-transform:uppercase;
      color:#6b7280;
      font-weight:700;
      margin-bottom:4px;
      display:flex;
      align-items:center;
      gap:8px;
    }

    .rv-eyebrow-pill{
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

    .rv-title{
      font-family:Poppins,system-ui,sans-serif;
      font-size:1.4rem;
      margin:0 0 4px 0;
    }

    .rv-sub{
      margin:0;
      font-size:0.9rem;
      color:var(--muted);
    }

    .rv-course-chip{
      padding:6px 10px;
      border-radius:999px;
      background:#ffffff;
      border:1px solid #e5e7eb;
      font-size:0.8rem;
      font-family:Poppins,system-ui,sans-serif;
      display:inline-flex;
      align-items:center;
      gap:6px;
      box-shadow:0 8px 20px rgba(15,23,42,0.12);
      max-width:220px;
    }

    .rv-course-chip span.label{
      font-weight:600;
      color:var(--fia-blue);
    }

    .rv-frame-shell{
      border-radius:18px;
      border:1px solid var(--card-border);
      background:var(--card-bg);
      box-shadow:0 12px 30px rgba(15,23,42,0.08);
      overflow:hidden;
    }

    .rv-toolbar{
      padding:8px 12px;
      display:flex;
      justify-content:space-between;
      align-items:center;
      gap:10px;
      border-bottom:1px solid #e5e7eb;
      font-size:0.8rem;
      background:linear-gradient(90deg,rgba(240,106,169,0.05),rgba(42,153,219,0.05));
    }

    .rv-toolbar span{
      color:var(--muted);
    }

    .rv-fallback-link{
      text-decoration:none;
      font-weight:600;
      color:var(--fia-blue);
      padding:4px 10px;
      border-radius:999px;
      background:rgba(42,153,219,0.10);
    }

    .rv-fallback-link:hover{
      background:rgba(42,153,219,0.18);
    }

    .rv-frame{
      width:100%;
      height:76vh;
      border:0;
    }

    .rv-message{
      margin-top:10px;
      font-size:0.82rem;
      color:var(--muted);
    }

    .rv-error{
      color:#b91c1c;
    }

    @media (max-width:720px){
      .rv-hero{flex-direction:column;}
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <div class="rv-hero">
        <div class="rv-hero-main">
          <div class="rv-eyebrow">
            <span class="rv-eyebrow-pill">FIA</span>
            <span>Microcourse resources</span>
          </div>
          <h1 class="rv-title">
            <asp:Literal ID="PageTitleLiteral" runat="server" />
          </h1>
          <p class="rv-sub">
            <asp:Literal ID="SummaryLiteral" runat="server" />
          </p>
        </div>
        <div class="rv-course-chip">
          <span>Viewing as</span>
          <span class="label">
            <asp:Literal ID="HelperName" runat="server" />
          </span>
        </div>
      </div>

      <asp:Panel ID="FrameWrapper" runat="server" CssClass="rv-frame-shell">
        <div class="rv-toolbar">
          <span>If the resource does not load here, you can open it in a separate tab.</span>
          <asp:HyperLink ID="FallbackLink" runat="server" CssClass="rv-fallback-link" Target="_blank">
            Open in new tab
          </asp:HyperLink>
        </div>
        <iframe id="ResourceFrame" runat="server" class="rv-frame"></iframe>
      </asp:Panel>

      <p class="rv-message">
        <asp:Literal ID="ErrorLiteral" runat="server" />
      </p>

    </div>
  </form>
</body>
</html>

