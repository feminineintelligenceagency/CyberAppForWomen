<%@ Page Language="C#" AutoEventWireup="true"
    CodeBehind="Enable2FA.aspx.cs"
    Inherits="CyberApp_FIA.Account.Security.Enable2FA" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <meta charset="utf-8" />
    <title>Two-Factor Authentication | FIA</title>

    <meta name="viewport" content="width=device-width, initial-scale=1.0" />

    <style>
        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 30px 16px;

            font-family: "Lato", Arial, sans-serif;

            background:
                linear-gradient(
                    135deg,
                    rgba(240, 106, 169, 0.12),
                    rgba(42, 153, 219, 0.12),
                    rgba(69, 195, 179, 0.12)
                );
        }

        .auth-card {
            width: 100%;
            max-width: 470px;
            background: white;
            border-radius: 20px;
            padding: 34px;
            box-shadow: 0 12px 35px rgba(0, 0, 0, 0.10);
            text-align: center;
        }

        .fia-badge {
            width: 58px;
            height: 58px;
            margin: 0 auto 18px;

            display: flex;
            align-items: center;
            justify-content: center;

            border-radius: 16px;

            background: linear-gradient(
                135deg,
                #f06aa9,
                #2a99db,
                #45c3b3
            );

            color: white;
            font-family: "Poppins", Arial, sans-serif;
            font-weight: 700;
            font-size: 18px;
        }

        h1 {
            margin: 0 0 10px;
            font-family: "Poppins", Arial, sans-serif;
            font-size: 25px;
            color: #252525;
        }

        h2 {
            font-family: "Poppins", Arial, sans-serif;
            color: #252525;
        }

        .description {
            color: #666;
            line-height: 1.55;
            margin-bottom: 24px;
        }

        .step-label {
            font-weight: 700;
            color: #333;
            margin: 20px 0 10px;
        }

        .qr-wrapper {
            display: inline-block;
            background: #fff;
            padding: 12px;
            border: 1px solid #e8e8e8;
            border-radius: 14px;
            margin-bottom: 10px;
        }

        .qr-wrapper img {
            display: block;
            width: 220px;
            height: 220px;
        }

        .code-input {
            width: 100%;
            padding: 14px 16px;
            margin-top: 8px;

            border: 1px solid #d8d8d8;
            border-radius: 10px;

            font-size: 18px;
            text-align: center;
            letter-spacing: 4px;

            outline: none;
        }

        .code-input:focus {
            border-color: #2a99db;
            box-shadow: 0 0 0 3px rgba(42, 153, 219, 0.12);
        }

        .verify-button {
            width: 100%;
            margin-top: 14px;
            padding: 14px;

            border: none;
            border-radius: 10px;

            background: linear-gradient(
                90deg,
                #2a99db,
                #45c3b3
            );

            color: white;
            font-size: 16px;
            font-weight: 700;

            cursor: pointer;
        }

        .verify-button:hover {
            opacity: 0.92;
        }

        .message {
            margin-top: 18px;
            font-weight: 600;
            color: #444;
        }

        .backup-codes {
            text-align: left;
        }

        .backup-codes h2 {
            text-align: center;
            margin-bottom: 8px;
        }

        .backup-warning {
            text-align: center;
            color: #666;
            line-height: 1.5;
            margin-bottom: 20px;
        }

        .codes-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
        }

        .backup-code {
            padding: 11px 8px;
            text-align: center;

            background: #f6f8fa;
            border: 1px solid #e3e6e8;
            border-radius: 8px;

            font-family: monospace;
            font-size: 14px;
            font-weight: 700;
        }

        .back-link {
            display: inline-block;
            margin-top: 22px;
            color: #2a99db;
            text-decoration: none;
            font-weight: 600;
        }

        .back-link:hover {
            text-decoration: underline;
        }

        .enabled-icon {
            width: 64px;
            height: 64px;

            margin: 10px auto 18px;

            display: flex;
            align-items: center;
            justify-content: center;

            border-radius: 50%;
            background: rgba(69, 195, 179, 0.14);

            color: #28a38f;
            font-size: 32px;
            font-weight: bold;
        }

        @media (max-width: 500px) {
            .auth-card {
                padding: 26px 20px;
            }

            .codes-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>

<body>

    <div class="auth-card">

        <div class="fia-badge">FIA</div>

        <%-- STATE 1: BACKUP CODES AFTER SUCCESSFUL ENROLLMENT --%>
        <% if (BackupCodes != null && BackupCodes.Count > 0) { %>

            <div class="enabled-icon">&#10003;</div>

            <h1>Two-Factor Authentication Enabled</h1>

            <p class="description">
                Your authenticator app is now connected to your FIA account.
            </p>

            <div class="backup-codes">

                <h2>Save Your Backup Codes</h2>

                <p class="backup-warning">
                    Store these somewhere safe. Each backup code can only
                    be used once if you lose access to your authenticator.
                </p>

                <div class="codes-grid">

                    <% foreach (string code in BackupCodes) { %>

                        <div class="backup-code">
                            <% Response.Write(Server.HtmlEncode(code)); %>
                        </div>

                    <% } %>

                </div>

            </div>


        <%-- STATE 2: NORMAL MFA ENROLLMENT --%>
        <% } else if (!string.IsNullOrEmpty(QrImageUrl)) { %>

            <h1>Enable Two-Factor Authentication</h1>

            <p class="description">
                Add an extra layer of security to your FIA account
                using an authenticator app.
            </p>

            <div class="step-label">
                1. Scan this QR code
            </div>

            <p class="description">
                Use Microsoft Authenticator, Google Authenticator,
                or another authenticator app.
            </p>

            <div class="qr-wrapper">
                <img
                    src="<% Response.Write(QrImageUrl); %>"
                    alt="Authenticator setup QR code" />
            </div>

            <div class="step-label">
                2. Enter the 6-digit code
            </div>

            <form method="post">

                <input
                    class="code-input"
                    type="text"
                    name="totpCode"
                    maxlength="6"
                    pattern="[0-9]{6}"
                    inputmode="numeric"
                    autocomplete="one-time-code"
                    placeholder="000000"
                    required />

                <button
                    class="verify-button"
                    type="submit">

                    Enable Two-Factor Authentication

                </button>

            </form>

            <% if (!string.IsNullOrEmpty(VerificationMessage)) { %>

                <div class="message" aria-live="polite">
                    <%= Server.HtmlEncode(VerificationMessage) %>
                </div>

            <% } %>


        <%-- STATE 3: MFA IS ALREADY ENABLED --%>
        <% } else { %>

            <div class="enabled-icon">&#10003;</div>

            <h1>Two-Factor Authentication</h1>

            <p class="description">
                <% Response.Write(Server.HtmlEncode(VerificationMessage)); %>
            </p>

        <% } %>


        <a class="back-link"
           href="<%: ResolveUrl("~/Account/Participant/Home.aspx") %>">
            Back to Cyberfair
        </a>
    </div>

</body>
</html>