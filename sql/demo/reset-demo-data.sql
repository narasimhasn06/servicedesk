-- =====================================================================
-- ServiceDesk demo data reset (Bright Path Coaching)
--
-- DELETES every customer, lead and booking, restarts the IDs at 1, and
-- inserts a small, realistic demo set. All people and emails are fake
-- (@example.com). Dates are relative to the moment you run it, so
-- "yesterday" in the daily report always has data:
--
--   Yesterday (India time): 5 new leads (website-form: 2, google: 1,
--   instagram: 1, referral: 1), 3 new customers, 4 bookings (3 paid),
--   revenue Rs 10,000, pending Rs 2,500. Open (New) leads overall: 4.
--
-- Run it in the Neon SQL Editor (or psql) against the database you want
-- to reset. Take a Neon backup branch first.
-- =====================================================================

BEGIN;

TRUNCATE TABLE "Leads", "Bookings", "Customers" RESTART IDENTITY;

WITH base AS (
    -- midnight today, India time (a timestamp without time zone)
    SELECT date_trunc('day', now() AT TIME ZONE 'Asia/Kolkata') AS today_ist
),
-- days = days relative to today (negative = past), hm = India time of day
c_data (name, email, phone, days, hm) AS (
    VALUES
    ('Priya Sharma',   'priya.sharma@example.com',   '+91 90000 10001', -12, '10:05'),
    ('Arjun Mehta',    'arjun.mehta@example.com',    '+91 90000 10002', -11, '18:40'),
    ('Kavya Reddy',    'kavya.reddy@example.com',    '+91 90000 10003',  -9, '09:20'),
    ('Rahul Verma',    'rahul.verma@example.com',    '+91 90000 10004',  -8, '13:15'),
    ('Sneha Patil',    'sneha.patil@example.com',    '+91 90000 10005',  -6, '20:30'),
    ('Vikram Singh',   'vikram.singh@example.com',   '+91 90000 10006',  -5, '11:45'),
    ('Divya Menon',    'divya.menon@example.com',    '+91 90000 10007',  -4, '16:10'),
    ('Karthik Nair',   'karthik.nair@example.com',   '+91 90000 10008',  -3, '08:55'),
    ('Meera Iyer',     'meera.iyer@example.com',     '+91 90000 10009',  -2, '19:25'),
    ('Rohan Desai',    'rohan.desai@example.com',    '+91 90000 10010',  -1, '09:15'),
    ('Pooja Kulkarni', 'pooja.kulkarni@example.com', '+91 90000 10011',  -1, '12:40'),
    ('Harsha Gowda',   'harsha.gowda@example.com',   '+91 90000 10012',  -1, '17:30')
),
ins_c AS (
    INSERT INTO "Customers" ("Name", "Email", "Phone", "CreatedAt")
    SELECT c.name, c.email, c.phone,
           (b.today_ist + make_interval(days => c.days) + c.hm::interval) AT TIME ZONE 'Asia/Kolkata'
    FROM c_data c CROSS JOIN base b
    ORDER BY c.days, c.hm
    RETURNING "Id", "Email"
),
l_data (email, message, source, status, days, hm) AS (
    VALUES
    -- older leads, already worked on by the owner
    ('priya.sharma@example.com',   'Interested in 1:1 career coaching, 4 sessions.',                    'website-form', 'Qualified', -12, '10:05'),
    ('arjun.mehta@example.com',    'Asking about group workshop pricing for my team.',                  'google',       'Lost',      -11, '18:40'),
    ('kavya.reddy@example.com',    'A friend recommended you. Need help with a job switch in IT.',      'referral',     'Qualified',  -9, '09:20'),
    ('rahul.verma@example.com',    'Looking for help with work-life balance.',                          'website-form', 'Contacted',  -8, '13:15'),
    ('sneha.patil@example.com',    'Do you offer weekend sessions for students?',                       'instagram',    'Lost',       -6, '20:30'),
    ('vikram.singh@example.com',   'Want leadership coaching before a promotion review.',               'website-form', 'Qualified',  -5, '11:45'),
    ('divya.menon@example.com',    'Relationship coaching for couples - what does a package include?',  'google',       'Contacted',  -4, '16:10'),
    ('meera.iyer@example.com',     'Struggling with interview anxiety, need 2-3 sessions.',             'website-form', 'New',        -2, '19:25'),
    -- yesterday's leads (these make the daily report)
    ('rohan.desai@example.com',    'Planning a career change to product management. Can we talk?',      'website-form', 'New',        -1, '09:15'),
    ('karthik.nair@example.com',   'Referred by Kavya. Interested in the 4-session career package.',    'referral',     'Qualified',  -1, '10:50'),
    ('pooja.kulkarni@example.com', 'Saw your reel on burnout. Do you take online clients?',             'instagram',    'New',        -1, '12:40'),
    ('priya.sharma@example.com',   'Back again - would like 4 more sessions after the first round.',   'website-form', 'New',        -1, '15:20'),
    ('harsha.gowda@example.com',   'Found you on Google. Need help preparing for a salary negotiation.','google',       'Contacted',  -1, '17:30')
),
ins_l AS (
    INSERT INTO "Leads" ("CustomerId", "Message", "Source", "Status", "CreatedAt")
    SELECT c."Id", l.message, l.source, l.status,
           (b.today_ist + make_interval(days => l.days) + l.hm::interval) AT TIME ZONE 'Asia/Kolkata'
    FROM l_data l
    JOIN ins_c c ON c."Email" = l.email
    CROSS JOIN base b
    ORDER BY l.days, l.hm
    RETURNING 1
),
-- starts = session start (days from today, IST time); days/hm = when the booking was made
k_data (email, amount, status, starts_days, starts_hm, days, hm) AS (
    VALUES
    ('priya.sharma@example.com',  3500.00, 'Paid',    -7, '10:00', -10, '11:00'),
    ('kavya.reddy@example.com',   4000.00, 'Paid',    -5, '18:00',  -8, '14:30'),
    ('rahul.verma@example.com',   2500.00, 'Pending',  2, '11:00',  -6, '10:15'),
    ('vikram.singh@example.com',  2500.00, 'Paid',     1, '17:00',  -4, '12:00'),
    -- yesterday's bookings: 3 paid (Rs 10,000) + 1 pending (Rs 2,500)
    ('harsha.gowda@example.com',  2500.00, 'Paid',     2, '19:00',  -1, '18:05'),
    ('karthik.nair@example.com',  4000.00, 'Paid',     3, '10:00',  -1, '11:30'),
    ('priya.sharma@example.com',  3500.00, 'Paid',     4, '10:00',  -1, '16:00'),
    ('meera.iyer@example.com',    2500.00, 'Pending',  5, '18:30',  -1, '20:10')
),
ins_b AS (
    INSERT INTO "Bookings" ("CustomerId", "StartsAt", "Amount", "PaymentStatus", "CreatedAt")
    SELECT c."Id",
           (b.today_ist + make_interval(days => k.starts_days) + k.starts_hm::interval) AT TIME ZONE 'Asia/Kolkata',
           k.amount, k.status,
           (b.today_ist + make_interval(days => k.days) + k.hm::interval) AT TIME ZONE 'Asia/Kolkata'
    FROM k_data k
    JOIN ins_c c ON c."Email" = k.email
    CROSS JOIN base b
    ORDER BY k.days, k.hm
    RETURNING 1
)
SELECT (SELECT count(*) FROM ins_c) AS customers_inserted,
       (SELECT count(*) FROM ins_l) AS leads_inserted,
       (SELECT count(*) FROM ins_b) AS bookings_inserted;

COMMIT;

-- ---------------------------------------------------------------------
-- Check: yesterday's numbers (should match GET /api/reports/daily)
-- Expected: 5 leads | 3 customers | 4 bookings | 3 paid | 10000.00 | 2500.00 | 4 open
-- ---------------------------------------------------------------------
WITH r AS (
    SELECT (date_trunc('day', now() AT TIME ZONE 'Asia/Kolkata') - interval '1 day') AT TIME ZONE 'Asia/Kolkata' AS f,
           date_trunc('day', now() AT TIME ZONE 'Asia/Kolkata') AT TIME ZONE 'Asia/Kolkata' AS t
)
SELECT
  (SELECT count(*) FROM "Leads", r WHERE "CreatedAt" >= f AND "CreatedAt" < t)                         AS new_leads,
  (SELECT count(*) FROM "Customers", r WHERE "CreatedAt" >= f AND "CreatedAt" < t)                     AS new_customers,
  (SELECT count(*) FROM "Bookings", r WHERE "CreatedAt" >= f AND "CreatedAt" < t)                      AS new_bookings,
  (SELECT count(*) FROM "Bookings", r WHERE "CreatedAt" >= f AND "CreatedAt" < t AND "PaymentStatus" = 'Paid') AS paid_bookings,
  (SELECT coalesce(sum("Amount"), 0) FROM "Bookings", r WHERE "CreatedAt" >= f AND "CreatedAt" < t AND "PaymentStatus" = 'Paid')    AS revenue,
  (SELECT coalesce(sum("Amount"), 0) FROM "Bookings", r WHERE "CreatedAt" >= f AND "CreatedAt" < t AND "PaymentStatus" = 'Pending') AS pending,
  (SELECT count(*) FROM "Leads" WHERE "Status" = 'New')                                                AS open_leads;
