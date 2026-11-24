<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ParticipantScoreWidget.ascx.cs" Inherits="CyberApp_FIA.Account.Participant.ParticipantScoreWidget" %>
<style>
  /* --- FIA tokens --- */
  :root{
    --fia-pink:#f06aa9;
    --fia-blue:#2a99db;
    --fia-teal:#45c3b3;
    --ink:#1c1c1c;
    --muted:#6b7280;
    --ring:rgba(42,153,219,.25);
  }

  /* --- Card shell --- */
  .scoreCard{
    background:#fff;
    border-radius:16px;
    box-shadow:0 8px 24px rgba(0,0,0,.08);
    padding:16px;
    margin:12px 0;
    border:1px solid #eaf1fb;
  }

  .row{display:flex;align-items:center;gap:12px}
  .title{font-weight:700;letter-spacing:.2px}
  .pill{
    margin-left:auto;
    background:linear-gradient(90deg,var(--fia-blue),var(--fia-pink));
    color:#fff;border-radius:999px;padding:4px 10px;font-size:.85rem;font-weight:700
  }
  .mini{color:var(--muted);font-size:.9rem;margin-top:6px}

  /* Overall pill bands (optional visual hint) */
  .pill.low{background:var(--fia-teal)}
  .pill.med{background:var(--fia-blue)}
  .pill.high{background:var(--fia-pink)}

  /* --- Description (what scores mean) --- */
  .desc{
    margin-top:6px;
    padding:10px 12px;
    border-radius:12px;
    background:linear-gradient(90deg,rgba(42,153,219,.06),rgba(240,106,169,.06));
    border:1px solid #e3f0fb;
    font-size:.92rem;
    color:#1f2937;
  }

  /* --- Grid of modules --- */
  .grid{
    display:grid;
    grid-template-columns:repeat(auto-fit,minmax(200px,1fr));
    gap:10px;
    margin-top:10px
  }
  .cell{
    position:relative;
    background:#f7fbff;
    border:1px solid #e3f0fb;
    border-radius:12px;
    padding:10px 12px 12px 12px;
    display:flex;align-items:center;gap:8px;justify-content:space-between
  }
  .cell .name{font-weight:600}
  .chip{
    padding:4px 8px;border-radius:999px;font-size:.85rem;font-weight:700;color:#fff;min-width:38px;text-align:center
  }

  /* Priority bands for cells/chips */
  .cell.low{background:linear-gradient(0deg,#f6fffd,#f6fffd);border-color:#cdf3ea}
  .chip.low{background:var(--fia-teal)}

  .cell.med{background:linear-gradient(0deg,#f6fbff,#f6fbff);border-color:#cfe7fb}
  .chip.med{background:var(--fia-blue)}

  .cell.high{background:linear-gradient(0deg,#fff7fb,#fff7fb);border-color:#f9d2e4}
  .chip.high{background:var(--fia-pink)}

  .actions{margin-top:10px;display:flex;gap:8px;align-items:center;flex-wrap:wrap}
  .btnTiny{padding:6px 10px;border:0;border-radius:10px;background:var(--fia-blue);color:#fff;cursor:pointer;font-weight:700}
  .btnTiny:disabled{opacity:.6;cursor:not-allowed}

  .cellLeft{
  display:flex;
  flex-direction:column;
  gap:6px;
  min-width:0;
}

.doneBadge{
  display:inline-flex;
  align-items:center;
  width:max-content;
  padding:3px 8px;
  border-radius:999px;
  font-size:.78rem;
  font-weight:700;
  color:#0a5b4e;
  background:linear-gradient(180deg,#e6fbf7,#ffffff);
  border:1px solid #bfeee6;
}
</style>

<div class="scoreCard">
  <div class="row">
    <span class="title">Cybersecurity Score</span>
    <span id="PillScore" runat="server" class="pill">–</span>
  </div>

  <!-- One–two sentence explainer -->
  <div class="desc">
    Your overall and module scores range from <strong>0–10</strong>. Higher numbers mean a higher priority for help in that area (you’ll see those first in your plan). Lower numbers mean you’re relatively covered and can be scheduled later.
  </div>

  <div class="mini">Private to you by default.</div>

  <!-- Cleaner, even grid with priority color-bands -->
  <div class="grid">
    <asp:Repeater ID="RptDomainMini" runat="server" OnItemDataBound="RptDomainMini_ItemDataBound">
      <ItemTemplate>
  <div id="Cell" runat="server" class="cell">
    <div class="cellLeft">
      <span class="name"><%#: Eval("Key") %></span>
      <span id="CompletedBadge" runat="server" class="doneBadge" visible="false">Completed</span>
    </div>
    <span id="Chip" runat="server" class="chip">–</span>
  </div>
</ItemTemplate>
    </asp:Repeater>
  </div>

  <div class="actions">
  </div>
</div>

