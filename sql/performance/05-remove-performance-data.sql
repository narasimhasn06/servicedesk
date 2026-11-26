-- Day 9 — optional clean-up: removes the 10,000 perf customers and their bookings
-- from your LOCAL database. Your normal demo rows stay.
DELETE FROM "Bookings"  WHERE "CustomerId" IN (SELECT "Id" FROM "Customers" WHERE "Email" LIKE 'perf.%@example.com');
DELETE FROM "Customers" WHERE "Email" LIKE 'perf.%@example.com';
VACUUM ANALYZE "Bookings";
VACUUM ANALYZE "Customers";
SELECT (SELECT count(*) FROM "Customers") AS customers, (SELECT count(*) FROM "Bookings") AS bookings;
