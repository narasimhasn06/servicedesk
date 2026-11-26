-- Day 9 — proof that the fast query returns exactly the same rows as the slow one.
-- Expected: rows_only_in_slow = 0, rows_only_in_fast = 0, rows_each = 10
WITH slow AS (
    SELECT c."Name", c."Email", sum(b."Amount") AS revenue, count(*) AS paid_bookings
    FROM "Customers" c
    JOIN "Bookings" b ON b."CustomerId" = c."Id"
    WHERE b."PaymentStatus" = 'Paid'
      AND date(b."StartsAt") >= current_date - 30
      AND date(b."StartsAt") <  current_date
    GROUP BY c."Id", c."Name", c."Email"
    ORDER BY revenue DESC, c."Id"
    LIMIT 10
),
fast AS (
    SELECT c."Name", c."Email", t.revenue, t.paid_bookings
    FROM (
        SELECT "CustomerId", sum("Amount") AS revenue, count(*) AS paid_bookings
        FROM "Bookings"
        WHERE "PaymentStatus" = 'Paid'
          AND "StartsAt" >= current_date - 30
          AND "StartsAt" <  current_date
        GROUP BY "CustomerId"
        ORDER BY revenue DESC, "CustomerId"
        LIMIT 10
    ) t
    JOIN "Customers" c ON c."Id" = t."CustomerId"
)
SELECT (SELECT count(*) FROM (SELECT * FROM slow EXCEPT ALL SELECT * FROM fast) x) AS rows_only_in_slow,
       (SELECT count(*) FROM (SELECT * FROM fast EXCEPT ALL SELECT * FROM slow) y) AS rows_only_in_fast,
       (SELECT count(*) FROM slow) AS rows_each;
