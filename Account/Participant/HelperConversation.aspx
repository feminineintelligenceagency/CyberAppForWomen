<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="HelperConversation.aspx.cs" Inherits="CyberApp_FIA.Participant.HelperConversation" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Helper Conversation</title>
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

    .conv-header{
      margin-bottom:14px;
    }
    .conv-topic{
      font-family:Poppins;
      font-size:1.1rem;
      margin:0 0 4px 0;
    }
    .conv-meta{
      font-size:.9rem;
      color:var(--muted);
    }

    .messages-wrap{
      max-height:420px;
      overflow-y:auto;
      padding:6px 2px 10px 2px;
      border-radius:14px;
      background:linear-gradient(180deg,#f7fbff,#ffffff);
      border:1px solid #e0e9f5;
      margin-bottom:14px;
    }

    .msg{
      max-width:88%;
      margin:8px 10px;
      padding:9px 11px;
      border-radius:16px;
      font-size:.93rem;
      line-height:1.4;
      box-shadow:0 4px 12px rgba(0,0,0,.04);
    }
    .msg-me{
      margin-left:auto;
      background:linear-gradient(135deg,#e6fbf7,#d6f5ef);
      border:1px solid rgba(69,195,179,.35);
    }
    .msg-them{
      margin-right:auto;
      background:#ffffff;
      border:1px solid #e4e8f5;
    }

    .msg-meta{
      font-size:.78rem;
      color:var(--muted);
      margin-bottom:3px;
    }
    .msg-sender{ font-weight:600; }
    .msg-time{ margin-left:6px; }

    .msg-body{ white-space:pre-wrap; }

    .field{ margin-top:6px; display:flex; flex-direction:column; gap:6px; }
    .field label{
      font-size:.9rem;
      color:var(--fia-blue);
      font-weight:600;
      padding-left:4px;
    }

    .textarea{
      width:100%;
      min-height:80px;
      border-radius:12px;
      border:1px solid #d9e9f6;
      padding:9px 11px;
      font-size:.93rem;
      font-family:Lato, Arial, sans-serif;
      background:linear-gradient(180deg,#f0f7fd,#ffffff);
      color:var(--ink);
      outline:none;
      resize:vertical;
    }
    .textarea:focus{
      border-color:#bfe6ff;
      box-shadow:0 0 0 4px var(--ring);
      background:#fff;
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
      margin-top:10px;
      font-size:.9rem;
    }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <asp:ScriptManager runat="server" />
    <asp:UpdatePanel runat="server">
    <ContentTemplate>
    <div class="wrap">
      <div class="brand">
        <div class="badge">FIA</div>
        <div>
          <h1>Helper conversation</h1>
          <p class="sub">
            <a href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>" class="back-link">← Back to Your Cyberfair</a>
          </p>
        </div>
      </div>

      <div class="card">
        <div class="conv-header">
          <h2 class="conv-topic"><asp:Literal ID="TopicLiteral" runat="server" /></h2>
          <p class="conv-meta">
            With Helper:
            <strong><asp:Literal ID="HelperNameLiteral" runat="server" /></strong>
          </p>
        </div>

        <div class="messages-wrap">
          <asp:Repeater ID="MessagesRepeater" runat="server">
            <ItemTemplate>
              <div class='<%# Eval("CssClass") %>'>
                <div class="msg-meta">
                  <span class="msg-sender"><%# Eval("SenderName") %></span>
                  <span class="msg-time"><%# Eval("TimeLocal", "{0:MMM d, h:mm tt}") %></span>
                </div>
                <div class="msg-body"><%# Eval("Body") %></div>
              </div>
            </ItemTemplate>
          </asp:Repeater>
        </div>

        <div class="field">
          <label for="ReplyBody">Send a new message</label>
          <asp:TextBox ID="ReplyBody" runat="server" CssClass="textarea" TextMode="MultiLine" Rows="4" />
        </div>

        <div class="cta-row">
          <asp:Button ID="SendReplyButton"
                      runat="server"
                      CssClass="btn btn-primary"
                      Text="Send"
                      OnClick="SendReplyButton_Click" />
        </div>

        <asp:Label ID="FormMessage" runat="server" CssClass="form-message" EnableViewState="false" />
      </div>
    </div>
 </ContentTemplate>
    </asp:UpdatePanel>
  </form>
</body>
</html>
