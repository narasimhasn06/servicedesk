-- =====================================================================
-- Day 9 — performance test data (LOCAL database only, never production)
--
-- Adds 10,000 customers and 1,000,000 bookings spread over the last two
-- years (+ 60 days ahead), so slow queries become measurable.
-- Re-runnable: it first removes any rows it added before.
-- Takes about 15–40 seconds.
-- =====================================================================

-- remove a previous run (perf rows only; your normal demo rows stay)
DELETE FROM "Bookings"  WHERE "CustomerId" IN (SELECT "Id" FROM "Customers" WHERE "Email" LIKE 'perf.%@example.com');
DELETE FROM "Customers" WHERE "Email" LIKE 'perf.%@example.com';

-- 10,000 customers
INSERT INTO "Customers" ("Name", "Email", "Phone", "CreatedAt")
SELECT 'Perf Customer ' || g,
       'perf.' || g || '@example.com',
       '+91 98' || lpad(g::text, 8, '0'),
       now() - random() * interval '730 days'
FROM generate_series(1, 10000) AS g;

-- 1,000,000 bookings: 70% Paid, 20% Pending, 7% Refunded, 3% Failed
INSERT INTO "Bookings" ("CustomerId", "StartsAt", "Amount", "PaymentStatus", "CreatedAt")
SELECT c.ids[1 + floor(random() * array_length(c.ids, 1))::int],
       s.starts_at,
       (1500 + 500 * floor(random() * 8))::numeric(10,2),          -- 1,500 to 5,000
       CASE WHEN r < 0.70 THEN 'Paid'
            WHEN r < 0.90 THEN 'Pending'
            WHEN r < 0.97 THEN 'Refunded'
            ELSE 'Failed' END,
       s.starts_at - random() * interval '14 days'
FROM (SELECT array_agg("Id") AS ids FROM "Customers" WHERE "Email" LIKE 'perf.%@example.com') AS c,
     LATERAL (
         SELECT g,
                now() - interval '730 days' + random() * interval '790 days' AS starts_at,
                random() AS r
         FROM generate_series(1, 1000000) AS g
     ) AS s;

-- fresh statistics so the planner knows the new sizes
ANALYZE "Customers";
ANALYZE "Bookings";

SELECT (SELECT count(*) FROM "Customers") AS customers,
       (SELECT count(*) FROM "Bookings")  AS bookings,
       pg_size_pretty(pg_total_relation_size('"Bookings"')) AS bookings_size;