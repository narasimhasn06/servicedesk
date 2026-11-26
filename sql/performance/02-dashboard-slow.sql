-- Day 9 — the "before" query: top 10 customers by paid revenue, last 30 days.
-- Written the way a first draft often looks. It is correct, but:
--   * date("StartsAt") wraps the column in a function, so no index on StartsAt can be used
--   * it joins every matching booking to its customer BEFORE grouping
EXPLAIN (ANALYZE, BUFFERS)
SELECT c."Name", c."Email", sum(b."Amount") AS revenue, count(*) AS paid_bookings
FROM "Customers" c
JOIN "Bookings" b ON b."CustomerId" = c."Id"
WHERE b."PaymentStatus" = 'Paid'
  AND date(b."StartsAt") >= current_date - 30
  AND date(b."StartsAt") <  current_date
GROUP BY c."Id", c."Name", c."Email"
ORDER BY revenue DESC, c."Id"
LIMIT 10;