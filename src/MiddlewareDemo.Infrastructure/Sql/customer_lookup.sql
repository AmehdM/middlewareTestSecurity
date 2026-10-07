SELECT *
FROM customers
WHERE email LIKE '%@example.%'
   OR phone LIKE '%555%'
   OR national_id LIKE '%0801%';
