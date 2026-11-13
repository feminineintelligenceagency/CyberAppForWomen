<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SwapSession.aspx.cs" Inherits="CyberApp_FIA.Participant.SwapSession" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Swap Session</title>
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
    body{ margin:0; font-family:Lato, Arial, sans-serif; color:var(--ink); background:var(--page-grad); }
    .wrap{ min-height:100vh; padding:24px; max-width:950px; margin:0 auto; }

    .brand{ display:flex; align-items:center; gap:10px; margin-bottom:10px; }
    .badge{
      width:40px; height:40px; border-radius:12px;
      background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue));
      display:grid; place-items:center; color:#fff; font-family:Poppins;
    }

    h1{ font-family:Poppins; margin:0 0 6px 0; font-size:1.3rem; }
    .sub{ color:var(--muted); margin:0 0 16px 0; }

    .topbar{ display:flex; align-items:flex-start; justify-content:space-between; gap:12px; margin-bottom:14px; }
    @media (max-width: 760px){ .topbar{ flex-direction:column; } }

    .note{
      background:#f0f7fd; border:1px solid #d9e9f6;
      border-radius:12px; padding:12px; color:#0f3d5e; font-size:.95rem;
    }

    .important-note{
      background:linear-gradient(135deg,#fff7fb,#f7fbff);
      border:1px solid #f3d7e4;
      border-radius:14px;
      padding:12px 14px;
      color:#5d2a47;
      font-size:.94rem;
      margin-bottom:14px;
    }

    .grid{ display:grid; grid-template-columns:1fr 1fr; gap:16px; }
    @media (max-width: 760px){ .grid{ grid-template-columns:1fr; } }

    .card{
      background:var(--card-bg);
      border:1px solid var(--card-border);
      border-radius:18px;
      padding:16px;
      box-shadow:0 10px 30px rgba(42,153,219,.08);
      display:flex;
      flex-direction:column;
      gap:8px;
      position:relative;
      overflow:hidden;
    }

    .card::before{
      content:"";
      position:absolute;
      inset:0 0 0 auto;
      width:6px;
      left:0;
      right:auto;
      background:linear-gradient(180deg, rgba(42,153,219,.85), rgba(240,106,169,.85));
      opacity:.9;
    }

    .title{ font-family:Poppins; font-weight:600; margin:0; font-size:1.02rem; }
    .meta{ color:var(--muted); font-size:.92rem; line-height:1.5; }

    .cta{ display:flex; gap:8px; margin-top:8px; }

    .btn{
      appearance:none; border:none; cursor:pointer;
      padding:10px 12px; border-radius:12px; font-weight:600; font-family:Poppins;
      box-shadow:0 2px 0 rgba(0,0,0,.04);
      text-decoration:none;
      display:inline-block;
    }

    .btn-primary{ background:linear-gradient(135deg,var(--fia-blue),#6bc1f1); color:#fff; }
    .btn-ghost{ background:#fff; color:var(--fia-blue); border:1px solid #d9e9f6; }
    .btn:focus{ outline:none; box-shadow:0 0 0 4px var(--ring); }

    .link{ color:var(--fia-blue); text-decoration:none; border-bottom:1px dashed #cfe8fb; padding-bottom:1px; }
    .message{ margin-top:16px; }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="brand">
        <div class="badge">FIA</div>
        <div>
          <h1>Swap Session</h1>
          <div class="sub">
            Choose another time for <strong><asp:Literal ID="CourseTitle" runat="server" /></strong> that does not overlap with your other enrolled sessions.
          </div>
        </div>
      </div>

      <div class="topbar">
        <div class="note">
          Current session time: <asp:Literal ID="OriginalTime" runat="server" />
        </div>
        <a id="CancelLink" runat="server" class="link">Cancel &amp; go back</a>
      </div>

      <div class="important-note">
        When you choose a new time, FIA will attempt the move in one step. If the new session is no longer available, your current seat stays exactly as it is.
      </div>

      <asp:PlaceHolder ID="EmptyPH" runat="server" Visible="false">
        <div class="note">No non-overlapping swap options are available right now. Please check again later.</div>
      </asp:PlaceHolder>

      <asp:Repeater ID="SwapRepeater" runat="server" OnItemCommand="SwapRepeater_ItemCommand">
        <HeaderTemplate><div class="grid"></HeaderTemplate>
        <ItemTemplate>
          <div class="card">
            <div class="title"><%# Eval("title") %></div>
            <div class="meta">
              <%# Eval("startLocal","{0:ddd, MMM d • h:mm tt}") %> – <%# Eval("endLocal","{0:h:mm tt}") %><br />
              Helper: <%# Eval("helper") %> • Seats remaining: <%# Eval("remaining") %> (<%# Eval("capacity") %>)
            </div>
            <div class="cta">
              <asp:Button ID="SwapToBtn" runat="server"
                          CssClass="btn btn-primary"
                          Text="Swap into this time"
                          CommandName="swap_to"
                          CommandArgument='<%# Eval("sessionId") %>' />
            </div>
          </div>
        </ItemTemplate>
        <FooterTemplate></div></FooterTemplate>
      </asp:Repeater>

      <div class="message">
        <asp:Label ID="FormMessage" runat="server" EnableViewState="false" />
      </div>
    </div>
  </form>
</body>
</html>
