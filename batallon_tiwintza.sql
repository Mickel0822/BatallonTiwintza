BEGIN;

-- Extensiones
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- =========================
-- TABLAS BASE (multi-tenant)
-- =========================

-- Sede (tenant)
CREATE TABLE IF NOT EXISTS public.sede (
  id    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  clave varchar(30)  NOT NULL UNIQUE,
  nombre varchar(100) NOT NULL
);

-- Usuario / Rol / UsuarioRol (no multi-tenant)
CREATE TABLE IF NOT EXISTS public.usuario (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  username varchar(50) NOT NULL UNIQUE,
  nombre_completo varchar(120) NOT NULL,
  email varchar(120) UNIQUE,
  password_hash text NOT NULL,
  is_active boolean NOT NULL DEFAULT true,
  failed_attempts smallint NOT NULL DEFAULT 0,
  lockout_end timestamptz,
  last_login timestamptz,
  area_id bigint,
  creado_en timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS public.rol (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  nombre varchar(40) NOT NULL UNIQUE,
  descripcion varchar(200)
);

CREATE TABLE IF NOT EXISTS public.usuario_rol (
  usuario_id uuid NOT NULL,
  rol_id uuid NOT NULL,
  PRIMARY KEY(usuario_id, rol_id),
  FOREIGN KEY (usuario_id) REFERENCES public.usuario(id) ON DELETE CASCADE,
  FOREIGN KEY (rol_id) REFERENCES public.rol(id) ON DELETE CASCADE
);

-- Acceso de usuarios a sedes (opcional, útil para admins)
CREATE TABLE IF NOT EXISTS public.usuario_sede (
  usuario_id uuid NOT NULL REFERENCES public.usuario(id) ON DELETE CASCADE,
  sede_id    uuid NOT NULL REFERENCES public.sede(id)    ON DELETE CASCADE,
  PRIMARY KEY (usuario_id, sede_id)
);

-- Catálogos
CREATE TABLE IF NOT EXISTS public.estado (
  id bigserial PRIMARY KEY,
  nombre text NOT NULL UNIQUE,
  es_baja boolean NOT NULL DEFAULT false,
  es_operativo boolean NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS public.tipo_bien (
  id bigserial PRIMARY KEY,
  nombre text NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS public.proveedor (
  id bigserial PRIMARY KEY,
  ruc varchar(13) NOT NULL UNIQUE,
  razon_social text NOT NULL,
  contacto text,
  telefono text,
  email text
);

-- ENTIDADES MULTI-TENANT (con sede_id)
CREATE TABLE IF NOT EXISTS public.area (
  id bigserial PRIMARY KEY,
  nombre text NOT NULL,
  area_padre_id bigint,
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT,
  CONSTRAINT uq_area_por_sede UNIQUE (sede_id, nombre, area_padre_id),
  FOREIGN KEY (area_padre_id) REFERENCES public.area(id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS public.existencia (
  id bigserial PRIMARY KEY,
  codigo varchar(50) NOT NULL,
  nombre text NOT NULL,
  descripcion text,
  unidad varchar(20) NOT NULL,
  nivel_maximo integer NOT NULL,
  nivel_seguridad integer NOT NULL,
  nivel_minimo integer NOT NULL,
  nivel_critico integer NOT NULL,
  stock_actual integer NOT NULL DEFAULT 0,
  proveedor_pref_id bigint,
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT,
  CONSTRAINT uq_existencia_codigo_por_sede UNIQUE (sede_id, codigo),
  FOREIGN KEY (proveedor_pref_id) REFERENCES public.proveedor(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_existencia_sede ON public.existencia(sede_id);

CREATE TABLE IF NOT EXISTS public.existencia_area_stock (
  id bigserial PRIMARY KEY,
  existencia_id bigint NOT NULL REFERENCES public.existencia(id) ON DELETE CASCADE,
  area_id bigint NOT NULL REFERENCES public.area(id) ON DELETE CASCADE,
  stock_area integer NOT NULL DEFAULT 0,
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE CASCADE,
  CONSTRAINT uq_exi_area UNIQUE (existencia_id, area_id)
);

CREATE INDEX IF NOT EXISTS idx_existencia_area_stock_sede ON public.existencia_area_stock(sede_id);

CREATE TABLE IF NOT EXISTS public.compra (
  id bigserial PRIMARY KEY,
  fecha date NOT NULL,
  proveedor_id bigint NOT NULL REFERENCES public.proveedor(id) ON DELETE RESTRICT,
  num_factura varchar(40),
  total numeric(14,2),
  area_id_destino bigint REFERENCES public.area(id) ON DELETE RESTRICT,
  creado_en timestamptz NOT NULL DEFAULT now(),
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT,
  CONSTRAINT uq_compra_factura_por_sede UNIQUE (sede_id, proveedor_id, num_factura)
);

CREATE INDEX IF NOT EXISTS idx_compra_sede_fecha ON public.compra(sede_id, fecha);

CREATE TABLE IF NOT EXISTS public.detalle_compra (
  id bigserial PRIMARY KEY,
  compra_id bigint NOT NULL REFERENCES public.compra(id) ON DELETE CASCADE,
  existencia_id bigint NOT NULL REFERENCES public.existencia(id) ON DELETE RESTRICT,
  cantidad integer NOT NULL,
  costo_unitario numeric(12,2),
  creado_en timestamptz NOT NULL DEFAULT now(),
  -- (No requiere sede_id: se deriva de compra/existencia en triggers y auditoría)
  sede_id uuid NULL
);

-- Activo y operaciones
CREATE TABLE IF NOT EXISTS public.activo (
  id bigserial PRIMARY KEY,
  codigo_inventario varchar(50) NOT NULL,
  nombre text NOT NULL,
  tipo_id bigint NOT NULL REFERENCES public.tipo_bien(id) ON DELETE RESTRICT,
  descripcion text,
  marca text,
  modelo text,
  serie text,
  material text,
  estado_id bigint NOT NULL REFERENCES public.estado(id) ON DELETE RESTRICT,
  area_id bigint NOT NULL REFERENCES public.area(id) ON DELETE RESTRICT,
  valor_unitario numeric(12,2) NOT NULL DEFAULT 0,
  fecha_compra date,
  proveedor_id bigint REFERENCES public.proveedor(id) ON DELETE SET NULL,
  compra_id bigint REFERENCES public.compra(id) ON DELETE SET NULL,
  vida_util_meses integer,
  depreciacion_mensual numeric(12,2),
  garantia_meses integer,
  foto_url text,
  observaciones text,
  creado_en timestamptz NOT NULL DEFAULT now(),
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT,
  CONSTRAINT uq_activo_codigo_por_sede UNIQUE (sede_id, codigo_inventario)
);

CREATE INDEX IF NOT EXISTS idx_activo_sede ON public.activo(sede_id);
CREATE INDEX IF NOT EXISTS idx_activo_area ON public.activo(area_id);
CREATE INDEX IF NOT EXISTS idx_activo_estado ON public.activo(estado_id);
CREATE INDEX IF NOT EXISTS idx_activo_tipo ON public.activo(tipo_id);

CREATE TABLE IF NOT EXISTS public.baja_activo (
  id bigserial PRIMARY KEY,
  activo_id bigint NOT NULL REFERENCES public.activo(id) ON DELETE RESTRICT,
  codigo_informe_tecnico text NOT NULL,
  fecha_baja date NOT NULL DEFAULT CURRENT_DATE,
  responsable text NOT NULL,
  observaciones text,
  creado_en timestamptz NOT NULL DEFAULT now(),
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_baja_activo_sede_fecha ON public.baja_activo(sede_id, fecha_baja);

CREATE TABLE IF NOT EXISTS public.salida (
  id bigserial PRIMARY KEY,
  fecha date NOT NULL,
  existencia_id bigint NOT NULL REFERENCES public.existencia(id) ON DELETE RESTRICT,
  cantidad integer NOT NULL,
  area_id bigint NOT NULL REFERENCES public.area(id) ON DELETE RESTRICT,
  responsable text,
  observacion text,
  creado_en timestamptz NOT NULL DEFAULT now(),
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_salida_sede_fecha ON public.salida(sede_id, fecha);
CREATE INDEX IF NOT EXISTS idx_salida_area ON public.salida(area_id);
CREATE INDEX IF NOT EXISTS idx_salida_exi ON public.salida(existencia_id);

CREATE TABLE IF NOT EXISTS public.traslado_activo (
  id bigserial PRIMARY KEY,
  activo_id bigint NOT NULL REFERENCES public.activo(id) ON DELETE CASCADE,
  area_origen_id bigint NOT NULL REFERENCES public.area(id) ON DELETE RESTRICT,
  area_destino_id bigint NOT NULL REFERENCES public.area(id) ON DELETE RESTRICT,
  fecha date NOT NULL DEFAULT CURRENT_DATE,
  observacion text,
  usuario text,
  creado_en timestamptz NOT NULL DEFAULT now(),
  sede_id uuid NOT NULL REFERENCES public.sede(id) ON DELETE RESTRICT
);

-- Auditoría y login_auditoria
CREATE TABLE IF NOT EXISTS public.auditoria (
  id bigserial PRIMARY KEY,
  fecha_hora timestamptz NOT NULL DEFAULT now(),
  usuario text NOT NULL,
  accion text NOT NULL,
  entidad text NOT NULL,
  id_entidad bigint,
  detalle jsonb,
  sede_id uuid NULL REFERENCES public.sede(id) ON DELETE SET NULL,
  transaction_id text NULL,
  accion_usuario text NULL
);

CREATE INDEX IF NOT EXISTS idx_auditoria_entidad_fecha ON public.auditoria(entidad, fecha_hora);
CREATE INDEX IF NOT EXISTS idx_auditoria_sede_fecha ON public.auditoria(sede_id, fecha_hora);
CREATE INDEX IF NOT EXISTS idx_auditoria_transaction ON public.auditoria(transaction_id) WHERE transaction_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS public.login_auditoria (
  id bigserial PRIMARY KEY,
  usuario_id uuid REFERENCES public.usuario(id) ON DELETE SET NULL,
  fecha_hora timestamptz NOT NULL DEFAULT now(),
  exito boolean NOT NULL,
  detalle text,
  sede_id uuid NULL REFERENCES public.sede(id) ON DELETE SET NULL
);

-- =========================
-- SEED de sedes
-- =========================
DO $$
BEGIN
  INSERT INTO public.sede(clave, nombre) VALUES ('tonsupa','Tonsupa')
  ON CONFLICT (clave) DO UPDATE SET nombre = EXCLUDED.nombre;

  INSERT INTO public.sede(clave, nombre) VALUES ('quito','Quito')
  ON CONFLICT (clave) DO UPDATE SET nombre = EXCLUDED.nombre;
END $$;

-- =========================================================
-- TRIGGERS (funciones + triggers)
-- =========================================================

-- 1) BEFORE: baja_activo toma sede del activo
CREATE OR REPLACE FUNCTION public.fn_before_baja_set_sede()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE v_sede uuid;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    SELECT a.sede_id INTO v_sede FROM public.activo a WHERE a.id = COALESCE(NEW.activo_id, OLD.activo_id);
    IF v_sede IS NULL THEN
      RAISE EXCEPTION 'No se encontró sede para el activo %', COALESCE(NEW.activo_id, OLD.activo_id);
    END IF;
    NEW.sede_id := v_sede;
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_before_baja_set_sede ON public.baja_activo;
CREATE TRIGGER tr_before_baja_set_sede
BEFORE INSERT OR UPDATE ON public.baja_activo
FOR EACH ROW EXECUTE FUNCTION public.fn_before_baja_set_sede();

-- 2) DETALLE_COMPRA: sincroniza stock y stock por área (sede segura)
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

DROP TRIGGER IF EXISTS tr_detalle_compra_aiud ON public.detalle_compra;
CREATE TRIGGER tr_detalle_compra_aiud
AFTER INSERT OR UPDATE OR DELETE ON public.detalle_compra
FOR EACH ROW EXECUTE FUNCTION public.fn_trg_detalle_compra_stock();

-- 3) SALIDA: valida sede/stock y descuenta
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

    SELECT stock_actual INTO v_actual
      FROM public.existencia
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi
     FOR UPDATE;
    IF v_actual < NEW.cantidad THEN RAISE EXCEPTION 'Stock insuficiente'; END IF;

    PERFORM 1 FROM public.existencia_area_stock eas
     WHERE eas.existencia_id = NEW.existencia_id
       AND eas.area_id = NEW.area_id
       AND eas.sede_id = v_sede_exi
       AND eas.stock_area >= NEW.cantidad
     FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Stock insuficiente en área'; END IF;

    UPDATE public.existencia
       SET stock_actual = stock_actual - NEW.cantidad
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi;

    UPDATE public.existencia_area_stock
       SET stock_area = GREATEST(0, stock_area - NEW.cantidad)
     WHERE existencia_id = NEW.existencia_id
       AND area_id = NEW.area_id
       AND sede_id = v_sede_exi;
    RETURN NULL;

  ELSIF TG_OP = 'UPDATE' THEN
    -- Reponer OLD
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = OLD.area_id;
    UPDATE public.existencia
       SET stock_actual = stock_actual + OLD.cantidad
     WHERE id = OLD.existencia_id AND sede_id = v_sede_area;

    UPDATE public.existencia_area_stock
       SET stock_area = stock_area + OLD.cantidad
     WHERE existencia_id = OLD.existencia_id
       AND area_id = OLD.area_id
       AND sede_id = v_sede_area;

    -- Aplicar NEW (misma validación que INSERT)
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = NEW.area_id;
    SELECT e.sede_id  INTO v_sede_exi  FROM public.existencia e WHERE e.id = NEW.existencia_id;
    IF v_sede_area IS DISTINCT FROM v_sede_exi THEN RAISE EXCEPTION 'Sedes no coinciden (UPDATE)'; END IF;

    SELECT stock_actual INTO v_actual
      FROM public.existencia
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi
     FOR UPDATE;
    IF v_actual < NEW.cantidad THEN RAISE EXCEPTION 'Stock insuficiente'; END IF;

    PERFORM 1 FROM public.existencia_area_stock eas
     WHERE eas.existencia_id = NEW.existencia_id
       AND eas.area_id = NEW.area_id
       AND eas.sede_id = v_sede_exi
       AND eas.stock_area >= NEW.cantidad
     FOR UPDATE;
    IF NOT FOUND THEN RAISE EXCEPTION 'Stock insuficiente en área'; END IF;

    UPDATE public.existencia
       SET stock_actual = stock_actual - NEW.cantidad
     WHERE id = NEW.existencia_id AND sede_id = v_sede_exi;

    UPDATE public.existencia_area_stock
       SET stock_area = GREATEST(0, stock_area - NEW.cantidad)
     WHERE existencia_id = NEW.existencia_id
       AND area_id = NEW.area_id
       AND sede_id = v_sede_exi;
    RETURN NULL;

  ELSIF TG_OP = 'DELETE' THEN
    SELECT ar.sede_id INTO v_sede_area FROM public.area ar WHERE ar.id = OLD.area_id;

    UPDATE public.existencia
       SET stock_actual = stock_actual + OLD.cantidad
     WHERE id = OLD.existencia_id AND sede_id = v_sede_area;

    UPDATE public.existencia_area_stock
       SET stock_area = stock_area + OLD.cantidad
     WHERE existencia_id = OLD.existencia_id
       AND area_id = OLD.area_id
       AND sede_id = v_sede_area;
    RETURN NULL;
  END IF;
  RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS tr_salida_aiud ON public.salida;
CREATE TRIGGER tr_salida_aiud
AFTER INSERT OR UPDATE OR DELETE ON public.salida
FOR EACH ROW EXECUTE FUNCTION public.fn_trg_salida_stock();

-- 4) BAJA_ACTIVO: cambia estado a “Baja”
CREATE OR REPLACE FUNCTION public.fn_trg_baja_cambia_estado()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_estado_baja bigint;
  v_sede_activo uuid;
BEGIN
  IF TG_OP IN ('INSERT','UPDATE') THEN
    SELECT id INTO v_estado_baja FROM public.estado WHERE lower(nombre)='baja' LIMIT 1;
    IF v_estado_baja IS NULL THEN RAISE EXCEPTION 'No existe estado "Baja"'; END IF;

    SELECT sede_id INTO v_sede_activo FROM public.activo WHERE id = COALESCE(NEW.activo_id, OLD.activo_id);

    UPDATE public.activo
       SET estado_id = v_estado_baja
     WHERE id = COALESCE(NEW.activo_id, OLD.activo_id)
       AND sede_id = v_sede_activo;

    RETURN COALESCE(NEW, OLD);
  END IF;
  RETURN COALESCE(NEW, OLD);
END;
$$;

DROP TRIGGER IF EXISTS tr_baja_activo_set_estado ON public.baja_activo;
CREATE TRIGGER tr_baja_activo_set_estado
AFTER INSERT OR UPDATE ON public.baja_activo
FOR EACH ROW EXECUTE FUNCTION public.fn_trg_baja_cambia_estado();

-- 5) TRASLADO_ACTIVO: actualiza área validando sede
CREATE OR REPLACE FUNCTION public.fn_trg_traslado_actualiza_area()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
  v_sede_activo  uuid;
  v_sede_origen  uuid;
  v_sede_destino uuid;
BEGIN
  SELECT a.sede_id INTO v_sede_activo  FROM public.activo a WHERE a.id = NEW.activo_id;
  SELECT ar.sede_id INTO v_sede_origen  FROM public.area  ar WHERE ar.id = NEW.area_origen_id;
  SELECT ar.sede_id INTO v_sede_destino FROM public.area  ar WHERE ar.id = NEW.area_destino_id;

  IF v_sede_activo IS DISTINCT FROM v_sede_origen OR v_sede_activo IS DISTINCT FROM v_sede_destino THEN
    RAISE EXCEPTION 'Sedes no coinciden en traslado (activo/áreas)';
  END IF;

  UPDATE public.activo
     SET area_id = NEW.area_destino_id
   WHERE id = NEW.activo_id
     AND sede_id = v_sede_activo;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_traslado_activo_actualiza_area ON public.traslado_activo;
CREATE TRIGGER tr_traslado_activo_actualiza_area
AFTER INSERT ON public.traslado_activo
FOR EACH ROW EXECUTE FUNCTION public.fn_trg_traslado_actualiza_area();

-- 6) AUDITORÍA genérica multi-tenant
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
  
  -- Capture transaction ID for grouping related operations
  v_transaction_id := txid_current()::text;
  
  -- Capture user action description (optional, set by application)
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

DO $make_aud$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'activo','area','proveedor','tipo_bien','existencia','existencia_area_stock',
    'compra','detalle_compra','salida','baja_activo','traslado_activo'
  ]
  LOOP
    EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I;', 'tr_aud_'||t, 'public', t);
    EXECUTE format(
      'CREATE TRIGGER %I AFTER INSERT OR UPDATE OR DELETE ON %I.%I
       FOR EACH ROW EXECUTE FUNCTION public.fn_auditoria_row_mt();',
      'tr_aud_'||t, 'public', t
    );
  END LOOP;
END;
$make_aud$;

COMMIT;
