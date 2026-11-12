<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="PrivacyDisclosure.aspx.cs"
    Inherits="CyberApp_FIA.Account.PrivacyDisclosure" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <title>FIA • Privacy Disclosure</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

    <style>
        :root{--fia-pink:#f06aa9;--fia-blue:#2a99db;--fia-teal:#45c3b3;--muted:#6b7280}
        *{box-sizing:border-box}
        body{margin:0;font-family:Lato,Arial,sans-serif;background:linear-gradient(135deg,#fff,#f9fbff)}
        .wrap{min-height:100vh;display:grid;place-items:center;padding:24px}
        .card{
          width:min(680px,100%);
          background:#fff;
          border:1px solid #e8eef7;
          border-radius:20px;
          box-shadow:0 12px 36px rgba(42,153,219,.08);
          padding:24px
        }
        .brand{display:flex;align-items:center;gap:10px;margin-bottom:10px}
        .badge{
          width:40px;height:40px;border-radius:12px;
          background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
          display:grid;place-items:center;color:#fff;font-family:Poppins
        }
        h1{font-family:Poppins,sans-serif;font-size:1.5rem;margin:0}
        .wrap{align-items:start}
        .card{width:min(780px,100%);color:#1c1c1c;line-height:1.55}
        .updated{color:var(--muted);font-size:.9rem;margin:.25rem 0 1rem}
        h2{font-family:Poppins,sans-serif;font-size:1.1rem;margin:24px 0 8px}
        p{margin:0 0 10px}
        ul{margin:0 0 10px;padding-left:22px}
        li{margin-bottom:6px}
        .btnrow{display:flex;gap:10px;margin-top:18px;flex-wrap:wrap}
        .btn{border:0;border-radius:12px;padding:12px 18px;font-weight:700;font-family:Poppins;cursor:pointer}
        .primary{color:#fff;background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal))}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="wrap">
            <div class="card">
                <div class="brand">
                    <div class="badge">FIA</div>
                    <h1>Privacy Disclosure</h1>
                </div>

        <p class="updated">Last updated: November 2026</p>

        <p>
            Your privacy matters to us. This disclosure explains what information we collect when you use [PLACEHOLDER],
            how we use it, and the choices you have. By creating an account, you agree to the practices described here.
        </p>

        <h2>Information we collect</h2>
        <p><strong>Information you give us.</strong> When you create an account or use the site, you may provide
            information such as your name, email address, password and answers you submit through the site.</p>
        <p><strong>Information about your use of the site.</strong> We keep records of your activity on the site,
            such as the courses and sessions you take part in and your progress.</p>
        <p><strong>Information collected automatically.</strong> When you use the site, we may collect technical
            information such as your IP address and the times you sign in. This helps us keep accounts secure.</p>

        <h2>How we use your information</h2>
        <ul>
            <li>To create and manage your account and provide the site's features</li>
            <li>To personalize your learning experience</li>
            <li>To communicate with you about your account</li>
            <li>To protect the security of your account and the site</li>
            <li>To improve the app experience</li>
        </ul>

        <h2>How we share your information</h2>
        <p>We do not sell your personal information or use it for advertising. We only share information:</p>
        <ul>
            <li>With program staff and volunteers who need it to support you</li>
            <li>With service providers that help us run the site (for example, hosting and email), who may only use it for that purpose</li>
            <li>When required by law, or to protect the safety of our users</li>
        </ul>

        <h2>How we protect your information</h2>
        <p>
            We use reasonable safeguards to protect your information, including encrypting sensitive data and
            never storing your password in a readable form. We strive to ensure the security for all users.
        </p>

        <h2>Communications</h2>
        <p>
            To protect your privacy, emails from us are kept discreet and do not describe the nature of the site
            in the subject line. Please be on the lookout for any emails coming form the offical [PLACEHOLDER] domain (PUT EXXAMPLE EMAIL WHEN WE HAVE ONE)
        </p>

        <h2>Cookies</h2>
        <p>
            We use only the cookies necessary for the site to work, such as keeping you signed in.
            We do not use advertising or tracking cookies.
        </p>

        <h2>Keeping and deleting your information</h2>
        <p>
            We keep your information for as long as your account is active. You can delete your account at any
            time from your account settings, which permanently removes your personal information. Some limited
            records may be kept for security purposes in a form that no longer identifies you.
        </p>

        <h2>Changes to this disclosure</h2>
        <p>
            We may update this disclosure from time to time. If we make significant changes, we will let you know
            through the site or email.
        </p>

        <h2>Contact us</h2>
        <p>If you have questions about this disclosure or your information, please contact the program team.</p>

                <div class="btnrow">
                    <button type="button" id="backBtn" class="btn primary"><%= Server.HtmlEncode(BackButtonText) %></button>
                </div>
            </div>
        </div>

        <script>
            (function () {
                var fromAbout = <%= FromAboutPage ? "true" : "false" %>;
                var backUrl = '<%= HttpUtility.JavaScriptStringEncode(BackUrl) %>';

                document.getElementById('backBtn').addEventListener('click', function () {
                    if (fromAbout) {
                        // Opened from About Us in the same tab -> go back there.
                        window.location.href = backUrl;
                        return;
                    }

                    // Closes this tab so the user lands back on the sign-up tab
                    // Changes to account creation If the tab can't be closed
                    window.close();
                    setTimeout(function () {
                        if (!window.closed) window.location.href = backUrl;
                    }, 300);
                });
            })();
        </script>
    </form>
</body>
</html>
