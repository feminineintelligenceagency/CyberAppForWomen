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
        .placeholder{
          background:#f6f7fb;border:1px dashed #cdd6e4;border-radius:12px;
          padding:24px;margin-top:14px;color:var(--muted);text-align:center
        }
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

                <div class="placeholder">
                    ***THIS IS PLACEHOLDER TEXT FOR THE PRIVACY DISCLOSURE.***
                </div>

                <div class="btnrow">
                    <button type="button" id="backBtn" class="btn primary">Back to account creation</button>
                </div>
            </div>
        </div>

        <script>
            // Closes this tab so the user lands back on the sign-up tab
            // Changes to account creation If the tab can't be closed
            document.getElementById('backBtn').addEventListener('click', function () {
                window.close();
                setTimeout(function () {
                    if (!window.closed) {
                        window.location.href = '<%= ResolveUrl("~/Account/CreateAccountPage.aspx") %>';
                    }
                }, 300);
            });
        </script>
    </form>
</body>
</html>
