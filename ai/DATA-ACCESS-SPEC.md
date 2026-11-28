# AI lead triage — data-access specification

**Feature:** suggest a priority, a service, a one-line summary and a draft reply for each new enquiry.
**Owner:** Bright Path Coaching (demo project). **Last reviewed:** 28 November 2026.

## What the AI receives

| Sent | Why |
|---|---|
| The enquiry message, **after masking**: email addresses → `[email]`, any run of 10+ digits (phone numbers, ID-like numbers) → `[phone]` | It is the text that needs classifying |
| The list of services offered (Career coaching, Relationship coaching, Work-life balance, Group workshop) | So the answer uses the business's own categories |
| Fixed instructions, versioned (current: prompt **v2**) | Consistent, auditable behaviour; the version is recorded with every call |

Masking happens inside the API before the prompt is built ([`PiiMasker`](../api/src/ServiceDesk.Api/Ai/PiiMasker.cs)),
and is covered by unit tests, including one that checks what the model actually receives.

## What the AI never receives

The customer's name, email address and phone number fields; customer, lead or booking ids;
booking or payment data; other customers' enquiries; any API key.

## What comes back, and how it is checked

JSON in a fixed schema generated from a C# record:

| Field | Rule enforced by the API |
|---|---|
| `priority` | Must be `hot`, `warm` or `cold`; anything else is rejected |
| `service` | Must be one of the offered services; otherwise stored as `Other` |
| `summary` | One sentence; the prompt forbids names, emails and phone numbers |
| `draftReply` | Under 80 words; empty for spam or off-topic enquiries |

An answer that does not fit is never guessed into shape.

## Failure behaviour

| Situation | Behaviour |
|---|---|
| Answer doesn't fit the schema or the rules | Retried once |
| No answer within 15 seconds | Retried once (15 s chosen from measurements: every successful answer took ≤ 11.7 s) |
| Provider error (down, quota, bad key) | Retried once |
| Still failing after the retry | Lead marked **NeedsHumanReview**; the owner's Telegram alert says so; nothing is blocked or lost |
| The API itself unreachable from n8n | The owner's alert still arrives, marked "AI: not available" |

## Human control

The AI only **suggests**. Nothing is sent to a customer automatically: the draft reply is a starting
point the owner edits and sends. Priority only orders the owner's work; the AI never discards a lead.

## Logging and retention

- Logged per AI attempt (table `AiCalls`): time, model, prompt version, attempt number, outcome
  (`ok` / `invalid` / `timeout` / `provider-error`), token counts, duration. **Never** the enquiry text
  or the AI's answer.
- Application logs: lead id, outcome, priority, timing and *how many* contact details were masked —
  never the values.
- Stored on the lead: the AI's suggestion (priority, service, summary, draft reply, status, time) until
  the lead is deleted.
- Usage report: `GET /api/ai/usage?days=N` (calls by outcome, tokens, average and p95 duration).

## Provider

Google Gemini (`gemini-3.1-flash-lite`) through its OpenAI-compatible API, behind the provider-neutral
`IChatClient` interface (Microsoft.Extensions.AI).

**This demo uses the free tier, where the provider may use requests to improve its products, so only
fictional data is ever sent.** A real deployment uses a paid tier under a data-processing agreement, or
another provider; switching is a configuration change.

## Quality

Measured on a hand-labelled set of 20 enquiries ([`eval/`](eval)), through an endpoint that saves nothing:

| Prompt | Priority | Service |
|---|---|---|
| v1 | 19/20 | 20/20 |
| v2 | 20/20 | 20/20 |

Re-run `tools/run-ai-eval.ps1` after every prompt or model change, and add new real-world examples to
the set regularly so the score keeps meaning something.
