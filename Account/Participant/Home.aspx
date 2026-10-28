<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="CyberApp_FIA.Participant.Home" MaintainScrollPositionOnPostBack="true" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Your Cyberfair</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />

  <!-- Fonts -->
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <style>
    /* ---------- Design tokens (brand) ---------- */
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

      /* card accents */
      --rail-grad:linear-gradient(180deg, rgba(42,153,219,.85), rgba(240,106,169,.85));
      --note-blue:#eaf5ff;
      --note-pink:#fff3f9;
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

    /* ---------- Header ---------- */
    .brand{ display:flex; align-items:center; gap:10px; margin-bottom:10px; }
    .badge{
      width:42px; height:42px; border-radius:12px;
      background:linear-gradient(135deg, var(--fia-pink), var(--fia-blue));
      display:grid; place-items:center; color:#fff; font-family:Poppins;
    }
    h1{ font-family:Poppins; margin:0 0 6px 0; font-size:1.35rem; }
    .sub{ color:var(--muted); margin:0 0 16px 0; }

    /* Base pill + remove underline for anchors */
    .pill{
      display:inline-block;
      padding:6px 10px;
      border-radius:999px;
      background:var(--pill-bg);
      border:1px solid var(--card-border);
      margin-right:8px;
      font-size:.9rem;
      text-decoration:none;
      color:inherit;
    }
    .pill-link{ text-decoration:none; color:var(--fia-blue); border-color:#d9e9f6; background:#f0f7fd; }

    .subchips{ display:flex; align-items:center; gap:8px; flex-wrap:wrap; }
    .pill-push{ margin-left:auto; }

    /* ---------- Note ---------- */
    .note{
      background:var(--pill-bg); border:1px solid var(--card-border);
      border-radius:12px; padding:12px; color:var(--muted);
      font-size:.95rem; margin-bottom:12px;
    }

    .security-panel{
      background:linear-gradient(180deg,#ffffff,#f9fbff);
      border:1px solid var(--card-border);
      border-radius:20px;
      box-shadow:0 12px 28px rgba(42,153,219,.08);
      padding:16px 18px;
    }

    .security-header{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:16px;
      margin-bottom:10px;
    }

    .security-meta{
      color:var(--muted);
      font-size:.9rem;
    }

    .security-status{
      margin-bottom:12px;
      padding:8px 12px;
      border-radius:12px;
      background:linear-gradient(180deg,#f3f8ff,#ffffff);
      border:1px solid #d9e9f6;
      color:#0f3d5e;
      font-size:.9rem;
    }

    .security-list{
      display:flex;
      flex-direction:column;
      gap:12px;
    }

    .security-item{
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:12px;
      border:1px solid #e7eef8;
      border-radius:14px;
      background:#fff;
      padding:12px 14px;
    }

    .security-device{
      display:flex;
      align-items:center;
      gap:12px;
      min-width:0;
      flex:1;
    }

    .session-dot{
      width:11px;
      height:11px;
      border-radius:50%;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      box-shadow:0 0 0 4px rgba(42,153,219,.12);
      flex-shrink:0;
    }

    .session-title{
      font-family:Poppins;
      font-weight:600;
      font-size:.95rem;
      color:var(--ink);
      margin:0 0 4px 0;
    }

    .session-meta{
      color:var(--muted);
      font-size:.82rem;
      line-height:1.45;
    }

    .security-item-actions{
      display:flex;
      align-items:center;
      gap:8px;
      flex-shrink:0;
    }

    .session-chip{
      display:inline-flex;
      align-items:center;
      padding:6px 10px;
      border-radius:999px;
      background:linear-gradient(180deg,#f5fbff,#eef8ff);
      border:1px solid rgba(42,153,219,.25);
      color:var(--fia-blue);
      font-size:.8rem;
      font-weight:700;
    }

    @media (max-width: 640px){
      .security-header,
      .security-item{
        flex-direction:column;
        align-items:flex-start;
      }

      .security-item-actions{
        width:100%;
        justify-content:space-between;
      }
    }

    /* ---------- Sections / titles / dividers ---------- */
    .section{ margin:28px 0; }
    .section-title{
      font-family:Poppins; font-weight:600; font-size:1.1rem; margin:0 0 10px 0;
      display:flex; align-items:center; gap:10px; flex-wrap:wrap;
    }
    .divider{
      height:1px; border:none; margin:24px 0 8px 0;
      background:linear-gradient(90deg, rgba(42,153,219,.0), rgba(42,153,219,.25), rgba(42,153,219,.0));
    }

    /* ---------- Sessions Grid ---------- */
    .fia-sessions-grid{
      display:grid;
      grid-template-columns:repeat(3, minmax(0, 1fr)) !important;
      gap:18px;
      margin-top:10px;
      grid-auto-flow:row;
    }
    @media (max-width: 980px){
      .fia-sessions-grid{ grid-template-columns:repeat(2, minmax(0, 1fr)) !important; }
    }
    @media (max-width: 640px){
      .fia-sessions-grid{ grid-template-columns:1fr !important; }
    }

    /* ---------- Card (stacked layout) ---------- */
    .card{
      position:relative;
      background:var(--card-bg);
      border:1px solid var(--card-border);
      border-radius:20px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);
      padding:0;
      display:flex; flex-direction:column; gap:0;
      transition: transform .12s ease, box-shadow .12s ease, border-color .12s ease;
      width:100%;
      overflow:hidden;
    }
    .card::before{
      content:"";
      position:absolute; inset:0 0 0 auto;
      width:6px; left:0; right:auto;
      background:var(--rail-grad);
      opacity:.9;
    }
    .card:hover, .card:focus-within{
      transform: translateY(-2px);
      border-color:#d4e6f7;
      box-shadow:0 16px 40px rgba(42,153,219,.14);
    }

    .card-sec{ padding:14px 16px 12px 18px; }
    .card-sec + .card-sec{ border-top:1px solid #f0f3f9; }

    .title{ font-family:Poppins; font-weight:600; margin:0; font-size:1.06rem; line-height:1.35; }

    .line{ display:flex; align-items:center; justify-content:flex-start; gap:8px; font-size:.95rem; color:var(--muted); }
    .label{ font-weight:700; color:var(--fia-blue); font-size:.9rem; min-width:84px; }
    .value{ color:var(--ink); }

    .chip{
      display:inline-flex; align-items:center; gap:6px; padding:6px 10px; border-radius:999px; font-size:.85rem;
      background:#fff; border:1px dashed #e7edf7;
    }
    .helper-chip{ border:1px solid rgba(240,106,169,.25); background:linear-gradient(180deg,#fff,#fff7fb); }
    .dot{ width:8px; height:8px; border-radius:999px; background:var(--fia-pink); display:inline-block; }

    /* Force helper name to FIA blue everywhere */
    .helper-chip strong { 
      color: var(--fia-blue) !important;
    }

    .info-bar{
      display:flex; align-items:center; justify-content:space-between; gap:10px;
      padding:10px 12px; border-radius:12px; background:linear-gradient(180deg,#f7fbff,#ffffff);
      border:1px solid #d9e9f6; color:#0f3d5e; font-size:.92rem;
    }
    .info-bar.ok{ background:linear-gradient(180deg,#e6fbf7,#ffffff); border-color:#bfeee6; color:#0a5b4e; }
    .info-bar.warn{ background:var(--note-pink); border-color:#ffd1e5; color:#7a103a; }
    /* ---------- NEW: My Sessions queue position card ---------- */
.queue-card{
  border-radius:16px;
  border:1px solid rgba(42,153,219,.22);
  background:
    radial-gradient(circle at 0 0, rgba(240,106,169,.12), transparent 55%),
    radial-gradient(circle at 100% 100%, rgba(69,195,179,.16), transparent 55%),
    linear-gradient(180deg,#ffffff,#f7fbff);
  padding:12px;
  box-shadow:0 10px 24px rgba(42,153,219,.07);
}

.queue-card-top{
  display:flex;
  align-items:center;
  justify-content:space-between;
  gap:10px;
  margin-bottom:8px;
}

.queue-card-title{
  font-family:Poppins;
  font-weight:600;
  color:var(--fia-blue);
  font-size:.95rem;
}

.queue-position-pill{
  display:inline-flex;
  align-items:center;
  justify-content:center;
  padding:6px 10px;
  border-radius:999px;
  background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
  color:#fff;
  font-family:Poppins;
  font-size:.8rem;
  font-weight:600;
  white-space:nowrap;
}

.queue-card-text{
  color:#0f3d5e;
  font-size:.9rem;
  line-height:1.45;
}

.queue-card-text strong{
  color:#0f3d5e;
  font-family:Poppins;
}
/* Slight pink tone for waitlisted session queue card */
.waitlist-card{
  border-color:rgba(240,106,169,.26);
  background:
    radial-gradient(circle at 0 0, rgba(240,106,169,.14), transparent 55%),
    radial-gradient(circle at 100% 100%, rgba(42,153,219,.12), transparent 55%),
    linear-gradient(180deg,#ffffff,#fff8fc);
}

.waitlist-position-pill{
  background:linear-gradient(135deg,var(--fia-pink),#ff86bf);
}
    .remain{
      font-family:Poppins; font-weight:600;
      padding:6px 10px; border-radius:999px;
      background:linear-gradient(135deg,#e6fbf7,#d6f5ef);
      border:1px solid rgba(69,195,179,.35); color:#084c61;
    }
    .remain.low{ background:linear-gradient(135deg,#fff2f7,#ffe7f0); color:#7a103a; border-color:rgba(240,106,169,.45); }

    .subtle{ color:var(--muted); font-size:.92rem; }
    .cta-row{ display:flex; gap:8px; }

    .btn{
      appearance:none; border:none; cursor:pointer;
      padding:10px 12px; border-radius:12px; font-weight:600; font-family:Poppins;
      box-shadow:0 2px 0 rgba(0,0,0,.04);
    }
    .btn-primary{ background:linear-gradient(135deg,var(--fia-blue),#6bc1f1); color:#fff; }
    .btn-ghost{ background:#fff; color:var(--fia-blue); border:1px solid #d9e9f6; }
    .btn:focus{ outline:none; box-shadow:0 0 0 4px var(--ring); }

    .pill-pink{
      background: linear-gradient(135deg, var(--fia-pink), #ff86bf) !important;
      border-color: rgba(240,106,169,.55) !important;
      color: #fff !important;
    }

    /* NEW: Pink action button */
    .btn-pink{
      background: linear-gradient(135deg, var(--fia-pink), #ff86bf);
      color:#fff;
      border:1px solid rgba(240,106,169,.55);
    }

    .btn-swap{
  display:inline-block;
  appearance:none;
  border:none;
  cursor:pointer;
  padding:10px 12px;
  border-radius:12px;
  font-weight:600;
  font-family:Poppins;
  box-shadow:0 2px 0 rgba(0,0,0,.04);
  text-decoration:none;
  background:linear-gradient(135deg, var(--fia-blue), #6bc1f1);
  color:#fff;
  border:1px solid rgba(42,153,219,.45);
}

.btn-swap:hover,
.btn-swap:focus{
  text-decoration:none;
  color:#fff;
  box-shadow:0 0 0 4px var(--ring);
}

    /* ---------- Prereq / Conflict ---------- */
    .blocked-note{
      background:var(--note-pink); border:1px solid #ffd1e5; color:#7a103a;
      border-radius:12px; padding:10px 12px; font-size:.92rem;
    }
    .conflict-note{
      background:var(--note-blue);
      border:1px solid #d9e9f6;
      color:#0f3d5e;
      border-radius:12px; padding:10px 12px; font-size:.92rem;
    }
    .conflict-actions{ display:flex; gap:8px; margin-top:8px; }
    .card.is-blocked{ opacity:.98; }

    .glow-alt{
      box-shadow:0 0 0 3px rgba(42,153,219,.28), 0 0 20px rgba(42,153,219,.38);
      transition:box-shadow .15s ease, transform .12s ease;
    }
    .glow-alt:hover{ transform:translateY(-1px); }

    /* === Filters card (visuals only) === */
    .filters-card{
      margin:18px 0 8px 0;
      padding:16px;
      background:#fff;
      border:1px solid var(--card-border);
      border-radius:20px;
      box-shadow:0 10px 26px rgba(42,153,219,.06), 0 0 0 2px rgba(69,195,179,.06) inset;
    }
    .filters-title{
      font-family:Poppins; font-weight:600; font-size:1.05rem; margin:0 0 12px 2px;
      color: var(--fia-blue);
    }
    .filters-grid{ display:grid; grid-template-columns: 1.1fr 1.1fr auto auto; gap:12px; align-items:end; }
    .row-full{ grid-column: 1 / -1; }
    @media (max-width: 980px){ .filters-grid{ grid-template-columns: 1fr 1fr; } }
    @media (max-width: 560px){ .filters-grid{ grid-template-columns: 1fr; } }
    .field{ display:flex; flex-direction:column; gap:6px; }
    .field label{ color:var(--fia-blue); font-size:.9rem; padding-left:6px; font-weight:600; }
    .input{
      width:100%; padding:10px 12px;
      border:1px solid #d9e9f6; border-radius:999px;
      background:linear-gradient(180deg,#f0f7fd,#ffffff);
      color:var(--ink); font-size:0.95rem; outline:none;
    }
    .input:focus{ border-color:#bfe6ff; box-shadow:0 0 0 4px var(--ring); background:#fff; }
    /* --- CLEAN TAGS: keep checkbox + label together cleanly --- */
.tags-wrap{
  border-radius:12px;
  padding:12px 14px;
  border:1px solid rgba(69,195,179,.35);
  background:#fff;
}

.tags-wrap [id$="FilterTags"]{
  display:flex;
  flex-wrap:wrap;
  gap:10px 12px;
  align-items:flex-start;
}

/* Each ASP.NET CheckBoxList item chip */
.tags-wrap [id$="FilterTags"] > span{
  display:inline-flex !important;
  align-items:center;
  flex-wrap:nowrap !important;
  flex:0 0 auto;
  width:max-content;
  max-width:100%;
  gap:8px;
  padding:7px 10px;
  border-radius:10px;
  background:linear-gradient(180deg,#ffffff,#f7fbff);
  border:1px solid rgba(42,153,219,.18);
  box-shadow:0 1px 0 rgba(0,0,0,.02);
  line-height:1;
  white-space:nowrap;
  vertical-align:top;
}

/* Keep checkbox from drifting or stretching */
.tags-wrap [id$="FilterTags"] input[type="checkbox"]{
  width:16px;
  height:16px;
  margin:0;
  flex:0 0 auto;
  accent-color: var(--fia-blue);
}

/* Keep tag text together with its box */
.tags-wrap [id$="FilterTags"] label{
  margin:0;
  padding:0;
  display:inline-block;
  white-space:nowrap;
  word-break:keep-all;
  overflow-wrap:normal;
  line-height:1.1;
  font-size:.9rem;
  color:var(--fia-blue);
  font-weight:600;
}

/* Hover/focus */
.tags-wrap [id$="FilterTags"] > span:hover{
  border-color: rgba(42,153,219,.35);
  box-shadow:0 0 0 4px var(--ring);
}

/* Selected state */
.tags-wrap [id$="FilterTags"] > span:has(input[type="checkbox"]:checked){
  background:linear-gradient(135deg, rgba(42,153,219,.12), rgba(240,106,169,.10));
  border-color: rgba(240,106,169,.30);
}

@media (max-width: 640px){
  .tags-wrap{
    padding:12px;
  }

  .tags-wrap [id$="FilterTags"]{
    gap:10px 10px;
  }

  .tags-wrap [id$="FilterTags"] > span{
    padding:8px 10px;
    max-width:none;
  }
}

     
    .filters-actions{ display:flex; gap:10px; }
    .filters-actions .btn{ min-width:108px; }

    .hint{ margin-top:6px; font-size:.85rem; color:var(--muted); padding-left:8px; }

    /* ---------- Highlight Alternatives: single FIA dropdown ---------- */
.alt-picker-wrap{
  position:relative;
  display:inline-flex;
  align-items:center;
}

.alt-trigger{
  display:inline-flex;
  align-items:center;
  gap:8px;
  padding:9px 14px;
  border-radius:999px;
  border:1px solid rgba(42,153,219,.22);
  background:linear-gradient(135deg,#f7fbff,#eef7ff);
  color:var(--fia-blue);
  font-family:Poppins;
  font-weight:600;
  font-size:.92rem;
  cursor:pointer;
  box-shadow:0 6px 18px rgba(42,153,219,.08);
  transition:box-shadow .15s ease, transform .12s ease, border-color .12s ease;
}

.alt-trigger:hover,
.alt-trigger:focus{
  outline:none;
  transform:translateY(-1px);
  border-color:rgba(42,153,219,.34);
  box-shadow:0 0 0 4px var(--ring), 0 10px 22px rgba(42,153,219,.12);
}

.alt-trigger-caret{
  font-size:.9rem;
  line-height:1;
  color:var(--fia-blue);
}

.alt-picker{
  position:absolute;
  top:48px;
  right:0;
  min-width:300px;
  max-width:360px;
  background:#fff;
  border:1px solid #dfeaf7;
  border-radius:18px;
  box-shadow:0 20px 42px rgba(42,153,219,.16);
  padding:10px;
  z-index:50;
}

.alt-title{
  font-family:Poppins;
  font-size:.95rem;
  color:var(--fia-blue);
  margin:4px 8px 10px 8px;
}

.alt-list{
  display:flex;
  flex-direction:column;
  gap:8px;
  max-height:260px;
  overflow:auto;
  padding:0 6px 6px 6px;
}

.alt-option{
  display:flex;
  justify-content:space-between;
  align-items:center;
  gap:10px;
  width:100%;
  padding:10px 12px;
  border-radius:14px;
  border:1px solid #e6eef8;
  background:linear-gradient(180deg,#f8fbff,#ffffff);
  cursor:pointer;
  font-size:.92rem;
  color:var(--ink);
}

.alt-option:hover{
  box-shadow:0 0 0 4px var(--ring);
  border-color:#cfe8ff;
}

.alt-actions{
  display:flex;
  gap:8px;
  margin-top:8px;
  justify-content:flex-end;
}

    /* ---------- NEW: Helper one-on-one section ---------- */
    .helper-1on1{
      padding:18px 16px;
      border-radius:20px;
      border:1px solid var(--card-border);
      background:linear-gradient(135deg,#fdf7ff,#f0f7fd);
      box-shadow:0 10px 26px rgba(240,106,169,.07);
    }
    .helper-1on1-header{ margin-bottom:6px; }
    .helper-1on1-header .section-title{ margin-bottom:4px; }

    .conversation-list{
      display:flex;
      flex-direction:column;
      gap:10px;
      margin-top:14px;
    }
    .conversation-card{
      display:flex;
      flex-direction:column;
      gap:4px;
      padding:12px 14px;
      border-radius:14px;
      border:1px solid var(--card-border);
      background:#ffffff;
      text-decoration:none;
      color:inherit;
      box-shadow:0 8px 20px rgba(42,153,219,.04);
      transition:transform .12s ease, box-shadow .12s ease, border-color .12s ease;
    }
    .conversation-card:hover{
      transform:translateY(-1px);
      border-color:#d4e6f7;
      box-shadow:0 12px 28px rgba(42,153,219,.10);
    }
    .conversation-title{
      font-family:Poppins;
      font-weight:600;
      font-size:.98rem;
    }
    .conversation-meta{
      font-size:.85rem;
      color:var(--muted);
    }

    /* ---------- Recommended microcourses ---------- */
.recommend-wrap{
  padding:18px 16px;
  border-radius:20px;
  border:1px solid var(--card-border);
  background:linear-gradient(135deg,#ffffff,#f7fbff);
  box-shadow:0 10px 26px rgba(42,153,219,.06);
}

.recommend-grid{
  display:grid;
  grid-template-columns:repeat(2, minmax(0, 1fr));
  gap:18px;
  margin-top:14px;
}

@media (max-width: 720px){
  .recommend-grid{ grid-template-columns:1fr; }
}

.recommend-topline{
  display:flex;
  align-items:center;
  gap:8px;
  flex-wrap:wrap;
}

.recommend-note{
  background:var(--note-blue);
  border:1px solid #d9e9f6;
  color:#0f3d5e;
  border-radius:12px;
  padding:10px 12px;
  font-size:.92rem;
}

.recommend-card .btn{
  display:inline-block;
  text-decoration:none;
}

.recommend-empty{
  margin-top:12px;
}

.glow-recommended{
  border-color:#e79bc3 !important;
  box-shadow:
    0 0 0 4px rgba(240,106,169,.34),
    0 0 0 9px rgba(42,153,219,.16),
    0 0 32px rgba(240,106,169,.30),
    0 20px 44px rgba(42,153,219,.18);
  transform: translateY(-1px);
  transition: box-shadow .18s ease, border-color .18s ease, transform .12s ease;
}

.glow-recommended::before{
  width:8px;
  background:linear-gradient(180deg, rgba(240,106,169,.95), rgba(42,153,219,.95));
  opacity:1;
}

.glow-recommended:hover{
  transform: translateY(-2px);
}
  </style>

  <script type="text/javascript">
      // ---- Helper: gather distinct missing prereqs from blocked cards ----
      function getBlockedPrereqs() {
          var blocked = Array.prototype.slice.call(document.querySelectorAll('.card.is-blocked'));
          var map = {};
          blocked.forEach(function (card) {
              var id = (card.getAttribute('data-missing-prereq-id') || '').trim();
              var title = (card.getAttribute('data-missing-prereq-title') || '').trim();
              if (id) {
                  if (!map[id]) map[id] = { id: id, title: title || id };
              }
          });
          return Object.keys(map).map(function (k) { return map[k]; });
      }

      // ---- Existing: highlight all alternatives for all blocked prereqs ----
      function highlightAlternatives() {
          try {
              var needed = {};
              getBlockedPrereqs().forEach(function (p) { needed[p.id] = true; });

              var cards = Array.prototype.slice.call(document.querySelectorAll('.card'));
              cards.forEach(function (c) {
                  var mcid = (c.getAttribute('data-microcourse-id') || '').trim();
                  if (needed[mcid]) c.classList.add('glow-alt'); else c.classList.remove('glow-alt');
              });

              if (Object.keys(needed).length === 0) {
                  var blocked = Array.prototype.slice.call(document.querySelectorAll('.card.is-blocked'));
                  blocked.forEach(function (b) {
                      b.classList.add('glow-alt');
                      setTimeout(function () { b.classList.remove('glow-alt'); }, 650);
                  });
              }
          } catch (e) { console.warn('Highlight alternatives failed:', e); }
      }

      // ---- New: highlight only for a chosen course ----
      function highlightAlternativesFor(courseId) {
          try {
              var cards = Array.prototype.slice.call(document.querySelectorAll('.card'));
              cards.forEach(function (c) {
                  var mcid = (c.getAttribute('data-microcourse-id') || '').trim();
                  if (mcid === courseId) c.classList.add('glow-alt'); else c.classList.remove('glow-alt');
              });
          } catch (e) { console.warn('Selective highlight failed:', e); }
      }

      function clearRecommendationHighlights() {
          try {
              Array.prototype.slice.call(document.querySelectorAll('.card.glow-recommended'))
                  .forEach(function (c) { c.classList.remove('glow-recommended'); });

              var clearBtn = document.getElementById('clearRecoHighlightBtn');
              if (clearBtn) clearBtn.style.display = 'none';
          } catch (e) {
              console.warn('Clear recommendation highlight failed:', e);
          }
      }

      function highlightRecommendedCourse(courseId) {
          try {
              clearRecommendationHighlights();

              var found = 0;
              var cards = Array.prototype.slice.call(document.querySelectorAll('.card[data-microcourse-id]'));

              cards.forEach(function (c) {
                  var mcid = (c.getAttribute('data-microcourse-id') || '').trim();
                  if (mcid === courseId) {
                      c.classList.add('glow-recommended');
                      found++;
                  }
              });

              var clearBtn = document.getElementById('clearRecoHighlightBtn');
              if (clearBtn) clearBtn.style.display = found > 0 ? 'inline-block' : 'none';
          } catch (e) {
              console.warn('Recommendation highlight failed:', e);
          }
      }

      // ---- New: toggle and (re)build the picker menu ----
      function toggleAltPicker(ev) {
          ev.preventDefault();

          var picker = document.getElementById('altPicker');
          var trigger = document.getElementById('altTrigger');

          if (!picker) return;

          if (picker.getAttribute('data-built') !== '1') {
              buildAltPicker(picker);
          }

          var willOpen = picker.hidden;
          picker.hidden = !willOpen;

          if (trigger) {
              trigger.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
          }
      }

      function buildAltPicker(picker) {
          picker.innerHTML = '';

          var title = document.createElement('div');
          title.className = 'alt-title';
          title.textContent = 'Highlight alternatives';
          picker.appendChild(title);

          var items = getBlockedPrereqs();
          var list = document.createElement('div');
          list.className = 'alt-list';

          if (items.length === 0) {
              var empty = document.createElement('div');
              empty.className = 'alt-option';
              empty.textContent = 'No prerequisite blocks detected right now.';
              list.appendChild(empty);
          } else {
              items.forEach(function (it) {
                  var row = document.createElement('button');
                  row.type = 'button';
                  row.className = 'alt-option';
                  row.innerHTML =
                      '<span>' + escapeHtml(it.title) + '</span>' +
                      '<span class="pill pill-link" style="margin-right:0;">Highlight</span>';

                  row.onclick = function () {
                      highlightAlternativesFor(it.id);
                      picker.hidden = true;

                      var trigger = document.getElementById('altTrigger');
                      if (trigger) trigger.setAttribute('aria-expanded', 'false');
                  };

                  list.appendChild(row);
              });
          }

          picker.appendChild(list);

          var actions = document.createElement('div');
          actions.className = 'alt-actions';

          var btnAll = document.createElement('button');
          btnAll.type = 'button';
          btnAll.className = 'btn btn-primary';
          btnAll.textContent = 'Highlight All';
          btnAll.onclick = function () {
              highlightAlternatives();
              picker.hidden = true;

              var trigger = document.getElementById('altTrigger');
              if (trigger) trigger.setAttribute('aria-expanded', 'false');
          };

          var btnClear = document.createElement('button');
          btnClear.type = 'button';
          btnClear.className = 'btn btn-ghost';
          btnClear.textContent = 'Clear';
          btnClear.onclick = function () {
              Array.prototype.slice.call(document.querySelectorAll('.card.glow-alt'))
                  .forEach(function (c) { c.classList.remove('glow-alt'); });

              picker.hidden = true;

              var trigger = document.getElementById('altTrigger');
              if (trigger) trigger.setAttribute('aria-expanded', 'false');
          };

          actions.appendChild(btnAll);
          actions.appendChild(btnClear);
          picker.appendChild(actions);

          picker.setAttribute('data-built', '1');
      }

      function escapeHtml(s) {
          return String(s).replace(/[&<>"']/g, function (m) {
              return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[m];
          });
      }

      // Make From/To act like datetime-local inputs.
      document.addEventListener('DOMContentLoaded', function () {
          ['<%=FilterFrom.ClientID%>','<%=FilterTo.ClientID%>'].forEach(function (id) {
              var el = document.getElementById(id); if (el) { el.setAttribute('type', 'datetime-local'); }
          });

          document.addEventListener('click', function (e) {
              var picker = document.getElementById('altPicker');
              var wrap = document.querySelector('.alt-picker-wrap');
              var trigger = document.getElementById('altTrigger');

              if (!picker || !wrap) return;

              if (!wrap.contains(e.target)) {
                  picker.hidden = true;
                  if (trigger) trigger.setAttribute('aria-expanded', 'false');
              }
          });
      });
  </script>
</head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">

      <!-- Header -->
      <div class="brand">
        <div class="badge">FIA</div>
        <div>
          <h1>Your Cyberfair</h1>

          <!-- Chips row -->
          <div class="sub subchips">
            <span class="pill">University: <asp:Literal ID="University" runat="server" /></span>
            <span class="pill">Event: <asp:Literal ID="EventName" runat="server" /></span>

            <a class="pill pill-pink" href="<%: ResolveUrl("~/Account/Participant/SelectEvent.aspx?change=1") %>">
              Change event
            </a>

<a class="pill pill-link"
   href="<%: ResolveUrl("~/Account/Security/Enable2FA.aspx") %>">
    Security
</a>

<a class="pill pill-link pill-push"
   href="<%: ResolveUrl("~/Account/Participant/Settings.aspx") %>">
    Settings
</a>

            <asp:LinkButton ID="BtnLogout"
                            runat="server"
                            CssClass="pill pill-pink"
                            OnClick="BtnLogout_Click"
                            CausesValidation="false">
              Sign out
            </asp:LinkButton>
          </div>
              </div>
      </div>

      <!-- NEW: Session deletion notice (shows when an admin deletes a session you were in) -->
      <asp:PlaceHolder ID="SessionDeletionNoticePH" runat="server" Visible="false">
        <div class="note" style="background:var(--note-pink); border-color:#ffd1e5; color:#7a103a;">
          <strong>Session update:</strong>
          <asp:Literal ID="SessionDeletionNoticeText" runat="server" />
        </div>
      </asp:PlaceHolder>

        <!-- NEW: No-show / attendance notice (from missingParticipantSessions.xml) -->
<asp:PlaceHolder ID="NoShowNoticePH" runat="server" Visible="false">
  <div class="note" style="background:var(--note-pink); border-color:#ffd1e5; color:#7a103a;">
    <strong>Attendance notice:</strong>
    <asp:Literal ID="NoShowNoticeText" runat="server" />
    <div style="margin-top:10px;">
      <asp:HiddenField ID="NoShowAckKey" runat="server" />
      <asp:LinkButton ID="AckNoShowBtn"
                      runat="server"
                      CssClass="pill pill-pink"
                      OnClick="AckNoShowBtn_Click"
                      CausesValidation="false">
        Acknowledge
      </asp:LinkButton>
    </div>
  </div>
</asp:PlaceHolder>

      <asp:PlaceHolder ID="SessionSecurityPanel" runat="server" Visible="false">
        <div class="section">
          <div class="security-panel">
            <div class="security-header">
              <div>
                <div class="section-title" style="margin:0 0 4px 0;">Security &amp; active sessions</div>
                <div class="security-meta">Manage the devices currently signed in to your FIA account.</div>
              </div>
              <asp:Button ID="SignOutOtherSessionsBtn" runat="server" CssClass="btn btn-primary" Text="Sign out other devices" OnClick="SignOutOtherSessionsBtn_Click" />
            </div>
            <div class="security-status">
              <asp:Literal ID="SessionSecurityStatus" runat="server" />
            </div>
            <asp:Repeater ID="ActiveSessionsRepeater" runat="server" OnItemCommand="ActiveSessionsRepeater_ItemCommand">
              <HeaderTemplate>
                <div class="security-list">
              </HeaderTemplate>
              <ItemTemplate>
                <div class="security-item">
                  <div class="security-device">
                    <span class="session-dot"></span>
                    <div>
                      <div class="session-title"><%# Eval("TitleText") %></div>
                      <div class="session-meta">Created <%# Eval("CreatedLocal", "{0:MMM d, yyyy • h:mm tt}") %> · Last activity <%# Eval("LastActiveLocal", "{0:MMM d, yyyy • h:mm tt}") %></div>
                    </div>
                  </div>
                  <div class="security-item-actions">
                    <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("IsCurrent") %>'>
                      <asp:Button runat="server" CssClass="btn btn-ghost" Text="Sign out" CommandName="signout" CommandArgument='<%# Eval("SessionId") %>' />
                    </asp:PlaceHolder>
                    <span class="session-chip"><%# Eval("CurrentLabel") %></span>
                  </div>
                </div>
              </ItemTemplate>
              <FooterTemplate>
                </div>
              </FooterTemplate>
            </asp:Repeater>
          </div>
        </div>
      </asp:PlaceHolder>

      <!-- Privacy note -->
      <div class="note">
        <strong>Privacy:</strong> Your learning results are private to you and the FIA team.
        Your university sees only anonymized or aggregated insights—never your personal answers.
      </div>


      <br /><br />
      <%@ Register Src="~/Account/Participant/ParticipantScoreWidget.ascx" TagName="ParticipantScoreWidget" TagPrefix="fia" %>
      <fia:ParticipantScoreWidget ID="ScoreWidget" runat="server" />
      <br /><br />

        <asp:PlaceHolder ID="RecommendedCoursesPanel" runat="server" Visible="false">
  <div class="section">
    <div class="recommend-wrap">
      <div class="helper-1on1-header">
  <h2 class="section-title" style="margin-bottom:6px;">
    Recommended Microcourses
    <button type="button"
            id="clearRecoHighlightBtn"
            class="pill pill-link"
            style="display:none;"
            onclick="clearRecommendationHighlights()">
      Turn Off Highlight
    </button>
  </h2>

  <p class="subtle" style="margin:0;">
    Based on your cybersecurity score, FIA recommends starting with the highest-risk areas first.
    If a course is locked by a prerequisite, we recommend that prerequisite before the locked course.
  </p>
</div>

      <asp:PlaceHolder ID="RecommendedCoursesEmpty" runat="server" Visible="false">
  <div class="note recommend-empty">
    You do not have any more recommended microcourses right now. Great job so far. You can take whichever course you would like next and have fun exploring the rest of the FIA experience.
  </div>
</asp:PlaceHolder>

      <asp:Repeater ID="RecommendedCoursesRepeater" runat="server">
        <HeaderTemplate>
          <div class="recommend-grid">
        </HeaderTemplate>
        <ItemTemplate>
          <div class="card recommend-card">
            <div class="card-sec">
              <div class="recommend-topline">
                <span class='<%# Eval("BadgeCss") %>'><%#: Eval("BadgeText") %></span>
                <span class="pill"><%#: Eval("GapText") %></span>
              </div>

              <h3 class="title" style="margin-top:10px;"><%#: Eval("Title") %></h3>
              <p class="subtle" style="margin:8px 0 0 0;"><%#: Eval("Summary") %></p>
            </div>

            <div class="card-sec">
              <div class="recommend-note">
                <%#: Eval("Reason") %>
              </div>
            </div>

            <div class="card-sec">
              <div class='info-bar <%# Convert.ToBoolean(Eval("CanSignUp")) ? "ok" : "warn" %>'>
                <span><strong>Status</strong></span>
                <span><%#: Eval("AvailabilityText") %></span>
              </div>
            </div>

            <asp:PlaceHolder runat="server" Visible='<%# Convert.ToBoolean(Eval("HasMatchingSessions")) %>'>
  <div class="card-sec">
    <button type="button"
            class="btn btn-primary"
            onclick="highlightRecommendedCourse('<%# Eval("CourseId") %>')">
      Highlight in Available Sessions
    </button>
  </div>
</asp:PlaceHolder>

<asp:PlaceHolder runat="server" Visible='<%# !Convert.ToBoolean(Eval("HasMatchingSessions")) %>'>
  <div class="card-sec">
    <div class="subtle">No matching session card is currently posted for this event.</div>
  </div>
</asp:PlaceHolder>
          </div>
        </ItemTemplate>
        <FooterTemplate>
          </div>
        </FooterTemplate>
      </asp:Repeater>
    </div>
  </div>
</asp:PlaceHolder>

      <!-- ==== FILTERS ==== -->
      <div class="filters-card">
        <h3 class="filters-title">Filter sessions</h3>

        <div class="filters-grid">
          <div class="field">
            <label for="FilterFrom">From</label>
            <asp:TextBox ID="FilterFrom" runat="server" CssClass="input" />
          </div>

          <div class="field">
            <label for="FilterTo">To</label>
            <asp:TextBox ID="FilterTo" runat="server" CssClass="input" />
          </div>

          <div class="filters-actions">
            <asp:Button ID="ApplyFilters" runat="server" CssClass="btn btn-primary" Text="Apply"
                        OnClick="ApplyFilters_Click" />
            <asp:LinkButton ID="ClearFilters" runat="server" CssClass="btn btn-ghost" Text="Clear"
                            OnClick="ClearFilters_Click" CausesValidation="false" />
          </div>

          <div class="field row-full">
            <label>Tags</label>
            <div class="tags-wrap">
              <asp:CheckBoxList ID="FilterTags" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" />
            </div>
          </div>

          <div class="field row-full">
            <label for="FilterQuery">Search</label>
            <asp:TextBox ID="FilterQuery" runat="server" CssClass="input"
                        placeholder="e.g., enter a microcourse title or helper name" />
            <div class="hint">Tip: type a few letters (e.g., “phish”, “Zoom”, or a helper’s name)</div>
          </div>
        </div>
      </div>
      <hr class="divider" />

      <!-- ==== MY SESSIONS ==== -->
      <asp:PlaceHolder ID="MySessionsWrap" runat="server" Visible="false">
        <div class="section">
          <h2 class="section-title">My Sessions</h2>
          <asp:Repeater ID="MySessionsRepeater" runat="server">
            <HeaderTemplate>
              <div class="fia-sessions-grid">
            </HeaderTemplate>
            <ItemTemplate>
              <div class="card">
                <div class="card-sec">
                  <h3 class="title"><%# Eval("microcourseTitle") %></h3>
                </div>

                <div class="card-sec">
                  <div class="line">
                    <span class="label">Helper</span>
                    <span class="chip helper-chip">
                      <span class="dot" style="background:var(--fia-teal);"></span>
                      <strong><%# Eval("helperName") %></strong>
                    </span>
                  </div>
                </div>

                  <!-- NEW: Session time-change indicator -->
<asp:PlaceHolder ID="TimeChangedPH" runat="server" Visible='<%# (bool)Eval("timeChanged") %>'>
  <div class="card-sec">
    <div class="conflict-note">
      <strong>Session time updated.</strong>
      <span><%# Eval("timeChangedMessage") %></span>
    </div>
  </div>
</asp:PlaceHolder>


                <div class="card-sec">
                  <div class="line">
                    <span class="label">Time</span>
                    <span class="value"><%# Eval("startLocal", "{0:ddd, MMM d • h:mm tt}") %></span>
                  </div>
                  <div class="line" style="margin-top:6px;">
                    <span class="label">Room</span>
                    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("canSeeRoom") %>'>
                      <span class="value">
                        <a href='<%# Eval("room") %>' target="_blank" rel="noopener">Join Room</a>
                      </span>
                    </asp:PlaceHolder>
                    <asp:PlaceHolder runat="server" Visible='<%# !(bool)Eval("canSeeRoom") %>'>
                      <span class="value subtle">Wait for Helper to Send Room Link</span>
                    </asp:PlaceHolder>
                  </div>
                </div>

                                <!-- NEW: Time conflict note for My Sessions (mirrors Available Sessions) -->
              <asp:PlaceHolder ID="MyConflictPH" runat="server" Visible='<%# (bool)Eval("hasConflict") %>'>
                <div class="card-sec">
                  <div class="conflict-note">
                    <strong>Time conflict.</strong>
                    <%# Eval("conflictMessage") %>
                    <div class="conflict-actions">
                      <a class="btn btn-ghost" href="<%# Eval("replacementUrl") %>">See Replacements</a>
                    </div>
                  </div>
                </div>
              </asp:PlaceHolder>


               <!-- Status: Enrolled -->
<asp:PlaceHolder ID="MyEnrolledStatusPH" runat="server" Visible='<%# (bool)Eval("isEnrolled") %>'>
  <div class="card-sec">
    <div class="info-bar ok">
      <span><strong>Status</strong></span>
      <span class="remain">Enrolled</span>
    </div>
  </div>
</asp:PlaceHolder>

<!-- Status: Waitlist -->
<asp:PlaceHolder ID="MyWaitlistStatusPH" runat="server" Visible='<%# !(bool)Eval("isEnrolled") %>'>
  <div class="card-sec">
    <div class="info-bar warn">
      <span><strong>Status</strong></span>
      <span class="remain low">Waitlist</span>
    </div>
  </div>
</asp:PlaceHolder>

                  <!-- NEW: Dedicated waitlist position card -->
<asp:PlaceHolder ID="WaitlistPositionPH" runat="server" Visible='<%# (bool)Eval("showWaitlistPosition") %>'>
  <div class="card-sec">
    <div class="queue-card waitlist-card">
      <div class="queue-card-top">
        <span class="queue-card-title">Waitlist queue</span>
        <span class="queue-position-pill waitlist-position-pill">
          Overall position <%# Eval("overallQueuePosition") %>
        </span>
      </div>

      <div class="queue-card-text">
        You are currently on the waitlist for this session. This is not a guaranteed seat,
        but you may still get a chance if participants cancel, no-show, or if earlier sessions
        move quickly. Please stay checked in around the session time and keep this page open
        to watch your position.
      </div>
    </div>
  </div>
</asp:PlaceHolder>

                  <!-- NEW: Queue position card for multi-participant enrolled sessions -->
<asp:PlaceHolder ID="QueuePositionPH" runat="server" Visible='<%# (bool)Eval("showQueuePosition") %>'>
  <div class="card-sec">
    <div class="queue-card">
      <div class="queue-card-top">
        <span class="queue-card-title">Session queue</span>
        <span class="queue-position-pill">
          Position <%# Eval("queuePosition") %> of <%# Eval("enrolledCount") %>
        </span>
      </div>

      <div class="queue-card-text">
        You are currently in position <strong><%# Eval("queuePosition") %></strong>
        for this session. Please keep this page open around the session time
        and check here for your room link or Helper update when your session is ready.
        Since there are mutiple participants enrolled, your start time may be
        a bit after what is displayed for the session!
      </div>
    </div>
  </div>
</asp:PlaceHolder>

                <!-- Actions -->
              <div class="card-sec">
  <div class="cta-row">
    <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("isEnrolled") && !(bool)Eval("hasConflict") %>'>
      <a class="btn btn-swap" href="<%# Eval("swapUrl") %>">Swap Session</a>
    </asp:PlaceHolder>
  </div>
</div>

                <!-- NEW: Unenroll button (pink) on its own line; visible for enrolled OR waitlisted -->
                <div class="card-sec">
                  <div class="cta-row">
                    <asp:Button ID="UnenrollBtn" runat="server" CssClass="btn btn-pink" Text="Unenroll"
                                CommandName="unenroll" CommandArgument='<%# Eval("sessionId") %>'
                                Visible='<%# (bool)Eval("isEnrolled") || (bool)Eval("isWaitlisted") %>' />
                  </div>
                </div>
              </div>
            </ItemTemplate>
            <FooterTemplate>
              </div>
            </FooterTemplate>
          </asp:Repeater>

          <hr class="divider" />
        </div>
      </asp:PlaceHolder>

      <!-- Empty state -->
      <asp:PlaceHolder ID="EmptySessionsPH" runat="server" Visible="false">
        <div class="note">No sessions are currently available. Please check back soon.</div>
      </asp:PlaceHolder>

      <!-- ==== AVAILABLE SESSIONS ==== -->
      <div class="section" id="available-sessions">
       <h2 class="section-title">
          Available Sessions
        </h2>

        <asp:Repeater ID="SessionsRepeater" runat="server">
          <HeaderTemplate>
            <div class="fia-sessions-grid">
          </HeaderTemplate>

          <ItemTemplate>
            <div class='card <%# ((bool)Eval("prereqMet")) ? "" : "is-blocked" %>'
                 data-microcourse-id='<%# Eval("courseId") %>'
                 data-missing-prereq-id='<%# Eval("missingPrereqId") %>'
                 data-missing-prereq-title='<%# Server.HtmlEncode(Convert.ToString(Eval("missingPrereqTitle") ?? "")) %>'>

              <!-- Title -->
              <div class="card-sec">
                <h3 class="title"><%# Eval("microcourseTitle") %></h3>
              </div>

              <!-- Helper -->
              <div class="card-sec">
                <div class="line">
                  <span class="label">Helper</span>
                  <span class="chip helper-chip">
                    <span class="dot" style="background:var(--fia-teal);"></span>
                    <strong><%# Eval("helperName") %></strong>
                  </span>
                </div>
              </div>

              <!-- Seats -->
              <div class="card-sec">
                <div class="info-bar">
                  <span><strong>Seats</strong> remaining</span>
                  <span class='<%# (Convert.ToInt32(Eval("remainingSeats")) <= 3) ? "remain low" : "remain" %>'>
                    <%# Eval("remainingSeats") %> (<%# Eval("capacity") %>)
                  </span>
                </div>
              </div>

              <!-- Enrollment / Waitlist status -->
              <asp:Literal ID="StatusBadge" runat="server"
                Visible='<%# (bool)Eval("isEnrolled") || (bool)Eval("isWaitlisted") %>'
                Text='<%# (bool)Eval("isEnrolled")
                    ? "<div class=\"card-sec\"><div class=\"info-bar ok\"><span><strong>Status</strong></span><span class=\"remain\">Enrolled</span></div></div>"
                    : "<div class=\"card-sec\"><div class=\"info-bar warn\"><span><strong>Status</strong></span><span>Waitlisted" + "</span></div></div>" %>' />

              <!-- Prereq blocked note -->
              <asp:PlaceHolder ID="BlockedPH" runat="server" Visible='<%# !(bool)Eval("prereqMet") %>'>
                <div class="card-sec">
                  <div class="blocked-note">
                    <strong>Prerequisite needed.</strong>
                    You can't join this yet. First complete: <em><%# Eval("missingPrereqTitle") %></em>.
                  </div>
                </div>
              </asp:PlaceHolder>

              <!-- Overlap conflict note -->
              <asp:PlaceHolder ID="ConflictPH" runat="server" Visible='<%# (bool)Eval("hasConflict") %>'>
                <div class="card-sec">
                  <div class="conflict-note">
                    <strong>Time conflict.</strong>
                    <%# Eval("conflictMessage") %>
                    <div class="conflict-actions">
                      <a class="btn btn-ghost" href="<%# Eval("replacementUrl") %>">See Replacements</a>
                    </div>
                  </div>
                </div>
              </asp:PlaceHolder>

              <!-- Time -->
              <div class="card-sec">
                <div class="line">
                  <span class="label">Time</span>
                  <span class="value"><%# Eval("startLocal", "{0:ddd, MMM d • h:mm tt}") %></span>
                </div>
              </div>

              <!-- Actions -->
              <div class="card-sec">
                <div class="cta-row">
                  <asp:Button ID="EnrollBtn" runat="server" CssClass="btn btn-primary" Text="Enroll"
            CommandName="enroll" CommandArgument='<%# Eval("sessionId") %>'
            Visible='<%# (bool)Eval("prereqMet") && !(bool)Eval("hasConflict") && !(bool)Eval("isEnrolled") && !(bool)Eval("isWaitlisted") && !(bool)Eval("isFull") %>' />

<asp:Button ID="WaitlistBtn" runat="server" CssClass="btn btn-ghost" Text="Join Waitlist"
            CommandName="waitlist" CommandArgument='<%# Eval("sessionId") %>'
            Visible='<%# (bool)Eval("prereqMet") && !(bool)Eval("hasConflict") && !(bool)Eval("isEnrolled") && !(bool)Eval("isWaitlisted") && (bool)Eval("isFull") %>' />
                </div>
              </div>
            </div>
          </ItemTemplate>

          <FooterTemplate>
            </div>
          </FooterTemplate>
        </asp:Repeater>
      </div>
               <!-- ==== MY BADGES (card-style, consistent with other components) ==== -->
<div class="section">
  <div class="helper-1on1" style="background:linear-gradient(135deg,#ffffff,#f0f7fd); border:1px solid var(--card-border); box-shadow:0 10px 26px rgba(42,153,219,.06);">
    <div class="helper-1on1-header">
      <h2 class="section-title" style="margin-bottom:6px;">
        My Badges
        
      </h2>

      <p class="subtle" style="margin:0;">
        Earn a badge each time you complete a microcourse for the first time.
      </p>
    </div>

    <div class="cta-row" style="margin-top:12px;">
      <a class="btn btn-primary" style="text-decoration:none;" href="<%: ResolveUrl("~/Account/Participant/Badges.aspx") %>">
        View my badges
      </a>
    </div>
  </div>
</div>

      <!-- ==== HELPER ONE-ON-ONE SUPPORT ==== -->
      <asp:PlaceHolder ID="HelperSupportPanel" runat="server" Visible="false">
        <div class="section">
          <div class="helper-1on1">
            <div class="helper-1on1-header">
              <h2 class="section-title">
                One-on-one support with
                <span class="pill pill-pink"><asp:Literal ID="HelperName" runat="server" /></span>
              </h2>
              <p class="subtle">
                Stuck on a topic, quiz, or example? You can send a message to your FIA Helper and request a short
                one-on-one meeting. When you write, use a clear topic title, explain exactly what you need help with,
                and share a few times you are available to meet.
              </p>
            </div>

            <div class="cta-row" style="margin-top:10px;">
              <asp:Button ID="StartHelperMessageBtn"
                          runat="server"
                          CssClass="btn btn-primary"
                          Text="Send message"
                          OnClick="StartHelperMessageBtn_Click" />
            </div>

            <asp:PlaceHolder ID="ConversationsEmpty" runat="server" Visible="false">
              <p class="hint" style="margin-top:12px;">
                You have not started a one-on-one conversation yet. After you send your first message, it will appear here.
              </p>
            </asp:PlaceHolder>

            <asp:Repeater ID="ConversationsRepeater" runat="server">
              <HeaderTemplate>
                <div class="conversation-list">
              </HeaderTemplate>
              <ItemTemplate>
                <a class="conversation-card" href='<%# Eval("ConversationUrl") %>'>
                  <div class="conversation-title"><%# Eval("Topic") %></div>
                  <div class="conversation-meta">
                    Sent
                    <span><%# Eval("CreatedOnLocal", "{0:MMM d, yyyy • h:mm tt}") %></span>
                  </div>
                </a>
              </ItemTemplate>
              <FooterTemplate>
                </div>
              </FooterTemplate>
            </asp:Repeater>
          </div>
        </div>
      </asp:PlaceHolder>
      <asp:Label ID="FormMessage" runat="server" EnableViewState="false" />
    </div>
  </form>
</body>
</html>