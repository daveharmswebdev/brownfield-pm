---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments:
  - docs/index.md
  - docs/api-contracts-backend.md
  - docs/data-models-backend.md
  - docs/architecture-backend.md
  - docs/architecture-frontend.md
  - docs/component-inventory-frontend.md
  - docs/integration-architecture.md
  - docs/source-tree-analysis.md
date: 2025-12-28
author: Dave
---

# Product Brief: brownfield-pm

## Executive Summary

Property Manager is a purpose-built expense tracking application for small landlords who manage 1-3 rental properties. Born from personal need, the application solves the universal landlord frustration: spreadsheet chaos that makes tax time stressful and error-prone.

The core insight is simple but powerful - IRS Schedule E categories are built in from day one. Every expense logged throughout the year automatically maps to the correct tax category, eliminating guesswork and ensuring nothing falls through the cracks. When tax time arrives, landlords export a clean Schedule E summary in seconds rather than spending hours reconciling messy spreadsheets.

Currently serving the founder's personal property portfolio, the application is architected for SaaS scalability with multi-tenant support already in place. The roadmap extends beyond tax preparation into comprehensive property management: vendor management, lease preparation, receipt capture, and bank synchronization.

---

## Core Vision

### Problem Statement

Small landlords managing 1-3 rental properties track expenses in spreadsheets throughout the year. By tax time, these spreadsheets have become disorganized, categories are inconsistent, and landlords face hours of reconciliation work. They risk missing legitimate deductions or making categorization errors that could trigger audit concerns.

### Problem Impact

- **Time lost**: Hours spent at year-end sorting and categorizing expenses
- **Money left on table**: Missed deductions due to poor tracking
- **Stress and anxiety**: Tax time becomes a dreaded annual ordeal
- **Error risk**: Incorrect categorization could raise IRS flags

### Why Existing Solutions Fall Short

- **Generic accounting software** (QuickBooks, FreshBooks): Overkill for small landlords, steep learning curve, not optimized for Schedule E
- **Spreadsheets**: No structure, no category enforcement, no reporting
- **Landlord-specific tools**: Often too complex, subscription fatigue, still require category mapping

None of these solutions speak the language of Schedule E from the start.

### Proposed Solution

Property Manager is a streamlined expense and income tracking application where IRS Schedule E categories are first-class citizens. Landlords log expenses throughout the year with minimal friction, knowing every entry maps directly to the correct tax line. When tax time arrives, a clean Schedule E summary exports in seconds.

The application starts focused: properties, expenses, income, and tax-ready reporting. Future expansion includes receipt capture, bank synchronization, vendor management, lease preparation, and potentially tax filing integration.

### Key Differentiators

1. **Schedule E Native**: All 15 IRS expense categories built-in from day one - no setup, no mapping
2. **Right-Sized Simplicity**: Purpose-built for small landlords, not downsized enterprise software
3. **Tax Time Magic**: The payoff moment - export a clean Schedule E summary in seconds
4. **SaaS-Ready Architecture**: Multi-tenant foundation supports growth from personal use to commercial product
5. **Founder-User Alignment**: Built by a landlord for landlords - every feature solves a real problem

---

## Target Users

### Primary Users

#### Persona: "Sarah" - The Household Expense Tracker

**Profile:**
- Partner/spouse in a household that owns 1-3 rental properties
- Handles the day-to-day expense tracking as part of household responsibilities
- Moderate tech comfort - uses spreadsheets but doesn't love them
- Not an accountant, but organized and diligent

**Current Behavior:**
- Saves receipts throughout the week (physical and digital)
- Weekly logging session to enter expenses into spreadsheet
- Struggles most with categorization - "Is this Repairs or Maintenance?"
- When unsure, guesses or asks spouse, creating friction and uncertainty

**Pain Points:**
- Categorization decisions slow her down and create doubt
- No confidence that expenses are in the right Schedule E buckets
- Fear of missing deductions or making errors that matter at tax time
- Tedious data entry with no payoff until year-end

**Success Vision:**
- Log an expense in under 30 seconds with clear category guidance
- Never wonder "which category?" - the app makes it obvious
- Hand off clean, organized data at tax time with confidence

---

#### Persona: "Mike" - The DIY Solo Landlord

**Profile:**
- Owns 1-2 rental properties as investment/side income
- Manages everything themselves - no property manager
- Does own taxes with TurboTax or similar
- Currently uses spreadsheets, maybe tried QuickBooks and abandoned it

**Current Behavior:**
- Sporadic expense tracking - logs when remembers, catches up quarterly
- Dreads tax time reconciliation
- Has missed deductions because receipts got lost or miscategorized

**Pain Points:**
- Existing tools are overkill (AppFolio, Buildium) or underkill (spreadsheets)
- No system designed for Schedule E specifically
- Doesn't want to learn accounting software for 2 properties

**Success Vision:**
- Simple tool that "just works" for rental expenses
- Export Schedule E data and be done in minutes, not hours

---

### Secondary Users

#### Persona: "Dave" - The Tax Time Closer

**Profile:**
- Spouse/partner of the primary expense tracker
- Handles tax filing or interfaces with CPA
- May also be the property decision-maker (repairs, improvements)
- In some cases, also the developer/builder of the system

**Role:**
- Reviews expense data for accuracy before tax filing
- Generates Schedule E reports or exports data
- Needs visibility into what's been tracked without doing the tracking

**Needs:**
- Dashboard view of year-to-date expenses by property and category
- Confidence that categorization was done correctly
- Clean export that maps directly to Schedule E lines

---

### Out of Scope Users

- **Large Portfolio Landlords (10+ properties)**: Need full property management (AppFolio, Buildium)
- **Property Management Companies**: Different workflow, tenant-centric needs
- **Commercial Real Estate**: Different tax treatment, not Schedule E

---

### User Journey

**Discovery:** Word of mouth from other small landlords, "my friend built this and it's way better than spreadsheets"

**Onboarding:**
1. Create account, add first property (address, name)
2. See pre-built Schedule E categories - no setup required
3. Log first expense - experience the "that was easy" moment

**Core Usage (Weekly):**
- Sarah sits down with week's receipts
- Logs each expense: amount, date, property, category (from clear dropdown)
- Category descriptions help her choose correctly
- Done in 10 minutes vs. 30 with spreadsheet

**Success Moment (Tax Time):**
- Open app in February
- Click "Export Schedule E Summary"
- See clean report organized by property and line item
- Hand to spouse or import to tax software
- "That's it? We're done?"

**Long-term:**
- Becomes the trusted system of record
- Receipts might get photographed and attached (future feature)
- Year-over-year comparison shows expense trends

---

## Success Metrics

### User Success Metrics

Success for Property Manager is measured by how well it transforms the household tax preparation experience from a stressful year-end scramble into a calm, confident process.

#### Primary Success Indicator: Year-Round Tracking Adoption

| Metric | Target | How Measured |
|--------|--------|--------------|
| Weekly expense logging | Consistent use (at least 3 of 4 weeks/month) | App usage patterns |
| Categorization at entry time | 100% of expenses categorized when logged | No uncategorized expenses |
| Time per logging session | Under 15 minutes for weekly batch | User experience feedback |

#### Tax Time Success Indicator: The "Export and Done" Moment

| Metric | Before (Pain State) | After (Success State) |
|--------|---------------------|----------------------|
| Tax prep time | Hours sorting year of receipts | Under 15 minutes (export + review) |
| Categorization work | Done at tax time from memory | Already done throughout year |
| Accountant handoff | Fill out worksheet manually | Export clean Schedule E summary |
| Confidence level | "I hope this is right" | "I know this is right" |

#### Behavioral Success Signals

- **Sarah prefers the app**: Would not voluntarily return to saving receipts for year-end
- **No category confusion**: Zero "which category is this?" conversations between spouses
- **Scales comfortably**: Adding new properties doesn't increase stress or time proportionally

---

### Business Objectives

*Note: Current phase is personal/family use. Business objectives are intentionally minimal.*

#### Phase 1: Personal Use (Current)

| Objective | Success Criteria |
|-----------|------------------|
| Solve household tax pain | Family uses app for full tax year and exports Schedule E data |
| Prove the concept | First tax season completed with Property Manager is noticeably easier |
| Validate with real use | Identify friction points and improvements through daily use |

#### Phase 2: Friends & Family (Future)

| Objective | Success Criteria |
|-----------|------------------|
| Share with trusted circle | 2-3 friends successfully onboarded and using the app |
| Gather external feedback | Identify needs beyond founder's use case |
| Validate SaaS potential | Friends would pay for this / recommend to others |

---

### Key Performance Indicators

#### Core KPIs (Personal Use Phase)

1. **Adoption KPI**: App used for expense logging every week for 3+ consecutive months
2. **Completeness KPI**: All expenses for all properties logged and categorized
3. **Tax Time KPI**: Schedule E export generated and accepted by accountant without rework
4. **Satisfaction KPI**: Primary user (Sarah) rates experience as better than previous method

#### Leading Indicators

- Expenses logged within 7 days of occurrence (freshness)
- No backlog of uncategorized expenses
- Time from "open app" to "expense saved" under 60 seconds

#### Lagging Indicators

- Tax preparation time reduced by 75%+ vs. previous year
- Accountant receives data in preferred format
- No missed deductions discovered after filing

---

## MVP Scope

### Current State (Already Built)

The application has significant functionality already implemented:

| Component | Status | Notes |
|-----------|--------|-------|
| Properties | Complete | CRUD, address fields, multi-property support |
| Expenses | Complete | CRUD, Schedule E categories, filtering, totals by property |
| Income | Complete | CRUD, per-property tracking, source field |
| Dashboard | Complete | Summary views, year-to-date totals |
| Authentication | Needs Refactor | Currently public registration - needs invite-only |
| Multi-tenancy | Complete | Account isolation, user scoping |

### Core MVP Features (To Be Implemented)

#### 1. Invite-Only Registration (Priority 1)

**Current State:** Public registration allows anyone to create an account

**Required Change:** Users can only be added via email invitation from existing admin/owner

**Acceptance Criteria:**
- Remove public registration endpoint/UI
- Add invitation flow: admin enters email → system sends invite link → invitee completes registration
- Invited users join the inviting user's account (multi-tenant)
- Invitations expire after reasonable timeframe

**Rationale:** Security and access control must be in place before sharing with family/friends

---

#### 2. Schedule E Report (Priority 2)

**Purpose:** Generate tax-ready summary of expenses and income by property and category

**Report Requirements:**
- Filter by tax year (2024, 2025, etc.)
- Group by property
- Show totals per Schedule E category (15 IRS expense categories)
- Include income totals per property
- Calculate net income (income - expenses) per property

**Output Formats:**
- PDF export (for printing/sharing with accountant)
- CSV/Excel export (for accountant to manipulate if needed)

**Acceptance Criteria:**
- User selects tax year
- User clicks "Generate Report" or "Export"
- System produces Schedule E summary matching accountant worksheet format
- Report includes all properties with expense totals by category and income totals
- Export downloads in chosen format (PDF or CSV/Excel)

---

### Out of Scope for MVP

The following features are explicitly deferred to future phases:

| Feature | Rationale | Target Phase |
|---------|-----------|--------------|
| Receipt capture/upload | Nice-to-have, not blocking tax workflow | Phase 2 |
| Bank synchronization | Complex integration, not essential for manual weekly logging | Phase 2 |
| Vendor management | Future expansion beyond expense tracking | Phase 3 |
| Lease preparation | Future expansion into property management | Phase 3 |
| Tax filing integration | Requires third-party integrations | Phase 3+ |
| Year-over-year comparison reports | Valuable but not essential for first tax season | Phase 2 |
| Monthly/quarterly summary reports | Schedule E annual report is the priority | Phase 2 |
| Mobile app | Web responsive is sufficient for MVP | Future |

---

### MVP Success Criteria

The MVP is successful when:

1. **Invite-Only Works:** Dave can invite Sarah (and later, friends) without exposing public registration
2. **Sarah Uses It Weekly:** Primary user adopts the app for actual expense tracking
3. **Tax Time Delivers:** Schedule E report exports cleanly for accountant handoff
4. **Accountant Accepts:** Report format matches expected worksheet, no rework required
5. **Time Savings Realized:** Tax prep takes minutes instead of hours

**Go/No-Go Decision Point:** After first tax season (April 2025), evaluate:
- Did the app deliver on the "export and done" promise?
- Would Sarah voluntarily continue using it?
- Is it worth sharing with friends / considering SaaS?

---

### Future Vision

#### Phase 2: Enhanced Tracking
- Receipt photo capture and attachment to expenses
- Year-over-year expense comparison reports
- Monthly/quarterly summary views
- Bulk expense import (CSV upload)

#### Phase 3: Property Management Expansion
- Vendor management (track contractors, service providers)
- Lease document storage and preparation
- Tenant information (basic, not full tenant management)
- Maintenance request tracking

#### Phase 4: SaaS & Integrations
- Bank/credit card transaction sync
- Tax software integration (TurboTax, etc.)
- Multi-user roles (accountant read-only access)
- Subscription billing for external users

#### Long-term Vision
Property Manager evolves from a personal expense tracker into a lightweight property management suite for small landlords - the "right-sized" alternative to enterprise tools like AppFolio, purpose-built for the 1-5 property owner who wants simplicity without sacrificing tax-time confidence.
