<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EnrollSuccess.aspx.cs" Inherits="CyberApp_FIA.Participant.EnrollSuccess" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Enrollment Confirmed</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    :root{
      --fia-pink:#f06aa9; --fia-blue:#2a99db; --fia-teal:#45c3b3;
      --ink:#1c1c1c; --muted:#6b7280; --ring:rgba(42,153,219,.25);
      --card-border:#e8eef7; --card-bg:#ffffff; --page-grad:linear-gradient(135deg,#ffffff,#f9fbff);
    }
    *{ box-sizing:border-box; } html,body{ height:100%; }
    body{ margin:0; font-family:Lato, Arial, sans-serif; color:var(--ink); background:var(--page-grad); }
    .wrap{ min-height:100vh; display:grid; place-items:center; padding:24px; }
    .card{
      width:min(780px, 94vw);
      background:var(--card-bg); border:1px solid var(--card-border); border-radius:24px;
      box-shadow:0 18px 50px rgba(42,153,219,.12); padding:28px;
    }
    .header{ display:flex; align-items:center; gap:14px; margin-bottom:14px; }
    .mark{
      width:54px;height:54px;border-radius:16px;display:grid;place-items:center;
      background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue)); color:#fff; font-family:Poppins; font-size:1.1rem;
      box-shadow:0 12px 36px rgba(240,106,169,.25); flex:0 0 54px;
    }
    h1{ font-family:Poppins; font-weight:600; font-size:1.3rem; margin:0; }
    .sub{ color:var(--muted); margin:2px 0 18px 0; }

    /* Layout: details on left, actions on right */
    .grid{
      display:grid; grid-template-columns: 1fr 300px; gap:18px;
      align-items:start;
    }
    @media (max-width: 760px){ .grid{ grid-template-columns: 1fr; } }
        /* Sprint 4 (#15): fit small phones */
    .grid > *{ min-width:0; }
    @media (max-width: 480px){
      .wrap{ padding:14px; }
      .card{ padding:18px; }
      .row{ flex-wrap:wrap; gap:2px 10px; }
      .k{ width:auto; min-width:0; }
    }

    .panel{
      background:#fff; border:1px dashed #e7edf7; border-radius:16px; padding:16px;
    }

    .info{ display:grid; gap:10px; }
    .row{ display:flex; align-items:center; gap:10px; }
    .k{ font-family:Poppins; font-weight:600; width:120px; min-width:120px; color:#38556b; }
    .v{ color:#0f172a; }

    .actions{ display:flex; flex-direction:column; gap:12px; }
    .badge{ align-self:flex-start; display:inline-block; padding:6px 10px; border-radius:999px; background:#f0f7fd; border:1px solid #d9e9f6; color:#2a99db; font-weight:600; font-family:Poppins; }

    .btn{
      display:inline-flex; align-items:center; justify-content:center; gap:8px; text-decoration:none;
      padding:12px 14px; border-radius:14px; font-weight:600; font-family:Poppins;
      border:1px solid transparent; cursor:pointer; width:100%;
      box-shadow:0 2px 0 rgba(0,0,0,.05);
    }
    .btn-primary{ background:linear-gradient(135deg,var(--fia-blue),#6bc1f1); color:#fff; }
    .btn-ghost{ background:#fff; color:var(--fia-blue); border-color:#d9e9f6; }
    .btn:focus{ outline:none; box-shadow:0 0 0 4px var(--ring); }

    .hr{
      height:1px; border:none; margin:18px 0;
      background:linear-gradient(90deg, rgba(42,153,219,.0), rgba(42,153,219,.25), rgba(42,153,219,.0));
    }
    .footnote{ color:var(--muted); margin-top:8px; }
  </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>
<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="card">
        <div class="header">
          <div class="mark">FIA</div>
          <div>
            <h1>Enrollment confirmed</h1>
            <p class="sub">You’re all set. We’ve saved your spot and prepared a calendar file.</p>
          </div>
        </div>

        <div class="grid">
          <!-- Details -->
          <div class="panel info">
            <div class="row"><div class="k">Course</div><div class="v"><asp:Literal ID="LitCourseTitle" runat="server" /></div></div>
            <div class="row"><div class="k">When</div><div class="v"><asp:Literal ID="LitWhen" runat="server" /></div></div>
            <div class="row"><div class="k">Location</div><div class="v"><asp:Literal ID="LitLocation" runat="server" /></div></div>
            <div class="row"><div class="k">Helper</div><div class="v"><asp:Literal ID="LitHelper" runat="server" /></div></div>
          </div>

          <!-- Actions -->
          <div class="panel actions">
            <span class="badge">Next step</span>
            <asp:HyperLink ID="LnkIcs" runat="server" CssClass="btn btn-primary" Text="Download .ics" Target="_blank" />
            <asp:HyperLink ID="LnkHome" runat="server" NavigateUrl="~/Account/Participant/Home.aspx" CssClass="btn btn-ghost" Text="Back to Home" />
          </div>
        </div>

        <hr class="hr" />
        <p class="footnote">Tip: If the time looks off in your calendar, check your device time zone and ensure it’s set to update automatically.</p>
      </div>
    </div>
  </form>
</body>
</html>


