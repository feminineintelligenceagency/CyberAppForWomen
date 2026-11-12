<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Replacements.aspx.cs" Inherits="CyberApp_FIA.Participant.Replacements" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Alternate Times</title>
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
      --pill-bg:#f6f7fb;
    }
    *{ box-sizing:border-box; }
    body{ margin:0; font-family:Lato, Arial, sans-serif; color:var(--ink); background:var(--page-grad); }
    .wrap{ min-height:100vh; padding:24px; max-width:900px; margin:0 auto; }

    .brand{ display:flex; align-items:center; gap:10px; margin-bottom:10px; }
    .badge{ width:38px; height:38px; border-radius:12px; background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue)); display:grid; place-items:center; color:#fff; font-family:Poppins; }

    h1{ font-family:Poppins; margin:0 0 6px 0; font-size:1.28rem; }
    .sub{ color:var(--muted); margin:0 0 18px 0; }

    .note{
      background:#f0f7fd; border:1px solid #d9e9f6;
      border-radius:12px; padding:12px; color:#0f3d5e; font-size:.95rem; margin-bottom:12px;
    }

    .grid{ display:grid; grid-template-columns:1fr 1fr; gap:16px; }
    @media (max-width: 760px){ .grid{ grid-template-columns:1fr; } }

    .card{
      background:var(--card-bg);
      border:1px solid var(--card-border);
      border-radius:18px; padding:16px;
      box-shadow:0 10px 30px rgba(42,153,219,.08);
      display:flex; flex-direction:column; gap:8px;
    }
    .title{ font-family:Poppins; font-weight:600; margin:0; font-size:1.02rem; }
    .meta{ color:var(--muted); font-size:.92rem; }
    .cta{ display:flex; gap:8px; margin-top:6px; }
    .btn{
      appearance:none; border:none; cursor:pointer;
      padding:10px 12px; border-radius:12px; font-weight:600; font-family:Poppins;
      box-shadow:0 2px 0 rgba(0,0,0,.04);
    }
    .btn-primary{ background:linear-gradient(135deg,var(--fia-blue),#6bc1f1); color:#fff; }
    .btn-ghost{ background:#fff; color:var(--fia-blue); border:1px solid #d9e9f6; }
    .btn:focus{ outline:none; box-shadow:0 0 0 4px var(--ring); }

    .topbar{ display:flex; align-items:center; justify-content:space-between; margin-bottom:14px; }
    .link{ color:var(--fia-blue); text-decoration:none; border-bottom:1px dashed #cfe8fb; padding-bottom:1px; }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="brand">
        <div class="badge">FIA</div>
        <div>
          <h1>Alternate Times</h1>
          <div class="sub">
            We found other times for <strong><asp:Literal ID="CourseTitle" runat="server" /></strong> that won’t overlap with your current schedule.
          </div>
        </div>
      </div>

      <div class="topbar">
        <div class="note">
          Original session time: <asp:Literal ID="OriginalTime" runat="server" />
        </div>
        <a id="CancelLink" runat="server" class="link">Cancel &amp; go back</a>
      </div>

      <asp:PlaceHolder ID="EmptyPH" runat="server" Visible="false">
        <div class="note">No non-overlapping alternatives are available right now. Please check again later.</div>
      </asp:PlaceHolder>

      <asp:Repeater ID="AltRepeater" runat="server" OnItemCommand="AltRepeater_ItemCommand">
        <HeaderTemplate><div class="grid"></HeaderTemplate>
        <ItemTemplate>
          <div class="card">
            <div class="title"><%# Eval("title") %></div>
            <div class="meta">
              <%# Eval("startLocal","{0:ddd, MMM d • h:mm tt}") %> – <%# Eval("endLocal","{0:h:mm tt}") %><br />
              Helper: <%# Eval("helper") %> • Seats remaining: <%# Eval("remaining") %> (<%# Eval("capacity") %>)
            </div>
            <div class="cta">
              <asp:Button ID="EnrollAlt" runat="server" CssClass="btn btn-primary" Text="Enroll in this time"
                          CommandName="enroll_alt" CommandArgument='<%# Eval("sessionId") %>' />
            </div>
          </div>
        </ItemTemplate>
        <FooterTemplate></div></FooterTemplate>
      </asp:Repeater>

      <asp:Label ID="FormMessage" runat="server" EnableViewState="false" />
    </div>
  </form>
</body>
</html>

