# ABD Website Rebuild — Master Project Decisions
> Hill Country Web Co. · Client: Austin Ballroom Dancers (danceatx.org)
> Last updated: June 2026

---

## 1. The Client — Austin Ballroom Dancers

| Item | Detail |
|---|---|
| Full name | Austin Ballroom Dancers (ABD) |
| Type | Non-profit, all-volunteer social club |
| Founded | 1981 |
| Location | Austin, TX |
| Current website | danceatx.org |
| Current platform | Squarespace |
| Domain registrar | Network Solutions |
| Budget | Low — cost minimization is a priority |

### What the current site has
- Public pages: Home, Social Dances & Events, Avalon Ball, Learn to Dance,
  Dance Hosts, Calendar, Membership, Our Supporters, Volunteer, History,
  Contact, ABD By Laws
- Google Calendar embed (keep — same calendar ID in new site)
- Square donation link (keep — square.link/u/j9NDxUHf)
- Squarespace store for $50/year membership (replace with Stripe)
- Facebook and Instagram links (keep)
- No member login, no member database, no automated emails

### What the current site is missing
- Member login / portal
- Member database
- Automated membership expiry reminders
- Officer admin area
- Broadcast email to members
- File storage for meeting notes, bylaws, forms

### Important before going live
- Download all photos from Squarespace before cancelling subscription
- Get access to Network Solutions account to update DNS
- Consider transferring domain to Cloudflare (~$10/yr vs ~$40+/yr at
  Network Solutions)

---

## 2. The Developer — Hill Country Web Co.

| Item | Detail |
|---|---|
| Business name | Hill Country Web Co. |
| Domain | hillcountrywebco.com (registered at Squarespace) |
| Dev email | hillcountryweb.atx@gmail.com |
| Future professional email | you@hillcountrywebco.com (Google Workspace, ~$6/mo) |
| Developer skills | .NET / C# · JavaScript · MSSQL · some Unix familiarity |
| Learning goals | Linux server admin · PostgreSQL · ASP.NET Core Razor Pages |

---

## 3. Hosting Decision — DigitalOcean VPS

**Chosen: DigitalOcean VPS (Linux)**

| Item | Detail |
|---|---|
| Provider | DigitalOcean |
| Plan | Basic Droplet — 1GB RAM, 1 CPU, 25GB SSD |
| Cost | ~$6/month |
| OS | Ubuntu 24.04 LTS |
| Web server | Nginx (reverse proxy) |
| App server | ASP.NET Core 8 (.NET 8) |
| Database | PostgreSQL |
| SSL | Certbot (Let's Encrypt — free, auto-renews) |

### Why VPS over alternatives
- Cheapest option (~$6/mo vs ~$18-20/mo for Azure App Service + Azure SQL)
- Full control over the server
- Good learning experience for Linux/server admin
- .NET 8 runs well on Linux

### Why not Azure
- More expensive for this scale
- Overkill for a small club site

### Why not Wild Apricot / Wix
- Monthly cost higher in long run
- Less control and customization
- Not a learning opportunity

### Total monthly cost estimate
| Item | Cost |
|---|---|
| DigitalOcean VPS | $6/mo |
| Domain (danceatx.org) | ~$1/mo (~$12/yr) |
| SSL | Free (Certbot) |
| Email sending (SMTP2GO) | Free (up to 1,000/mo) |
| Stripe | Free + 2.9% + $0.30 per transaction |
| **Total fixed** | **~$7/mo** |

---

## 4. Technology Stack

| Layer | Choice | Notes |
|---|---|---|
| Framework | ASP.NET Core 10 | Developer already knows .NET |
| UI pattern | Razor Pages | Best fit for page-centric app; simpler than MVC |
| ORM | Entity Framework Core | Code-first migrations |
| Database | PostgreSQL | Free, runs well on cheap VPS, EF Core supported |
| Local dev DB | SQL Server Express | Familiar; EF Core abstracts differences |
| Authentication | Google OAuth 2.0 | Free; no password management needed |
| Payment | Stripe | 2.9% + $0.30/transaction; no monthly fee |
| Email sending | SMTP2GO | Free tier up to 1,000 emails/month |
| Email testing | Ethereal Email | Fake SMTP for local dev — no real emails sent |
| IDE | Visual Studio 2022 Community | Free; familiar to developer |
| Version control | Git + GitHub | Free |
| VPS SSH client | Windows Terminal | Built-in SSH on Windows |

### Why Razor Pages over MVC
- Page-centric apps are a natural fit for Razor Pages
- Less boilerplate than MVC for this use case
- Microsoft-recommended pattern for new page-driven web apps
- Each page is self-contained — easier to navigate as a solo developer

### Why Razor Pages over Blazor
- Blazor is Microsoft's future direction but is overkill for this project
- Steeper learning curve — too many new things at once alongside
  Linux, PostgreSQL, and server admin
- Recommended: learn Blazor as a next project after this one

### Why PostgreSQL over SQL Server on VPS
- SQL Server requires 2GB+ RAM minimum — too heavy for a $6/mo VPS
- PostgreSQL is free, lightweight, fully supported by EF Core (Npgsql)
- ~90% same SQL syntax as MSSQL; differences handled by EF Core provider

### Why Stripe over PayPal
- Identical cost: 2.9% + $0.30 per transaction
- Cleaner, more modern checkout — user never leaves your site
- Better developer documentation and SDK
- No controversial founding figures (founded by Patrick & John Collison)
- Developer decided: Stripe is the chosen payment processor

---

## 5. Authentication Design

- **Google OAuth 2.0** — free, no passwords stored
- Members log in with their Gmail address
- Gmail must match the email address on their Member record
- New members: pay via Stripe → member record created automatically →
  can then log in with Google
- Custom claims added to login session: MemberId, IsOfficer, OfficerRole,
  ExpiryDate

### Access levels
| Area | Access rule |
|---|---|
| Public pages | Anyone — no login |
| /Members/* | Must be logged in with active membership |
| /Officers/* | Must be logged in + IsOfficer = true |

---

## 6. Database Schema

Six tables — all created via EF Core code-first migrations:

| Table | Purpose |
|---|---|
| Members | Core member records — name, email, GoogleSubId, expiry, officer flag |
| Payments | Stripe transaction history per member |
| EmailLog | Record of every automated email sent (prevents duplicate sends) |
| BroadcastEmails | Record of officer-sent announcements |
| MeetingNotes | Officer-uploaded meeting minute files |
| ClubFiles | General file storage (bylaws, forms, flyers) |

### Key fields on Members table
- `Email` — unique, matched to Google account at login
- `GoogleSubId` — unique Google identifier, stored on first login
- `ExpiryDate` — checked nightly by ReminderService background job
- `IsOfficer` + `OfficerRole` — controls access to officer area

---

## 7. Email System

### Automated expiry reminders (ReminderService.cs)
- Background service runs every 24 hours
- Checks Members table for upcoming expiry dates
- Sends emails at: **60 days**, **30 days**, **7 days** before expiry
- Also sends: Welcome (on signup), Expired (on expiry date)
- EmailLog table prevents duplicate sends
- No officer action required — fully automatic

### Broadcast emails
- Officers can send announcements to member segments:
  - All members
  - Active members only
  - Expiring soon
  - Lapsed members
- Logged in BroadcastEmails table

### Email provider
- **Production**: SMTP2GO (free up to 1,000 emails/month)
- **Local dev/testing**: Ethereal Email (fake SMTP — emails visible
  in browser, never delivered)
- From address: noreply@danceatx.org (once club email is set up)
- Dev/testing from address: hillcountryweb.atx@gmail.com

---

## 8. Payment Flow (Stripe)

```
Member fills in name + email on /Membership
        ↓
App calls Stripe API — creates checkout session
        ↓
Member redirected to Stripe hosted checkout page
        ↓
Member pays $50 with credit/debit card
        ↓
Stripe redirects back to /Membership?success=true
        ↓
Stripe also POSTs webhook to /Webhooks/Stripe
        ↓
Webhook handler creates Member + Payment records
Welcome email sent via SMTP2GO
        ↓
Member can now log in with Google
```

### Membership options to decide
The current site offers two options at the same price:
- Annual Membership (Auto-renewing) — $50/yr
- Annual Membership (Non-renewing) — $50/yr

Decision needed: keep both options or simplify to one in the new site?

### Stripe fees on $50 membership
- Fee: $1.75 (2.9% + $0.30)
- Club receives: $48.25 per membership

---

## 9. New Site Page Structure

```
Public (no login required)
├── /                    Home
├── /dances              Social Dances & Events
├── /avalon-ball         Avalon Ball annual trip
├── /learn-to-dance      Dance instruction resources
├── /dance-hosts         Dance host program info
├── /calendar            Google Calendar embed
├── /membership          Join / Renew — Stripe payment
├── /supporters          Donor recognition
├── /volunteer           Volunteer opportunities
├── /history             Club history since 1981
└── /contact             Contact form

Auth
├── /Auth/Login          Google sign-in entry point
├── /Auth/Callback       OAuth callback handler
└── /Auth/AccessDenied   Not a member message

Member area (login required)
└── /Members/Dashboard   Membership status, events, profile

Officer area (officer login required)
├── /Officers/Dashboard  Overview + quick stats
├── /Officers/Members    Member list, filter, edit
├── /Officers/Emails     Broadcast email composer
├── /Officers/MeetingNotes  Upload / view meeting minutes
└── /Officers/Files      Club file storage

Webhooks
└── /Webhooks/Stripe     Stripe payment confirmation handler

Dev only (delete before go-live)
└── /Dev/Seed            Add founding officer to empty database
```

---

## 10. Project Folder Structure

```
AbdClub/
├── Data/
│   └── AbdContext.cs
├── Models/
│   ├── Member.cs
│   ├── Payment.cs
│   ├── EmailLog.cs
│   ├── MeetingNote.cs
│   └── ClubFile.cs
├── Pages/
│   ├── Index.cshtml
│   ├── Dances.cshtml
│   ├── AvalonBall.cshtml
│   ├── LearnToDance.cshtml
│   ├── DanceHosts.cshtml
│   ├── Calendar.cshtml
│   ├── Membership.cshtml
│   ├── Supporters.cshtml
│   ├── Volunteer.cshtml
│   ├── History.cshtml
│   ├── Contact.cshtml
│   ├── Auth/
│   │   ├── Login.cshtml
│   │   ├── Callback.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Members/
│   │   └── Dashboard.cshtml
│   ├── Officers/
│   │   ├── Dashboard.cshtml
│   │   ├── Members.cshtml
│   │   ├── EditMember.cshtml
│   │   ├── Emails.cshtml
│   │   ├── MeetingNotes.cshtml
│   │   └── Files.cshtml
│   ├── Webhooks/
│   │   └── Stripe.cshtml
│   └── Dev/
│       └── Seed.cshtml
├── Services/
│   ├── EmailService.cs
│   ├── ReminderService.cs
│   └── StripeService.cs
├── wwwroot/
│   ├── css/
│   └── js/
├── appsettings.json
└── Program.cs
```

---

## 11. Go-Live Checklist

```
Before launch
├── [ ] Download all photos from current Squarespace site
├── [ ] Get access to Network Solutions account
├── [ ] Consider transferring danceatx.org to Cloudflare
├── [ ] Club gets email address (info@danceatx.org)
├── [ ] Set up SMTP2GO account with club email
├── [ ] Create live Stripe account under club email
├── [ ] Create Google OAuth app under club Google account
├── [ ] Set up Google Workspace or Zoho Mail for club email

Build
├── [ ] Complete all Razor Pages (public + member + officer)
├── [ ] Wire up Stripe live keys
├── [ ] Wire up SMTP2GO for real email sending
├── [ ] Test full payment → member creation → login flow
├── [ ] Test expiry reminder emails
├── [ ] Test officer broadcast email
├── [ ] Test file upload (meeting notes, club files)
├── [ ] Migrate content from current site
├── [ ] Delete /Dev/Seed page

Deploy to VPS
├── [ ] Spin up DigitalOcean droplet (Ubuntu 24.04)
├── [ ] Secure server (non-root user, SSH keys, UFW firewall)
├── [ ] Install .NET 8
├── [ ] Install PostgreSQL
├── [ ] Install Nginx
├── [ ] Deploy app
├── [ ] Run EF Core migrations on production DB
├── [ ] Add founding officers to Members table
├── [ ] Configure Nginx reverse proxy
├── [ ] Run Certbot for SSL (HTTPS)
├── [ ] Test at raw VPS IP address
├── [ ] Register Stripe webhook with production URL
├── [ ] Add production URL to Google OAuth Console

Go live
├── [ ] Update DNS A record at Network Solutions → VPS IP
├── [ ] Wait for DNS propagation (up to 24 hours)
├── [ ] Verify danceatx.org loads new site
├── [ ] Test login, payment, emails on live site
└── [ ] Cancel Squarespace subscription
```

---

## 12. Open Decisions

| Decision | Status | Notes |
|---|---|---|
| Membership: auto-renew vs one-time vs both | **Undecided** | Current site offers both at $50 |
| Club email address | **Pending** | Need Network Solutions access first |
| Email hosting | **Pending** | Zoho free vs Google Workspace $6/mo |
| Domain transfer to Cloudflare | **Pending** | Recommended but not urgent |
| hillcountrywebco.com email | **Pending** | Google Workspace when ready to go professional |

---

*Document maintained by Hill Country Web Co.*
*For questions: hillcountryweb.atx@gmail.com*
