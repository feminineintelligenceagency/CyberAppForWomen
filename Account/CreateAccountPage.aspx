<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CreateAccountPage.aspx.cs" Inherits="CyberApp_FIA.Account.CreateAccountPage" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
  <title>FIA • Sign Up</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@600&display=swap" rel="stylesheet" />

  <link runat="server" href="~/Content/fia-brand.css" rel="stylesheet" />
  <style>
    /* ========== Design tokens / base colors / focus ring ========== */
    :root{--ink:#1c1c1c;--muted:#6b7280;--ring:rgba(42,153,219,.25)}

    /* ========== Base layout ========== */
    *{box-sizing:border-box}
    body{margin:0;font-family:Lato,Arial,sans-serif;background:linear-gradient(135deg,#fff,#f9fbff)}

    /* ========== Centered card wrapper ========== */
    .wrap{min-height:100vh;display:grid;place-items:center;padding:24px}

    /* ========== Main card ========== */
    .card{
      width:min(680px,100%);
      background:#fff;
      border:1px solid #e8eef7;
      border-radius:20px;
      box-shadow:0 12px 36px rgba(42,153,219,.08);
      padding:24px
    }

    /* ========== Brand row (badge + title) ========== */
    .brand{display:flex;align-items:center;gap:10px;margin-bottom:10px}
    .badge{
      width:40px;height:40px;border-radius:12px;
      background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
      display:grid;place-items:center;color:#fff;font-family:Poppins
    }
    h1{font-family:Poppins,sans-serif;font-size:1.5rem;margin:0}
    p.sub{color:var(--muted);margin:.25rem 0 1rem}

    /* ========== Grid of inputs ========== */
    .grid{display:grid;grid-template-columns:1fr 1fr;gap:14px}
    @media (max-width:720px){.grid{grid-template-columns:1fr}}

    /* ========== Form fields ========== */
    label{font-weight:600;font-family:Poppins;font-size:.95rem}
    input[type=text],input[type=email],input[type=password]{
      width:100%;padding:12px 14px;border-radius:12px;border:1px solid #e5e7eb
    }
    input:focus{outline:0;box-shadow:0 0 0 5px var(--ring);border-color:var(--fia-blue)}

    /* ========== Consent box ========== */
    .consent{background:#f6f7fb;border:1px solid #e8eef7;border-radius:12px;padding:12px;margin-top:6px}
    .row{display:flex;gap:10px;align-items:flex-start}

    /* ========== Buttons row ========== */
    .btnrow{display:flex;gap:10px;margin-top:14px;flex-wrap:wrap}
    .btn{border:0;border-radius:12px;padding:12px 18px;font-weight:700;font-family:Poppins;cursor:pointer}
    .primary{color:#fff;background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal))}
    .link{background:#fff;border:2px solid var(--fia-blue);color:var(--fia-blue)}

    /* ========== Validation text ========== */
    .val{color:#c21d1d;font-size:.9rem;margin-top:4px}
  </style>
</head>

<body>
  <form id="form1" runat="server">
    <div class="wrap">
      <div class="card">

        <!-- Header / title -->
        <div class="brand">
          <img runat="server" src="~/Content/logo.png"
            width="40" height="40" alt="Feminine Intelligence Agency" />
          <h1>Create your Participant account</h1>
        </div>
        <p class="sub">Click with confidence—learn, practice, and stay safe online.</p>

        <!-- ===================== Inputs: name, email, password ===================== -->
        <div class="grid">
          <!-- First name -->
          <div>
            <label for="FirstName">First name</label>
            <asp:TextBox ID="FirstName" runat="server" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="FirstName"
              runat="server"
              ErrorMessage="First name is required."
              Display="Dynamic" />
          </div>

          <!-- Last name -->
          <div>
            <label for="LastName">Last name</label>
            <asp:TextBox ID="LastName" runat="server" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="LastName"
              runat="server"
              ErrorMessage="Last name is required."
              Display="Dynamic" />
          </div>

          <!-- Email -->
          <div style="grid-column:1/-1">
            <label for="Email">Email</label>
            <asp:TextBox ID="Email" runat="server" TextMode="Email" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="Email"
              runat="server"
              ErrorMessage="Email is required."
              Display="Dynamic" />
            <asp:RegularExpressionValidator
              CssClass="val"
              ControlToValidate="Email"
              runat="server"
              ValidationExpression="^\S+@\S+\.\S+$"
              ErrorMessage="Enter a valid email."
              Display="Dynamic" />
          </div>

          <!-- Password -->
          <div>
            <label for="Password">Password</label>
            <asp:TextBox ID="Password" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator
              CssClass="val"
              ControlToValidate="Password"
              runat="server"
              ErrorMessage="Password is required."
              Display="Dynamic" />
          </div>

          <!-- Confirm password -->
          <div>
            <label for="Confirm">Confirm password</label>
            <asp:TextBox ID="Confirm" runat="server" TextMode="Password" />
            <asp:CompareValidator
              CssClass="val"
              ControlToValidate="Confirm"
              ControlToCompare="Password"
              runat="server"
              ErrorMessage="Passwords must match."
              Display="Dynamic" />
          </div>
        </div>

        <!-- Hidden role (always Participant in this flow) -->
        <!-- Role fixed to Participant for this flow -->
        <asp:HiddenField ID="Role" runat="server" Value="Participant" />

        <!-- ===================== Consent section ===================== -->
        <div class="consent">
          <div class="row">
            <asp:CheckBox ID="Consent" runat="server" />
            <label for="Consent">
              I consent to FIA storing my account details and activity needed for my learning and safety.
              I understand I can request deletion at any time.
            </label>
          </div>

          <!-- Server-side validator for consent -->
          <asp:CustomValidator
            ID="ConsentValidator"
            runat="server"
            CssClass="val"
            OnServerValidate="ConsentValidator_ServerValidate"
            ErrorMessage="Consent is required to continue."
            Display="Dynamic" />
        </div>

        <!-- ===================== Actions ===================== -->
        <div class="btnrow">
          <!-- Create account (validates fields) -->
          <asp:Button
            ID="BtnSignUp"
            runat="server"
            Text="Create account"
            CssClass="btn primary"
            OnClick="BtnSignUp_Click" />

          <!-- Navigate to sign-in (skips validation) -->
          <asp:Button
            ID="BtnSignIn"
            runat="server"
            Text="Already have an account? Sign in"
            CssClass="btn link"
            CausesValidation="false"
            PostBackUrl="~/Account/Login.aspx" />
        </div>

        <!-- Inline status/error message area (no viewstate to avoid stale text) -->
        <asp:Label ID="FormMessage" runat="server" EnableViewState="false" />

      </div>
    </div>
  </form>
</body>
</html>


