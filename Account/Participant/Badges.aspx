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
    /* Certificate download (only on badges that have a certificate) */
    .cert-row{ margin-top:10px; display:flex; flex-wrap:wrap; align-items:center; gap:8px; }
    .btn-cert{
      display:inline-flex; align-items:center; min-height:44px;
      padding:8px 14px; border-radius:12px; text-decoration:none;
      font-family:Poppins; font-weight:600; font-size:.88rem; color:#fff;
      background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
    }
    .btn-cert:hover{ box-shadow:0 0 0 5px var(--ring); }
    .btn-copy{
      display:inline-flex; align-items:center; min-height:44px;
      padding:8px 14px; border-radius:12px; cursor:pointer;
      font-family:Poppins, system-ui, sans-serif; font-weight:600; font-size:.88rem;
      color:var(--fia-blue); background:#fff; border:2px solid var(--fia-blue);
    }
    .btn-copy:hover{ box-shadow:0 0 0 5px rgba(42,153,219,.25); }
    .btn-copy.copied{ color:#0a5b4e; border-color:var(--fia-teal); }
    .cert-code{ color:var(--muted); font-size:.8rem; font-family:Consolas, monospace; }

    /* Test badge button (development only) */
    .test-row{ display:flex; flex-wrap:wrap; align-items:center; gap:10px; margin:0 0 6px; }
    .btn-test{
      border:2px dashed var(--fia-pink); background:#fff; color:var(--fia-pink);
      border-radius:12px; padding:8px 14px; font-family:Poppins; font-weight:600; cursor:pointer;
    }
    .test-msg{ color:var(--muted); font-size:.9rem; }
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

      <!-- Development only: hidden on the live site (see code-behind) -->
      <div class="test-row">
        <asp:Button ID="BtnAddTestBadge" runat="server"
          Text="Add test badge (with certificate)"
          CssClass="btn-test"
          OnClick="BtnAddTestBadge_Click"
          CausesValidation="false" />
        <asp:Button ID="BtnAddTestBadgeNoCert" runat="server"
          Text="Add test badge (no certificate)"
          CssClass="btn-test"
          OnClick="BtnAddTestBadgeNoCert_Click"
          CausesValidation="false" />
        <asp:Label ID="TestBadgeMessage" runat="server" EnableViewState="false" CssClass="test-msg" />
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
            <asp:Image runat="server" CssClass="badgeImg" ImageUrl='<%# Eval("ImageUrl") %>' AlternateText=""
              Visible='<%# !string.IsNullOrEmpty((string)Eval("ImageUrl")) %>' />
            <div class="meta">
              <p class="title"><%# Eval("Title") %></p>
              <p class="desc"><%# Eval("Description") %></p>
              <span class="earned">Earned • <%# Eval("EarnedOnLabel") %></span>

              <%-- Only shown when this badge has a certificate --%>
              <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("HasCertificate") %>'>
                <div class="cert-row">
                  <a class="btn-cert" href='<%# Eval("CertificateUrl") %>'>Download certificate (PDF)</a>
                  <button type="button" class="btn-copy" data-url='<%# HttpUtility.HtmlAttributeEncode((string)Eval("VerifyUrl")) %>'>
                    Copy verification link
                  </button>
                  <span class="cert-code">No. <%# Eval("CertificateCode") %></span>
                </div>
              </asp:PlaceHolder>
            </div>
          </div>
        </ItemTemplate>
        <FooterTemplate>
          </div>
        </FooterTemplate>
      </asp:Repeater>

    </div>
    <script>
      // "Copy verification link": copies the employer link for that certificate.
      document.addEventListener('click', function (e) {
        var btn = e.target.closest ? e.target.closest('.btn-copy') : null;
        if (!btn) return;
        var url = btn.getAttribute('data-url');
        var label = btn.textContent;

        function done(ok) {
          btn.textContent = ok ? 'Link copied!' : 'Copy failed: ' + url;
          if (ok) btn.classList.add('copied');
          setTimeout(function () { btn.textContent = label; btn.classList.remove('copied'); }, 2500);
        }

        if (navigator.clipboard && window.isSecureContext) {
          navigator.clipboard.writeText(url).then(function () { done(true); }, function () { done(false); });
        } else {
          // Older browsers: copy through a hidden text box
          var t = document.createElement('textarea');
          t.value = url; t.style.position = 'fixed'; t.style.opacity = '0';
          document.body.appendChild(t); t.select();
          var ok = false; try { ok = document.execCommand('copy'); } catch (err) {}
          document.body.removeChild(t);
          done(ok);
        }
      });
    </script>
  </form>
</body>
</html>