<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="CyberApp_FIA.Participant.Settings" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Account Settings</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9;
      --fia-blue:#2a99db;
      --ink:#1c1c1c;
      --muted:#6b7280;
      --card-border:#e8eef7;
      --danger:#b91c1c;
      --danger-bg:#fef2f2;
      --page-grad:linear-gradient(135deg,#ffffff,#f9fbff);
    }
    *{ box-sizing:border-box; }
    body{ margin:0; font-family:Lato, Arial, sans-serif; color:var(--ink); background:var(--page-grad); }
    .wrap{ min-height:100vh; padding:24px 16px; max-width:640px; margin:0 auto; }
    h1{ font-family:Poppins; margin:0 0 6px 0; font-size:1.35rem; }
    h2{ font-family:Poppins; margin:0 0 8px 0; font-size:1.05rem; }
    .sub{ color:var(--muted); margin:0 0 18px 0; }
    .pill{ display:inline-block; padding:6px 10px; border-radius:999px; background:#f0f7fd;
           border:1px solid #d9e9f6; color:var(--fia-blue); text-decoration:none; font-size:.9rem; margin-bottom:16px; }
    .card{ background:#fff; border:1px solid var(--card-border); border-radius:14px; padding:18px; margin-bottom:16px; }
    .card.danger{ border-color:#fecaca; background:var(--danger-bg); }
    ul{ margin:6px 0 12px 18px; padding:0; }
    label{ display:block; font-weight:400; margin:10px 0 6px; }
    .input{ width:100%; padding:10px; border:1px solid #d1d5db; border-radius:10px; font-size:1rem; }
    .check{ display:flex; gap:8px; align-items:flex-start; margin:12px 0; }
    .btn{ border:0; border-radius:10px; padding:10px 16px; font-size:1rem; cursor:pointer; }
    .btn-danger{ background:var(--danger); color:#fff; }
    .btn-plain{ background:#f3f4f6; color:var(--ink); text-decoration:none; display:inline-block; }
    .msg{ margin-top:12px; color:var(--danger); }
    .msg.ok{ color:#0a7a3c; }
    .btn-primary{ background:linear-gradient(135deg,var(--fia-blue),#45c3b3); color:#fff; }
    .done{ text-align:center; }
  </style>
</head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <%-- Shown before deletion --%>
      <asp:Panel ID="SettingsPanel" runat="server">
        <a class="pill" href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>">&larr; Back to home</a>
        <h1>Account settings</h1>
        <p class="sub">Signed in as <asp:Literal ID="EmailLiteral" runat="server" /></p>

                <%-- Sprint 3 (#21): course reminder settings --%>
        <div class="card">
          <h2>Course reminders</h2>
          <p class="sub" style="margin-bottom:6px;">Get a reminder in your inbox when you have courses you haven't finished.</p>

          <label for="ReminderFrequency">How often</label>
          <asp:DropDownList ID="ReminderFrequency" runat="server" CssClass="input">
            <asp:ListItem Value="Daily">Daily</asp:ListItem>
            <asp:ListItem Value="Weekly">Weekly</asp:ListItem>
            <asp:ListItem Value="Off">Off</asp:ListItem>
          </asp:DropDownList>

          <div class="check">
            <asp:CheckBox ID="EmailReminders" runat="server" />
            <label for="EmailReminders" style="margin:0;">Also email me a reminder. Emails use a plain subject line and don't mention this site.</label>
          </div>

          <asp:Button ID="BtnSaveReminders" runat="server" Text="Save reminder settings"
                      CssClass="btn btn-primary" OnClick="BtnSaveReminders_Click" CausesValidation="false" />

          <div class="msg ok"><asp:Literal ID="ReminderMessage" runat="server" EnableViewState="false" /></div>
        </div>
        <div class="card danger">
          <h2>Delete my account</h2>
          <p>This permanently removes:</p>
          <ul>
            <li>Your account and sign-in details</li>
            <li>Your quiz answers and results</li>
            <li>Your course progress and badges</li>
            <li>Your session sign-ups</li>
            <li>Your messages with helpers</li>
            <li>Your notifications and reminder settings</li>
          </ul>
          <p><strong>This cannot be undone.</strong></p>

          <%-- Confirmation step 1: re-enter password --%>
          <label for="ConfirmPassword">Enter your password to confirm</label>
          <asp:TextBox ID="ConfirmPassword" runat="server" TextMode="Password" CssClass="input"
                       autocomplete="current-password" />

          <%-- Confirmation step 2: explicit checkbox --%>
          <div class="check">
            <asp:CheckBox ID="ConfirmCheck" runat="server" />
            <label for="ConfirmCheck" style="margin:0;">I understand my account and data will be permanently deleted.</label>
          </div>

          <%-- Confirmation step 3: browser "are you sure?" popup --%>
          <asp:Button ID="BtnDelete" runat="server" Text="Delete my account"
                      CssClass="btn btn-danger" OnClick="BtnDelete_Click"
                      OnClientClick="return confirm('Permanently delete your account? This cannot be undone.');" />

          <div class="msg"><asp:Literal ID="FormMessage" runat="server" /></div>
        </div>
      </asp:Panel>

      <%-- Shown after deletion --%>
      <asp:Panel ID="DonePanel" runat="server" Visible="false" CssClass="card done">
        <h1>Your account has been deleted</h1>
        <p class="sub">All of your data has been removed.</p>
        <a class="btn btn-plain" href="<%: ResolveUrl("~/Welcome_Page.aspx") %>">Return to the home page</a>
      </asp:Panel>

    </div>
  </form>
</body>
</html>