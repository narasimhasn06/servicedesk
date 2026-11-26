-- Day 9 — the "after" query: same result, faster.
--   * plain range on "StartsAt" (no function on the column) → an index can be used
--   * group and pick the top 10 in "Bookings" first, then join only those 10 customers
-- With the covering index from the migration, Postgres answers it from the index alone.
EXPLAIN (ANALYZE, BUFFERS)
WITH top_paid AS (
    SELECT "CustomerId", sum("Amount") AS revenue, count(*) AS paid_bookings
    FROM "Bookings"
    WHERE "PaymentStatus" = 'Paid'
      AND "StartsAt" >= current_date - 30
      AND "StartsAt" <  current_date
    GROUP BY "CustomerId"
    ORDER BY revenue DESC, "CustomerId"
    LIMIT 10
)
SELECT c."Name", c."Email", t.revenue, t.paid_bookings
FROM top_paid t
JOIN "Customers" c ON c."Id" = t."CustomerId"
ORDER BY t.revenue DESC, t."CustomerId";