# AbdClub Development Roadmap

## Short Term — Finish the App
- [ ] Build remaining pages
  - [x] EditMember
  - [ ] Emails
  - [ ] MeetingNotes
  - [ ] Files
- [x] Wire up Resend (or SMTP2GO) for real email sending
- [x] Test Stripe payments with sandbox
- [x] Test expiry reminder emails
- [x] Sign out flow

## Medium Term — Portfolio Site and ABD UAT on the VPS
- [ ] Spin up a 2GB DigitalOcean Ubuntu 24.04 droplet
- [ ] Secure the server: non-root user, SSH keys, automatic updates, and UFW
- [ ] Install ASP.NET Core Runtime 10, PostgreSQL, and Nginx
- [ ] Create `hillcountrywebco.com` as the public Hill Country Web Co. portfolio site
- [ ] Create `abd-demo.hillcountrywebco.com` as the ABD board UAT site
- [ ] Configure separate Nginx sites and systemd services for the portfolio and ABD UAT app
- [ ] Create a separate UAT PostgreSQL database using only synthetic member data
- [ ] Configure UAT-only secrets: Stripe test keys/webhook, email provider, and Google OAuth callback URL
- [ ] Run Certbot and test the HTTPS UAT hostname
- [ ] Remove or restrict development-only pages and endpoints before deployment
- [ ] Give the ABD board demo accounts and gather UAT feedback

## Production Go Live
- [ ] Get Network Solutions access
- [ ] Consider transferring to Cloudflare
- [ ] Create separate production database, secrets, Stripe live webhook, and Google OAuth callback URL
- [ ] Deploy ABD production at `danceatx.org`
- [ ] Update the `danceatx.org` DNS A record to the VPS IP
- [ ] Run Certbot for `danceatx.org`
- [ ] Verify production login, payment, emails, and backups
- [ ] Retire Squarespace
