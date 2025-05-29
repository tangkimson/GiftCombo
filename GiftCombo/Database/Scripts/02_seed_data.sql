---- sample items ------------------------------------------------
INSERT INTO item (name,item_type,price,stock_qty)
VALUES ('Espresso'         ,'DRINK', 2.00, 200);
INSERT INTO item (name,item_type,price,stock_qty)
VALUES ('Cappuccino'       ,'DRINK', 2.50, 150);
INSERT INTO item (name,item_type,price,stock_qty)
VALUES ('Ham Sandwich'     ,'FOOD' , 4.00, 120);
INSERT INTO item (name,item_type,price,stock_qty)
VALUES ('Cheese Croissant' ,'FOOD' , 3.20, 100);
INSERT INTO item (name,item_type,price,stock_qty)
VALUES ('Reusable Cup'     ,'OTHER', 5.00,  50);

---- sample combo ------------------------------------------------
INSERT INTO combo (name,price) VALUES
  ('Morning Kick-starter', 6.00);      -- promo price (cheaper than sum)

-- map items → combo (2 × Espresso + 1 Croissant)
INSERT INTO combo_item (combo_id,item_id,quantity)
SELECT c.combo_id, i.item_id,
       CASE i.name WHEN 'Espresso' THEN 2 ELSE 1 END AS qty
FROM combo c
JOIN item  i ON i.name IN ('Espresso','Cheese Croissant')
WHERE c.name = 'Morning Kick-starter';

SELECT * FROM COMBO;
SELECT * FROM ITEM;
SELECT * FROM COMBO_ITEM;
SELECT * FROM SALE;
SELECT * FROM SALE_LINE;

-- try selling 3 combos (will deduct 6 Espresso + 3 Croissant)
INSERT INTO sale (sale_date) VALUES (SYSDATE) RETURNING sale_id INTO :v_id;

INSERT INTO sale_line(sale_id,combo_id,quantity)
VALUES (:v_id, (SELECT combo_id FROM combo WHERE name='Morning Kick-starter'), 3);

SELECT * FROM ITEM;   -- check new stock levels

COMMIT;

DELETE FROM COMBO
WHERE combo_id = (SELECT combo_id FROM combo WHERE name='New combo');
COMMIT;

SELECT * FROM COMBO;
SELECT * FROM COMBO_ITEM;

-- Insert 1 capuccino into combo 1
INSERT INTO combo_item (combo_id, item_id, quantity)
VALUES (
  (SELECT combo_id FROM combo WHERE name = 'Morning Kick-starter'),
  (SELECT item_id FROM item WHERE name = 'Cappuccino'),
  1
);

-- Delete 1 capuccino from combo 1
DELETE FROM combo_item
WHERE combo_id = (SELECT combo_id FROM combo WHERE name = 'Morning Kick-starter')
  AND item_id = (SELECT item_id FROM item WHERE name = 'Cappuccino');

  SELECT combo_id, name, price FROM combo ORDER BY combo_id;