-- =========================================================
-- SCRIPT DE CORRECCIÓN DE TRIGGERS (Stock Doble)
-- =========================================================

BEGIN;

-- 1. ELIMINAR TODOS LOS TRIGGERS EXISTENTES EN LAS TABLAS AFECTADAS
-- Esto asegura que no queden triggers "fantasmas" o duplicados que causen el doble conteo.
DO $$
DECLARE
    r RECORD;
BEGIN
    -- Eliminar triggers de detalle_compra (Ingreso)
    FOR r IN (SELECT tgname FROM pg_trigger WHERE tgrelid = 'public.detalle_compra'::regclass AND tgisinternal = false) LOOP
        EXECUTE 'DROP TRIGGER ' || quote_ident(r.tgname) || ' ON public.detalle_compra';
        RAISE NOTICE 'Trigger eliminado: % en detalle_compra', r.tgname;
    END LOOP;

    -- Eliminar triggers de salida (Salida)
    FOR r IN (SELECT tgname FROM pg_trigger WHERE tgrelid = 'public.salida'::regclass AND tgisinternal = false) LOOP
        EXECUTE 'DROP TRIGGER ' || quote_ident(r.tgname) || ' ON public.salida';
        RAISE NOTICE 'Trigger eliminado: % en salida', r.tgname;
    END LOOP;
END $$;

-- 2. RECREAR FUNCIONES Y TRIGGERS CON LA LÓGICA CORRECTA

-- ---------------------------------------------------------
-- A) Trigger de SALIDA (fn_trg_salida_stock)
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_trg_salida_stock()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_sede_area uuid; v_sede_exi uuid;
  v_actual integer;
BEGIN
  IF TG_OP = 'INSERT' THEN
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = NEW.area_id;
    SELECT e.sede_id  INTO v_sede_exi  FROM public.existencia e WHERE e.id = NEW.existencia_id;
    IF v_sede_area IS DISTINCT FROM v_sede_exi THEN RAISE EXCEPTION 'Sedes no coinciden entre área y existencia'; END IF;

    -- Validar contra Stock Global (Bodega)
    SELECT stock_actual INTO v_actual
      FROM public.existencia
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi
     FOR UPDATE;
    
    IF v_actual < NEW.cantidad THEN RAISE EXCEPTION 'Stock insuficiente en bodega general'; END IF;

    -- Actualizar Stock Global
    UPDATE public.existencia
       SET stock_actual = stock_actual - NEW.cantidad
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi;

    RETURN NULL;

  ELSIF TG_OP = 'UPDATE' THEN
    -- Reponer OLD (Solo Global)
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = OLD.area_id;
    UPDATE public.existencia
       SET stock_actual = stock_actual + OLD.cantidad
     WHERE id = OLD.existencia_id AND sede_id = v_sede_area;

    -- Aplicar NEW (Solo Global)
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = NEW.area_id;
    SELECT e.sede_id  INTO v_sede_exi  FROM public.existencia e WHERE e.id = NEW.existencia_id;
    IF v_sede_area IS DISTINCT FROM v_sede_exi THEN RAISE EXCEPTION 'Sedes no coinciden (UPDATE)'; END IF;

    SELECT stock_actual INTO v_actual
      FROM public.existencia
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi
     FOR UPDATE;
    
    IF v_actual < NEW.cantidad THEN RAISE EXCEPTION 'Stock insuficiente en bodega general'; END IF;

    UPDATE public.existencia
       SET stock_actual = stock_actual - NEW.cantidad
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi;

    RETURN NULL;

  ELSIF TG_OP = 'DELETE' THEN
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = OLD.area_id;

    -- Reponer OLD (Solo Global)
    UPDATE public.existencia
       SET stock_actual = stock_actual + OLD.cantidad
     WHERE id = OLD.existencia_id AND sede_id = v_sede_area;

    RETURN NULL;
  END IF;
  RETURN NULL;
END;
$$;

CREATE TRIGGER tr_salida_aiud
AFTER INSERT OR UPDATE OR DELETE ON public.salida
FOR EACH ROW EXECUTE FUNCTION public.fn_trg_salida_stock();


-- ---------------------------------------------------------
-- B) Trigger de INGRESO/COMPRA (fn_trg_detalle_compra_stock)
-- ---------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_trg_detalle_compra_stock()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_sede uuid; v_sede_old uuid;
  v_area bigint; v_area_old bigint;
  v_exi_sede uuid; v_exi_sede_old uuid;
BEGIN
  IF TG_OP = 'INSERT' THEN
    SELECT c.sede_id, c.area_id_destino INTO v_sede, v_area FROM public.compra c WHERE c.id = NEW.compra_id;
    SELECT e.sede_id INTO v_exi_sede FROM public.existencia e WHERE e.id = NEW.existencia_id;
    IF v_exi_sede IS DISTINCT FROM v_sede THEN RAISE EXCEPTION 'Sedes no coinciden entre compra y existencia'; END IF;

    UPDATE public.existencia SET stock_actual = stock_actual + NEW.cantidad
     WHERE id = NEW.existencia_id AND sede_id = v_sede;

    IF v_area IS NOT NULL THEN
      INSERT INTO public.existencia_area_stock(existencia_id, area_id, stock_area, sede_id)
      VALUES (NEW.existencia_id, v_area, NEW.cantidad, v_sede)
      ON CONFLICT (existencia_id, area_id) DO
        UPDATE SET stock_area = public.existencia_area_stock.stock_area + EXCLUDED.stock_area;
    END IF;
    RETURN NULL;

  ELSIF TG_OP = 'UPDATE' THEN
    SELECT c.sede_id, c.area_id_destino INTO v_sede, v_area FROM public.compra c WHERE c.id = NEW.compra_id;
    SELECT c.sede_id, c.area_id_destino INTO v_sede_old, v_area_old FROM public.compra c WHERE c.id = OLD.compra_id;
    SELECT e.sede_id INTO v_exi_sede FROM public.existencia e WHERE e.id = NEW.existencia_id;
    SELECT e.sede_id INTO v_exi_sede_old FROM public.existencia e WHERE e.id = OLD.existencia_id;

    IF v_exi_sede IS DISTINCT FROM v_sede OR v_exi_sede_old IS DISTINCT FROM v_sede_old THEN
      RAISE EXCEPTION 'Sedes no coinciden (UPDATE)';
    END IF;

    IF NEW.existencia_id = OLD.existencia_id AND v_sede = v_sede_old THEN
      UPDATE public.existencia
         SET stock_actual = stock_actual + (NEW.cantidad - OLD.cantidad)
       WHERE id = NEW.existencia_id AND sede_id = v_sede;

      IF v_area_old IS NOT NULL THEN
        UPDATE public.existencia_area_stock
           SET stock_area = stock_area - OLD.cantidad
         WHERE existencia_id = OLD.existencia_id AND area_id = v_area_old AND sede_id = v_sede_old;
      END IF;

      IF v_area IS NOT NULL THEN
        INSERT INTO public.existencia_area_stock(existencia_id, area_id, stock_area, sede_id)
        VALUES (NEW.existencia_id, v_area, NEW.cantidad, v_sede)
        ON CONFLICT (existencia_id, area_id) DO
          UPDATE SET stock_area = public.existencia_area_stock.stock_area + EXCLUDED.stock_area;
      END IF;
    ELSE
      UPDATE public.existencia
         SET stock_actual = stock_actual - OLD.cantidad
       WHERE id = OLD.existencia_id AND sede_id = v_sede_old;

      IF v_area_old IS NOT NULL THEN
        UPDATE public.existencia_area_stock
           SET stock_area = stock_area - OLD.cantidad
         WHERE existencia_id = OLD.existencia_id AND area_id = v_area_old AND sede_id = v_sede_old;
      END IF;

      UPDATE public.existencia
         SET stock_actual = stock_actual + NEW.cantidad
       WHERE id = NEW.existencia_id AND sede_id = v_sede;

      IF v_area IS NOT NULL THEN
        INSERT INTO public.existencia_area_stock(existencia_id, area_id, stock_area, sede_id)
        VALUES (NEW.existencia_id, v_area, NEW.cantidad, v_sede)
        ON CONFLICT (existencia_id, area_id) DO
          UPDATE SET stock_area = public.existencia_area_stock.stock_area + EXCLUDED.stock_area;
      END IF;
    END IF;
    RETURN NULL;

  ELSIF TG_OP = 'DELETE' THEN
    SELECT c.sede_id, c.area_id_destino INTO v_sede, v_area FROM public.compra c WHERE c.id = OLD.compra_id;

    UPDATE public.existencia
       SET stock_actual = stock_actual - OLD.cantidad
     WHERE id = OLD.existencia_id AND sede_id = v_sede;

    IF v_area IS NOT NULL THEN
      UPDATE public.existencia_area_stock
         SET stock_area = GREATEST(0, stock_area - OLD.cantidad)
       WHERE existencia_id = OLD.existencia_id AND area_id = v_area AND sede_id = v_sede;
    END IF;
    RETURN NULL;
  END IF;
  RETURN NULL;
END;
$$;

CREATE TRIGGER tr_detalle_compra_aiud
AFTER INSERT OR UPDATE OR DELETE ON public.detalle_compra
FOR EACH ROW EXECUTE FUNCTION public.fn_trg_detalle_compra_stock();


-- 3. RESTAURAR TRIGGERS DE AUDITORÍA (Si existían)
-- Como borramos TODOS los triggers, debemos asegurarnos de que la auditoría siga funcionando.
CREATE OR REPLACE FUNCTION public.fn_auditoria_row_mt()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_id bigint;
  v_usuario text;
  v_sede uuid;
  v_tbl text := TG_TABLE_NAME;
  v_transaction_id text;
  v_accion_usuario text;
  jnew jsonb := to_jsonb(NEW);
  jold jsonb := to_jsonb(OLD);
BEGIN
  v_id := COALESCE((jnew->>'id')::bigint, (jold->>'id')::bigint);
  v_usuario := COALESCE(current_setting('app.user', true), current_user);
  v_sede := COALESCE((jnew->>'sede_id')::uuid, (jold->>'sede_id')::uuid);
  v_transaction_id := txid_current()::text;
  v_accion_usuario := current_setting('app.accion_usuario', true);

  IF v_sede IS NULL THEN
    IF v_tbl = 'detalle_compra' THEN
      v_sede := (SELECT sede_id FROM public.compra WHERE id = COALESCE((jnew->>'compra_id')::bigint, (jold->>'compra_id')::bigint));
    ELSIF v_tbl = 'baja_activo' THEN
      v_sede := (SELECT sede_id FROM public.activo WHERE id = COALESCE((jnew->>'activo_id')::bigint, (jold->>'activo_id')::bigint));
    ELSIF v_tbl = 'existencia_area_stock' THEN
      v_sede := (SELECT sede_id FROM public.existencia WHERE id = COALESCE((jnew->>'existencia_id')::bigint, (jold->>'existencia_id')::bigint));
    END IF;
  END IF;

  INSERT INTO public.auditoria (usuario, accion, entidad, id_entidad, detalle, sede_id, transaction_id, accion_usuario)
  VALUES (v_usuario, TG_OP, v_tbl, v_id, CASE WHEN TG_OP='DELETE' THEN jold ELSE jnew END, v_sede, v_transaction_id, v_accion_usuario);

  IF TG_OP='DELETE' THEN RETURN OLD; ELSE RETURN NEW; END IF;
END;
$$;

-- Re-crear triggers de auditoría para detalle_compra y salida
DROP TRIGGER IF EXISTS tr_aud_detalle_compra ON public.detalle_compra;
CREATE TRIGGER tr_aud_detalle_compra AFTER INSERT OR UPDATE OR DELETE ON public.detalle_compra
FOR EACH ROW EXECUTE FUNCTION public.fn_auditoria_row_mt();

DROP TRIGGER IF EXISTS tr_aud_salida ON public.salida;
CREATE TRIGGER tr_aud_salida AFTER INSERT OR UPDATE OR DELETE ON public.salida
FOR EACH ROW EXECUTE FUNCTION public.fn_auditoria_row_mt();

COMMIT;
