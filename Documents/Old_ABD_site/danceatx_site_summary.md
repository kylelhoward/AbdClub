# Austin Ballroom Dancers — Current Site Summary
> danceatx.org · Built on Squarespace · Reviewed May 2026

---

## Overview

Austin Ballroom Dancers (ABD) is a non-profit, all-volunteer social club
established in 1981, dedicated to bringing ballroom dancing events and
professional instruction to the Austin area at affordable prices. The current
website is built on Squarespace and serves as the club's public-facing
presence. It has no member portal, no member database, and no automated
membership management.

---

## Platform & Technology

| Item | Detail |
|---|---|
| Platform | Squarespace |
| Domain | danceatx.org |
| Registrar | Network Solutions (Domain.com - Network Solutions, LLC) |
| Calendar | Google Calendar (embedded iframe) |
| Payments | Squarespace store for memberships; Square for donations |
| Email list | Squarespace newsletter signup (email address collection only) |
| Social media | Facebook, Instagram |

---

## Navigation Structure

```
Home
Social Dances & Events
Avalon Ball
Learn to Dance
Dance Hosts
Calendar
Membership
Our Supporters
Volunteer
About Us
  ├── History
  ├── Contact Us
  └── ABD By Laws
```

---

## Page-by-Page Content Inventory

### Home
- Tagline: "The Fun Starts Now"
- Brief club description — non-profit, volunteer, ballroom focus
- Dance styles listed: Waltz, Foxtrot, Cha Cha, Rumba, Bolero, West Coast
  Swing, East Coast Swing, Country Western
- Embedded Google Calendar
- Membership purchase links (auto-renewing and one-time, both $50/year)
- Email newsletter signup
- Square donation link

### Social Dances & Events
- Saturday dances held at **Go Dance Studios** on the 4th Saturday of each month
- Mention of the **Avalon Ball** (May 16, 2026 — Catalina Island, CA)
- Embedded Google Calendar
- Three photos: Holiday Party at Hancock, Lessons at Austin Uptown, Members
  at the UT Great Waltz

### Avalon Ball
- Annual trip to the **1929 Art Deco Casino Ballroom** on Catalina Island
- 10,000 sq ft ballroom, live music by the Dean Mora Orchestra, 600-person
  Conga Line
- Next event: **May 16, 2026**
- Contact: Kevin (919) 623-1110
- Cost estimate per person: ~$1,900 total
  (Flight $1,000 · Rideshare $100 · Ferry $100 · Ticket $100 · Hotel $600/night)
- Tickets: $75.72 each via Agile Ticketing (non-refundable)
- Tables: sold in groups of 2 or 4; sell out quickly to Art Deco Society members
- Travel logistics: Southwest Air to Long Beach; Catalina Express ferry
- Organized by the Art Deco Society of Los Angeles

### Learn to Dance
- Partner organization: **Go Dance Studio** (godancestudio.com)
  - 50+ group classes per week at multiple levels
  - Thursday and Sunday focus on ballroom
  - Private instruction available
- Recommended instructors (formerly at Hancock Recreation Center):
  - **Laura LeKander** — Ballroom, Latin, Swing, Country Western, Salsa,
    Wedding First Dance. Contact: lekander.laura@gmail.com
  - **Kerry Kelly** — American, International, Club, Country Western styles.
    Contact: Kerry.Kelly@gmail.com

### Dance Hosts
- Program exists to welcome new attendees and make socials more inclusive
- Hosts are trained in most dances played, wear "Dance Host" name tags
- Both lead and follow hosts available; many can do both roles
- Program funded by donations — currently funded through August 2026
- To become a host: contact a board member or fill out a Google Form
- Hosts are compensated

### Calendar
- Uses an embedded Google Calendar
- Google Calendar ID:
  `a31483a1c2b641276f878a294c56f92665d3881d43fe14ad24a5a8fd3a19b493@group.calendar.google.com`
- Timezone: America/Chicago

### Membership
- Two options, both $50/year:
  - **Annual Membership (Renewing)** — $50 every 12 months, auto-renews
  - **Annual Membership (Non-renewing)** — $50 one-time, starts day of purchase
- Processed via Squarespace store
- No member portal, no member login, no automated expiry reminders

### Our Supporters
- Donor recognition page
- 2025 donors listed:
  - $3,000 — Anonymous
  - $500 — Anonymous
  - $300 — Steve Flannagan (flanic.com/dance)
  - $150 — Anonymous
- Total 2025 donations noted on Volunteer page: over $3,800
- Donations fund: venue rental, dance hosts, live band at holiday dance

### Volunteer
- Opportunities listed:
  1. Make a donation via Square
  2. Welcome new attendees at social dances
  3. Organize social outings
  4. Run for a board position or join election committee
  5. Print and post flyers (PDFs provided)
- Google Form embedded for volunteer interest
- PDF resources linked: Member/Friends Letter, flyers

### History
- Founded: **1981** as a non-profit club
- Mission: affordable ballroom dancing in Austin
- Monthly variety dances on the **4th Saturday** of each month
- Board meets monthly (location/time on calendar)
- Board meetings open to all ABD members

### Contact Us
- Not fetched — likely a contact form

### ABD By Laws
- Linked as a page; likely a PDF or text document

---

## What the Current Site Does Well

- Clean, simple navigation covering all the club's main activities
- Google Calendar integration — low maintenance, always current
- Avalon Ball page is detailed and useful for trip planning
- Dance Hosts page is thorough and well-written
- Learn to Dance page clearly connects visitors to instruction options
- Supporter recognition is a nice touch for donor relations

---

## What the Current Site Is Missing

| Gap | Impact |
|---|---|
| No member login / portal | Members can't self-serve — check status, update info |
| No member database | Officers manage membership manually |
| No automated expiry reminders | Members lapse without notice |
| No broadcast email to members | No direct channel to communicate with membership |
| No officer admin area | No way to manage members, files, or meeting notes online |
| No file storage | Meeting notes, bylaws, forms managed off-site |
| Donations via Square, membership via Squarespace store | Two separate payment systems; no unified transaction history |

---

## Third-Party Services in Use

| Service | Purpose | Keep / Replace |
|---|---|---|
| Squarespace | Website platform | **Replace** with new ASP.NET site |
| Google Calendar | Events calendar | **Keep** — embed in new site |
| Square | Donations | **Keep** — works fine for donations |
| Stripe (planned) | Membership payments | **Replace** Squarespace store |
| Squarespace newsletter | Email list | **Replace** with SMTP2GO + member DB |
| Facebook | Social media | **Keep** — link from new site |
| Instagram | Social media | **Keep** — link from new site |

---

## Content to Migrate to New Site

| Content | Format | Notes |
|---|---|---|
| Home page copy | Text | Rewrite slightly for new design |
| Club history (1981 founding) | Text | Short, copy directly |
| Dance styles list | Text | Copy directly |
| Social dances info | Text | Update venue/schedule as needed |
| Avalon Ball page | Text + links | Rich content, migrate fully |
| Learn to Dance page | Text + emails | Instructor bios, contact links |
| Dance Hosts page | Text | Well written — migrate as-is |
| Volunteer page | Text + links | Include Google Form embed |
| Supporters / donors list | Text | Update annually |
| ABD By Laws | PDF | Host on new site under Files |
| Member/Friends Letter PDF | PDF | Host on new site |
| Event flyer PDFs | PDF | Host on new site |
| Google Calendar embed | iframe | Same calendar ID, new embed |
| Square donation link | URL | Keep square.link/u/j9NDxUHf |
| Photos (3 known) | Images | Download from Squarespace before cancelling |

> **Important:** Download all images from Squarespace before cancelling the
> subscription. Once cancelled, Squarespace CDN links will stop working and
> images will be lost.

---

## Recommended New Site Page Structure

```
Public pages (no login required)
├── / — Home
├── /dances — Social Dances & Events
├── /avalon-ball — Avalon Ball
├── /learn-to-dance — Learn to Dance
├── /dance-hosts — Dance Hosts
├── /calendar — Calendar (Google Calendar embed)
├── /membership — Join / Renew (Stripe)
├── /supporters — Our Supporters
├── /volunteer — Volunteer
├── /history — History / About Us
└── /contact — Contact

Member area (login required)
└── /members/dashboard — Membership status, upcoming events

Officer area (officer login required)
├── /officers/dashboard — Overview
├── /officers/members — Member list + management
├── /officers/emails — Broadcast email
├── /officers/meeting-notes — Upload / view meeting notes
└── /officers/files — Club file storage
```

---

*Summary prepared May 2026 for new site development project.*
