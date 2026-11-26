# SQL performance — making a slow dashboard query fast

**Service:** Database & API work · demo project ("Bright Path Coaching"), measured on a local copy with test data

## The problem

The owner's dashboard shows the top 10 customers by paid revenue over the last 30 days.
With 1,000,000 bookings, the query read the whole table on every page load.

## Result

| Version | Execution time | Data read (8 KB pages) | How Postgres reads |
|---|---|---|---|
| Before | 70.1 ms | 8,906 | Parallel Seq Scan, ~973,000 rows read and thrown away |
| Rewrite only | 30.7 ms | 8,426 | Bitmap Index Scan on `StartsAt`, then the table |
| Rewrite + covering index | 14.3 ms | 203 | Index Only Scan, 0 heap fetches |

**About 5× faster (70.1 → 14.3 ms) and 44× less data read (8,906 → 203 pages)**, with the same
10 rows, proved by a check query. The rewrite halved the time; the covering index removed the table
reads and halved it again.

_Measured with `EXPLAIN (ANALYZE, BUFFERS)` on PostgreSQL in Docker on a laptop, third run of each.
Times vary by machine; the page counts don't._

## What I changed

1. **Made the date filter index-friendly.** `date("StartsAt") >= …` hides the column inside a function,
   so no index can be used. `"StartsAt" >= …` means the same thing and can use one.
2. **Aggregated before joining.** Sum the bookings per customer, keep the top 10, then join only
   those 10 customers instead of ~26,500 booking rows.
3. **Added a covering index** `("PaymentStatus", "StartsAt") INCLUDE ("CustomerId", "Amount")`
   through an EF Core migration: the query is answered from the index without touching the table.

**Trade-off:** the index is ~48 MB per million bookings and adds a little work to every insert and
payment-status update. For a table read on every dashboard load and written a few hundred times a
day, that's worth it. On a busy production table I'd build it with `CREATE INDEX CONCURRENTLY`.

## Also fixed: an N+1 in the CRM sync

The Google Sheets sync fetched the open leads (1 API call), then each lead's customer (1 call per lead).
A new endpoint, `GET /api/leads/with-customer`, returns both in one SQL JOIN (an EF Core projection).

| | API calls for the leads | Whole sync run (2nd run, API awake) |
|---|---|---|
| Before (1 + N, 4 open leads) | 1 + 4 = 5 | 4.36 s |
| After | 1 | 4.06 s |

With only 4 open leads the run is just ~0.3 s faster: most of the run is Google Sheets and the
status write-back. The point is how it scales: the old version adds one HTTPS round trip to the API
(plus a database query) per open lead, so 100 open leads meant 101 calls; now it's always 1.

## Raw SQL where it pays: Dapper

`GET /api/reports/top-customers?days=30&limit=10` runs the tuned query with Dapper: parameterised
(no SQL injection, reusable plan), validated limits, and India-time day boundaries. EF Core stays for
everyday CRUD.

## Reproduce it

All scripts are in [`performance/`](performance), the saved plans in [`performance/plans/`](performance/plans).
Run them against a **local** database only:

1. `01-seed-performance-data.sql`: 10,000 customers + 1,000,000 bookings
2. `02-dashboard-slow.sql` → [`plans/1-before.txt`](performance/plans/1-before.txt)
3. `03-dashboard-fast.sql` → [`plans/2-after-rewrite.txt`](performance/plans/2-after-rewrite.txt);
   after the index migration → [`plans/3-after-index.txt`](performance/plans/3-after-index.txt)
4. `04-check-same-results.sql`: expects `0 | 0 | 10`
5. `05-remove-performance-data.sql`: optional clean-up

On Windows: `Get-Content sql\performance\02-dashboard-slow.sql -Raw | docker exec -i servicedesk-db psql -U servicedesk -d servicedesk`

## Noticed, not yet changed

Lead intake finds customers with `Email.ToLower() == email`. Wrapping the column in a function
stops the unique email index from being used (the same problem as fix 1). Emails are already stored
in lower case by intake, so the next step is to lower-case them on every write and compare directly.
