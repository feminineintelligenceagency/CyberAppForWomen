<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AboutUs.aspx.cs" Inherits="CyberApp_FIA.AboutUs" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>FIA • About Us</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <!-- Google Fonts -->
    <link href="https://fonts.googleapis.com/css2?family=Lato:wght@300;400&family=Poppins:wght@500;700&display=swap" rel="stylesheet" />

    <style>
        :root{
            /* Brand Palette */
            --fia-pink:#f06aa9;
            --fia-blue:#2a99db;
            --fia-teal:#45c3b3;
            --ink:#1c1c1c;
            --muted:#6b7280;
            --bg:#ffffff;
            --chip:#f6f7fb;
            --ring:rgba(42,153,219,.25);
            --cardBorder:#e8eef7;
        }

        *{box-sizing:border-box}
        html,body{height:100%}
        body{
            margin:0;
            font-family:"Lato", system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif;
            color:var(--ink);
            background:linear-gradient(135deg,#ffffff 0%, #f9fbff 100%);
        }

        /* Header */
        .site-header{
            display:flex;
            align-items:center;
            justify-content:space-between;
            gap:1rem;
            padding:24px clamp(16px,4vw,48px);
        }
        .brand{
            display:flex;
            align-items:center;
            gap:12px;
        }
        .brand-badge{
            width:44px;height:44px;border-radius:12px;
            display:grid;place-items:center;
            background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
            color:#fff;font-weight:700;font-family:"Poppins", sans-serif;
            letter-spacing:.5px;
        }
        .brand h1{
            margin:0;font-family:"Poppins", sans-serif;font-weight:700;
            font-size:1.25rem;line-height:1.1;
        }

        .nav-chips{
            display:flex;gap:8px;flex-wrap:wrap;
        }
        .chip{
            padding:8px 12px;border-radius:999px;background:var(--chip);
            color:#455; font-size:.9rem;
            border:1px solid #e7eaf3;
            text-decoration:none;
            display:inline-block;
        }

        /* FIA branded nav chips (About Us / Contact Us) */
.chip-brand{
    background:linear-gradient(135deg,var(--fia-pink),var(--fia-blue));
    color:#fff;
    border:1px solid rgba(255,255,255,.0);
    box-shadow:0 10px 22px rgba(42,153,219,.18);
}
.chip-brand:hover{
    box-shadow:0 0 0 6px var(--ring), 0 12px 26px rgba(42,153,219,.22);
}


        .chip:hover{ box-shadow:0 0 0 6px var(--ring); }

        /* Page Layout */
        .wrap{
            padding:clamp(24px,6vw,64px) clamp(16px,4vw,48px);
        }

        /* Hero */
        .hero{
            display:grid;
            grid-template-columns:1.15fr .85fr;
            gap:clamp(20px,4vw,48px);
            align-items:start;
        }
        @media (max-width:900px){ .hero{ grid-template-columns:1fr } }

        .tagline{
            display:inline-block;
            padding:6px 12px;
            margin-bottom:14px;
            border-radius:999px;
            background:rgba(240,106,169,.1);
            color:var(--fia-pink);
            font-weight:600;
            font-family:"Poppins", sans-serif;
            letter-spacing:.3px;
        }
        .hero h2{
            font-family:"Poppins", sans-serif;
            font-weight:600;
            font-size:clamp(1.8rem,3.2vw,2.6rem);
            margin:0 0 12px 0;
        }
        .accent{ color:var(--fia-blue); }
        .hero p{
            color:var(--muted);
            font-size:1.05rem;
            margin:0 0 14px 0;
            line-height:1.6;
        }

        /* Right card (image/portrait + quick facts) */
        .sidecard{
            border-radius:20px;
            background:#fff;
            border:1px solid var(--cardBorder);
            box-shadow:0 10px 30px rgba(42,153,219,.08);
            overflow:hidden;
        }
        .portrait{
            height:210px;
            background:
                radial-gradient(1200px 300px at 20% 10%, rgba(240,106,169,.22), transparent 60%),
                radial-gradient(900px 260px at 80% 40%, rgba(42,153,219,.20), transparent 58%),
                radial-gradient(900px 260px at 50% 90%, rgba(69,195,179,.18), transparent 55%),
                #ffffff;
            border-bottom:1px solid #eef1f6;
        }
        .sidecard-body{ padding:18px 18px 20px 18px; }
        .sidecard h3{
            font-family:"Poppins", sans-serif;
            margin:0 0 8px 0;
            font-size:1.15rem;
        }
        .bullets{
            margin:0;
            padding-left:18px;
            color:var(--muted);
            line-height:1.6;
        }

        /* Sections */
        .section{
            margin-top:clamp(26px,5vw,46px);
        }
        .section h3{
            font-family:"Poppins", sans-serif;
            margin:0 0 12px 0;
            font-size:1.35rem;
        }
        .grid-2{
            display:grid;
            grid-template-columns:1fr 1fr;
            gap:16px;
        }
        @media (max-width:900px){ .grid-2{ grid-template-columns:1fr } }

        .card{
            border-radius:20px;
            background:#fff;
            border:1px solid var(--cardBorder);
            box-shadow:0 10px 30px rgba(0,0,0,.04);
            padding:18px;
        }
        .card h4{
            font-family:"Poppins", sans-serif;
            margin:0 0 8px 0;
            font-size:1.1rem;
        }
        .card p{
            margin:0;
            color:var(--muted);
            line-height:1.6;
        }

        /* CTA */
        .cta{
            margin-top:clamp(26px,5vw,46px);
        }
        .panel{
            border-radius:20px;
            background:#fff;
            border:1px solid var(--cardBorder);
            box-shadow:0 10px 30px rgba(42,153,219,.08);
            padding:24px;
            display:flex; flex-wrap:wrap; align-items:center; justify-content:space-between; gap:16px;
        }
        .panel h3{
            font-family:"Poppins", sans-serif; margin:0 0 6px 0; font-size:1.25rem;
        }
        .panel p{margin:0; color:var(--muted)}
        .btnrow{ display:flex; gap:12px; flex-wrap:wrap; }
        .btn{
            appearance:none; border:none; cursor:pointer; font-weight:700;
            font-family:"Poppins", sans-serif;
            padding:12px 18px; border-radius:12px; transition:.15s ease transform, box-shadow;
            outline:2px solid transparent; outline-offset:2px;
            text-decoration:none;
            display:inline-block;
        }
        .btn:focus{ box-shadow:0 0 0 6px var(--ring); }
        .btn-primary{
            background:linear-gradient(135deg,var(--fia-blue),var(--fia-teal));
            color:#fff;
        }
        .btn-outline{
            background:#fff; color:var(--fia-blue); border:2px solid var(--fia-blue);
        }
        .btn:hover{ transform:translateY(-1px) }
        .btn:active{ transform:translateY(0) }

        /* Footer */
        .site-footer{
            padding:20px clamp(16px,4vw,48px);
            color:#7a8796; font-size:.9rem;
            display:flex; justify-content:space-between; flex-wrap:wrap; gap:8px;
            border-top:1px solid #eef1f6;
        }
        .footer-link{ color:#7a8796; text-decoration:underline; }
        .footer-link:hover{ color:var(--fia-blue); }
        .dots{
            display:flex; gap:8px; align-items:center;
        }
        .dot{ width:14px; height:14px; border-radius:50% }
        .c1{ background:#404040 } .c2{ background:#d9d9d9 } .c3{ background:#ff2b12 }
        .c4{ background:#ff7a1a } .c5{ background:#b1c12a } .c6{ background:#56c7b5 }
        .c7{ background:#2c82b7 } .c8{ background:#a55bd7 } .c9{ background:#ff6fbd }
    </style>
     <link href="~/Content/mobile.css" rel="stylesheet" /></head>

<body>
    <form id="form1" runat="server">
        <!-- Header -->
        <header class="site-header" role="banner">
            <div class="brand">
                <div class="brand-badge">FIA</div>
                <h1>Feminine Intelligence Agency</h1>
            </div>
            <nav class="nav-chips" aria-label="Quick links">
    <asp:HyperLink runat="server" CssClass="chip chip-brand" NavigateUrl="~/Welcome_Page.aspx" Text="Home" />
    <asp:HyperLink runat="server" CssClass="chip chip-brand" NavigateUrl="~/AboutUs.aspx" Text="About Us" />
    <asp:HyperLink runat="server" CssClass="chip chip-brand" NavigateUrl="~/ContactUs.aspx" Text="Contact Us" />
</nav>

        </header>

        <main class="wrap" role="main">
            <!-- Hero -->
            <section class="hero" aria-label="About FIA">
                <div>
                    <span class="tagline">About Us</span>
                    <h2>What FIA is, and why it matters <span class="accent">for women’s safety</span></h2>

                    <p>
                        FIA is a social impact enterprise that builds tools, training, and services to help women connect to their strongest self.
                        We focus on <strong>social discernment</strong> — the ability to notice coded language and subtle behavior patterns that can
                        appear before manipulation or harm.
                    </p>
                    <p>
                        Through assessment and AI-enabled training, FIA helps users identify personal blind spots and practice practical skills so
                        they can feel more bold, clear, and prepared in new or high-pressure environments.
                    </p>
                    <p>
                        In a world that constantly sends messages that we “lack,” we provide personalized insight and actionable practice that helps
                        women see behavior sooner, respond with confidence, and protect themselves and their community in measurable ways.
                    </p>
                </div>

                <!-- Right side highlight card -->
                <aside class="sidecard" aria-label="Quick overview">
                    <div class="portrait" aria-hidden="true"></div>
                    <div class="sidecard-body">
                        <h3>FIA in a nutshell</h3>
                        <ul class="bullets">
                            <li>Builds social discernment skills you can practice</li>
                            <li>Helps spot patterns before they escalate</li>
                            <li>Turns insight into clear, repeatable actions</li>
                            <li>Designed to feel supportive and trauma-informed</li>
                        </ul>
                    </div>
                </aside>
            </section>

            <!-- What we do -->
            <section class="section" aria-label="What FIA provides">
                <h3>What we provide</h3>
                <div class="grid-2">
                    <div class="card">
                        <h4>Tools and training</h4>
                        <p>
                            We offer structured learning that teaches practical skills for reading social dynamics, recognizing manipulation signals,
                            and responding in ways that protect your boundaries and safety.
                        </p>
                    </div>
                    <div class="card">
                        <h4>Personalized practice</h4>
                        <p>
                            Our approach centers on personalized insight and guided practice, so growth feels concrete and repeatable rather than
                            vague or overwhelming.
                        </p>
                    </div>
                </div>
            </section>

            <!-- Our story -->
            <section class="section" aria-label="Our story">
                <h3>Our story</h3>
                <div class="grid-2">
                    <div class="card">
                        <h4>From “What just happened?” to clear skills</h4>
                        <p>
                            We’re Erika Pajovic and Lauren Nignon. We know what it feels like to be blindsided by social dynamics — confusing
                            relationships, sudden fallouts, and that lingering question: “What just happened?”
                        </p>
                    </div>
                    <div class="card">
                        <h4>Tools we wished we had earlier</h4>
                        <p>
                            As neurodivergent women, we spent years searching for answers. Eventually, we realized we weren’t “broken” — we were
                            missing teachable, practical tools that fill the social gaps most books never explain.
                        </p>
                    </div>
                    <div class="card">
                        <h4>Making the invisible more predictable</h4>
                        <p>
                            Once we learned to notice subtle cues and hidden dynamics, social situations became far more predictable. We could finally
                            understand the “why” behind what used to feel confusing or random.
                        </p>
                    </div>
                    <div class="card">
                        <h4>Our mission</h4>
                        <p>
                            We’re building what we wish existed back then: accessible Social Discernment tools so you don’t have to live in catch-up
                            mode. We’re here to help you spot signals early, stay a step ahead, and feel grounded in your decisions.
                        </p>
                    </div>
                </div>
            </section>

            <!-- CTA -->
            <section class="cta" aria-label="Next steps">
                <div class="panel">
                    <div>
                        <h3>Ready to explore the app?</h3>
                        <p>Head back home to create an account or sign in.</p>
                    </div>
                    <div class="btnrow">
                        <asp:HyperLink runat="server" CssClass="btn btn-primary" NavigateUrl="~/Account/CreateAccountPage.aspx" Text="Create Account" />
                        <asp:HyperLink runat="server" CssClass="btn btn-outline" NavigateUrl="~/Account/Login.aspx" Text="Sign In" />
                        <asp:HyperLink runat="server" CssClass="btn btn-outline" NavigateUrl="~/Welcome_Page.aspx" Text="Back to Home" />
                        <!-- <asp:HyperLink runat="server" CssClass="btn btn-outline" NavigateUrl="~/PrivacyDisclosure.aspx?from=about" Text="Privacy Disclosure" /> -->
                    </div>
                </div>
            </section>
        </main>

        <!-- Footer -->
        <footer class="site-footer">
            <div>
                © <%= DateTime.Now.Year %> Feminine Intelligence Agency
                · <asp:HyperLink runat="server" CssClass="footer-link" NavigateUrl="~/PrivacyDisclosure.aspx?from=about" Text="Privacy Disclosure" />
            </div>
            <div class="dots" aria-hidden="true">
                <div class="dot c1"></div><div class="dot c2"></div><div class="dot c3"></div>
                <div class="dot c4"></div><div class="dot c5"></div><div class="dot c6"></div>
                <div class="dot c7"></div><div class="dot c8"></div><div class="dot c9"></div>
            </div>
        </footer>
    </form>
</body>
</html>

