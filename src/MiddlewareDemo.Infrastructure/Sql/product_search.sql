SELECT *
FROM products
WHERE name LIKE '%' || @term || '%'
   OR description LIKE '%' || @term || '%'
   OR category LIKE '%' || @term || '%'
ORDER BY name;
