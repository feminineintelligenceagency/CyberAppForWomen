<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Notifications.aspx.cs" Inherits="CyberApp_FIA.Participant.Notifications" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Inbox</title>
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
      --page-grad:linear-gradient(135deg,#ffffff,#f9fbff);
    }
    *{ box-sizing:border-box; }
    body{ margin:0; font-family:Lato, Arial, sans-serif; color:var(--ink); background:var(--page-grad); }
    .wrap{ min-height:100vh; padding:24px 16px; max-width:720px; margin:0 auto; }
    h1{ font-family:Poppins; margin:0 0 6px 0; font-size:1.35rem; }
    .sub{ color:var(--muted); margin:0 0 18px 0; }
    .pill{ display:inline-block; padding:6px 10px; border-radius:999px; background:#f0f7fd;
           border:1px solid #d9e9f6; color:var(--fia-blue); text-decoration:none; font-size:.9rem; margin-bottom:16px; }
    .toolbar{ display:flex; justify-content:space-between; align-items:center; gap:10px; margin-bottom:12px; flex-wrap:wrap; }
    .item{ background:#fff; border:1px solid var(--card-border); border-radius:14px; padding:14px 16px; margin-bottom:10px;
           display:flex; gap:12px; align-items:flex-start; }
    .item.unread{ border-left:4px solid var(--fia-blue); }
    .dot{ width:10px; height:10px; border-radius:50%; background:var(--fia-blue); margin-top:6px; flex:0 0 auto; }
    .item.read .dot{ background:transparent; }
    .body{ flex:1; min-width:0; }
    .title{ font-family:Poppins; font-size:1rem; margin:0 0 4px 0; }
    .msg{ margin:0 0 6px 0; overflow-wrap:anywhere; }
    .when{ color:var(--muted); font-size:.85rem; }
    .actions{ display:flex; gap:8px; flex-wrap:wrap; margin-top:8px; }
    .btn{ border:0; border-radius:10px; padding:8px 12px; font-size:.9rem; cursor:pointer; text-decoration:none; display:inline-block; }
    .btn-primary{ color:#fff; background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal)); }
    .btn-plain{ background:#f3f4f6; color:var(--ink); }
    .empty{ background:#fff; border:1px solid var(--card-border); border-radius:14px; padding:18px; color:var(--muted); text-align:center; }
  </style>
</head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <a class="pill" href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>">&larr; Back to home</a>

      <div class="toolbar">
        <div>
          <h1>Inbox</h1>
          <p class="sub" style="margin:0;">Reminders and updates for your account.</p>
        </div>
        <asp:Button ID="MarkAllBtn" runat="server" Text="Mark all as read"
                    CssClass="btn btn-plain" OnClick="MarkAllBtn_Click" CausesValidation="false" />
      </div>

      <asp:Repeater ID="NotificationsRepeater" runat="server" OnItemCommand="NotificationsRepeater_ItemCommand">
        <ItemTemplate>
          <div class='<%# (bool)Eval("IsRead") ? "item read" : "item unread" %>'>
            <span class="dot"></span>
            <div class="body">
              <p class="title"><%#: Eval("Title") %></p>
              <p class="msg"><%#: Eval("Message") %></p>
              <span class="when"><%#: Eval("When") %></span>

              <div class="actions">
                <asp:LinkButton runat="server" CssClass="btn btn-primary" CausesValidation="false"
                                CommandName="open" CommandArgument='<%# Eval("Id") %>'
                                Visible='<%# (bool)Eval("HasLink") %>'>Open</asp:LinkButton>
                <asp:LinkButton runat="server" CssClass="btn btn-plain" CausesValidation="false"
                                CommandName="read" CommandArgument='<%# Eval("Id") %>'
                                Visible='<%# !(bool)Eval("IsRead") %>'>Mark as read</asp:LinkButton>
              </div>
            </div>
          </div>
        </ItemTemplate>
      </asp:Repeater>

      <asp:Panel ID="EmptyPanel" runat="server" Visible="false" CssClass="empty">
        You're all caught up. You don't have any notifications.
      </asp:Panel>

      <p class="sub" style="margin-top:16px;">
        You can change how often you get reminders in
        <a href="<%: ResolveUrl("~/Account/Participant/Settings.aspx") %>">Settings</a>.
      </p>
    </div>
  </form>
</body>
</html>