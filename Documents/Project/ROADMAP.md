# AbdClub Development Roadmap

> Living project plan. Historical architecture and earlier decisions remain in
> `abd_project_decisions.md`; this roadmap is the current source of truth for
> remaining work.

## Short Term — Finish the App

- [ ] Build remaining pages
  - [x] EditMember
  - [ ] Emails
  - [ ] MeetingNotes
  - [ ] Files
- [x] Wire up SMTP email sending and sandbox rerouting
- [x] Test Stripe payments with sandbox
- [x] Test expiry reminder emails
- [x] Sign out flow

## Current Priority — ABD Board UAT

### 1. Provision and secure the VPS

- [ ] Spin up a 2GB DigitalOcean Ubuntu 24.04 droplet
- [ ] Secure the server: non-root user, SSH keys, automatic updates, and UFW
- [ ] Install ASP.NET Core Runtime 10, PostgreSQL, and Nginx
- [ ] Configure recurring PostgreSQL and uploaded-file backups

### 2. Establish the two sites

- [ ] Create `hillcountrywebco.com` as the public Hill Country Web Co. portfolio site
- [ ] Create `abd-demo.hillcountrywebco.com` as the ABD board UAT site
- [ ] Configure separate Nginx sites and systemd services for the portfolio and ABD UAT app
- [ ] Add an `A` record for `abd-demo.hillcountrywebco.com` pointing to the DigitalOcean VPS
- [ ] Run Certbot and verify HTTPS for `abd-demo.hillcountrywebco.com`
- [ ] Display a prominent **UAT / Demo** banner on every ABD demo page

### 3. Create an isolated UAT data environment

- [ ] Create a separate UAT PostgreSQL database using only synthetic member data
- [ ] Use the database name `abdclub_staging`
- [ ] Create a dedicated least-privilege application database user for UAT
- [ ] Apply all EF Core migrations to the UAT database
- [ ] Import only sanitized data; do not copy live email addresses, payment credentials, or private files
- [ ] Verify that member-number sequences, officer accounts, event types, ticketing, and announcements work after migration

### 4. Configure UAT email

- [ ] Create an SMTP2GO account and verify `hillcountrywebco.com` as a sending domain
- [ ] Use SMTP2GO only as the AbdClub application's automated outbound relay
- [ ] Use `admin@hillcountrywebco.com` as the temporary UAT From address
- [ ] Use the existing Zoho group `danceatx@hillcountrywebco.com` as the UAT receiving destination
- [ ] Confirm the appropriate internal Zoho users belong to the `danceatx@hillcountrywebco.com` group
- [ ] Enable `EmailTesting:EnforceSandboxReroute` in UAT
- [ ] Set `EmailTesting:RedirectTargetAddress` to `danceatx@hillcountrywebco.com`
- [ ] Allow only the `hillcountrywebco.com` recipient domain in UAT
- [ ] Store SMTP2GO credentials in server environment variables, not committed settings files
- [ ] Test welcome, membership-status, expiration-reminder, officer-reminder, broadcast, and ticket-confirmation emails
- [ ] Confirm that attempted delivery to any non-Hill-Country-Web-Co. address is rerouted

### 5. Configure UAT Stripe

- [ ] Create or select the Stripe **test-mode** configuration for UAT
- [ ] Add the UAT webhook endpoint at `https://abd-demo.hillcountrywebco.com/Webhooks/Stripe`
- [ ] Store the test publishable key, secret key, and webhook secret as server environment variables
- [ ] Verify individual and two-person membership checkout
- [ ] Verify special-event ticket purchase, repeated-webhook protection, check-in, and refund handling
- [ ] Confirm no Stripe live-mode keys are present in UAT

### 6. Configure UAT officer authentication

- [ ] Create a separate Google OAuth client for the ABD demo site
- [ ] Add `https://abd-demo.hillcountrywebco.com/signin-google` as an authorized redirect URI
- [ ] Store the UAT Google client ID and secret as server environment variables
- [ ] Create controlled UAT officer accounts linked to synthetic member records
- [ ] Verify Officer, Admin, and Tech Admin authorization separately
- [ ] Confirm ordinary members cannot log in or access officer pages

### 7. Configure and deploy the application

- [ ] Configure UAT-only secrets: Stripe test keys/webhook, email provider, and Google OAuth callback URL
- [ ] Set `App:BaseUrl` to `https://abd-demo.hillcountrywebco.com`
- [ ] Publish the Release build and deploy it under the ABD UAT systemd service
- [ ] Configure Nginx reverse proxy headers, upload limits, and HTTPS redirection
- [ ] Confirm the application restarts automatically after a server reboot
- [ ] Remove or restrict development-only pages and endpoints before deployment

### 8. UAT safety and acceptance checks

- [ ] Verify the UAT database connection cannot reach the production database
- [ ] Verify every outbound email is rerouted to the Zoho UAT group
- [ ] Verify all payment pages clearly use Stripe test mode
- [ ] Verify uploaded test files are stored separately from future production files
- [ ] Test membership, member-status email, officer administration, events, announcements, ticket sales, refunds, and exports
- [ ] Review application and Nginx logs for errors without exposing secrets
- [ ] Give the ABD board demo accounts and gather UAT feedback
- [ ] Record UAT issues and obtain board approval before production preparation

## Production Go Live

- [ ] Get Network Solutions access
- [ ] Consider transferring to Cloudflare
- [ ] Create an ABD-owned email organization for `danceatx.org`
- [ ] Create permanent role addresses and establish ABD-controlled administration and recovery
- [ ] Continue using SMTP2GO for application-generated email; use ABD mailboxes for human correspondence
- [ ] Create separate production database, secrets, Stripe live webhook, and Google OAuth callback URL
- [ ] Deploy ABD production at `danceatx.org`
- [ ] Update the `danceatx.org` DNS A record to the VPS IP
- [ ] Run Certbot for `danceatx.org`
- [ ] Disable UAT email rerouting only in production after sender-domain verification
- [ ] Verify production login, payments, emails, ticketing, monitoring, and backups
- [ ] Retire Squarespace
