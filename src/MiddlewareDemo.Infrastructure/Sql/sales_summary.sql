SELECT c.name AS customer, p.name AS product, o.status AS status, o.total AS total
FROM orders o, customers c, products p
WHERE o.status <> 'cancelled'
ORDER BY o.total DESC
LIMIT 500;
