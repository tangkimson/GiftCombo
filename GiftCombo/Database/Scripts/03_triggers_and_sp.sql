---- helper: raise when stock would turn negative
CREATE OR REPLACE FUNCTION check_item_stock (
  p_item_id  IN item.item_id%TYPE,
  p_needed   IN NUMBER
) RETURN NUMBER IS
  v_stock item.stock_qty%TYPE;
BEGIN
  SELECT stock_qty INTO v_stock
  FROM item
  WHERE item_id = p_item_id
  FOR UPDATE;                         -- lock row until tx commits

  IF v_stock < p_needed THEN
     RAISE_APPLICATION_ERROR(-20001,
       'Insufficient stock for item_id='||p_item_id);
  END IF;
  RETURN v_stock;                     -- handy if caller needs it
END;
/

---- BEFORE INSERT → validate qty & calculate line_total
CREATE OR REPLACE TRIGGER trg_sale_line_bi
BEFORE INSERT ON sale_line
FOR EACH ROW
DECLARE
   v_price     item.price%TYPE;
   v_dummy     NUMBER;              -- holds the stock qty we get back
BEGIN
   IF :new.item_id IS NOT NULL THEN
      /* ----- single-item sale line --------------------------------- */
      SELECT price
        INTO v_price
        FROM item
       WHERE item_id = :new.item_id;

      v_dummy := check_item_stock(:new.item_id, :new.quantity);  -- may raise
      :new.line_total := v_price * :new.quantity;

   ELSE
      /* ----- combo sale line -------------------------------------- */
      SELECT price
        INTO v_price
        FROM combo
       WHERE combo_id = :new.combo_id;

      :new.line_total := v_price * :new.quantity;

      FOR r IN (SELECT item_id, quantity
                  FROM combo_item
                 WHERE combo_id = :new.combo_id) LOOP
         v_dummy := check_item_stock(
                      r.item_id,
                      r.quantity * :new.quantity);                -- may raise
      END LOOP;
   END IF;
END;
/

---- AFTER INSERT → actually deduct stock
CREATE OR REPLACE TRIGGER trg_sale_line_ai
AFTER INSERT ON sale_line
FOR EACH ROW
BEGIN
  IF :new.item_id IS NOT NULL THEN
     UPDATE item
        SET stock_qty = stock_qty - :new.quantity
      WHERE item_id   = :new.item_id;

  ELSE
     FOR r IN (SELECT item_id, quantity
                 FROM combo_item
                WHERE combo_id = :new.combo_id) LOOP
       UPDATE item
          SET stock_qty = stock_qty - r.quantity * :new.quantity
        WHERE item_id   = r.item_id;
     END LOOP;
  END IF;
END;
/

---- BEFORE DELETE (voided sale, rollback, etc.) → restock
CREATE OR REPLACE TRIGGER trg_sale_line_bd
BEFORE DELETE ON sale_line
FOR EACH ROW
BEGIN
  IF :old.item_id IS NOT NULL THEN
     UPDATE item
        SET stock_qty = stock_qty + :old.quantity
      WHERE item_id   = :old.item_id;
  ELSE
     FOR r IN (SELECT item_id, quantity
                 FROM combo_item
                WHERE combo_id = :old.combo_id) LOOP
       UPDATE item
          SET stock_qty = stock_qty + r.quantity * :old.quantity
        WHERE item_id   = r.item_id;
     END LOOP;
  END IF;
END;
/

---- simple SP for bulk restocking (e.g. receiving delivery)
CREATE OR REPLACE PROCEDURE add_stock (
  p_item_id  IN item.item_id%TYPE,
  p_delta    IN NUMBER
) AS
BEGIN
  UPDATE item
     SET stock_qty = stock_qty + ABS(p_delta)
   WHERE item_id   = p_item_id;
END;
/
