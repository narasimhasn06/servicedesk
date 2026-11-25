# Automations (n8n)

Workflows that connect the website form, the ServiceDesk API and email, so no lead is copied by hand.

| Workflow | File | What it does | Status |
|---|---|---|---|
| Lead Intake | [`lead-intake.json`](lead-intake.json) | Website form → validated lead in the API → instant welcome email | ✅ Done |
| CRM sync + alerts | — | Leads to Google Sheets, Telegram alert, error workflow | ⏳ Day 7 |
| Daily report | — | Morning summary of leads, bookings and revenue | ⏳ Day 8 |

## Lead Intake

![Lead Intake workflow](lead-intake.png)

**Problem:** enquiries from the website sat in an inbox until someone copied them into a spreadsheet, and replies took hours.

**Result:** every submission is cleaned, checked, saved as a lead (and customer) through the API, and answered by email within seconds.

### Flow

1. **On form submission** (Form Trigger): "Book a free discovery call" form with Name, Email, Phone, Service and Message.
2. **Prepare lead** (Edit Fields): trims values, lower-cases the email, prefixes the message with the chosen service, sets `source = website-form`.
3. **Looks valid?** (If): message at least 10 characters and name at least 2; invalid submissions stop here and never reach the API.
4. **Wake up API** (HTTP GET `/health`): wakes the free-plan Render service (cold start up to ~1 minute); 60 s timeout, 5 retries, continues on error.
5. **Create lead in ServiceDesk** (HTTP POST `/api/leads/intake`, `X-Api-Key` header auth): finds or creates the customer by email and creates the lead in one transaction; 5 retries. Returns `leadId`, `customerId`, `isNewCustomer`.
6. **Welcome email** (SMTP): personalised reply with the lead reference number.

### Run it yourself

1. Start n8n locally from the repo root: `docker compose up -d` (n8n at http://localhost:5678; set `N8N_ENCRYPTION_KEY` in `.env` first, see `.env.example`).
2. In n8n: **Workflows → Import from file** → `lead-intake.json`.
3. Create two credentials (they are not included in the export):
   - **Header Auth**: name `X-Api-Key`, value = your ServiceDesk API key → select it in *Create lead in ServiceDesk*.
   - **SMTP**: e.g. Gmail with an app password → select it in *Welcome email*, and set your own *From* address.
4. If you run the API locally, change the two URLs from the Render address to your local one.
5. **Publish** the workflow and open the form's **Production URL**.

### Tested scenarios

| Scenario | Expected result |
|---|---|
| New email | New customer + lead, `isNewCustomer: true`, welcome email sent |
| Same email again | Same `customerId`, new `leadId`, `isNewCustomer: false` |
| Message under 10 characters | Stops at *Looks valid?*; no API call, no email |
| API asleep (cold start) | *Wake up API* waits/retries, then the lead is created |
