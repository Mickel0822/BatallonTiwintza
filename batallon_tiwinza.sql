BEGIN;

-- ------------------------------------------------------------
-- 0) LIMPIEZA (opcional pero recomendado para pruebas)
-- ------------------------------------------------------------
TRUNCATE TABLE
  auditoria,
  traslado_activo,
  baja_activo,
  activo,
  salida,
  existencia_area_stock,
  detalle_compra,
  compra,
  existencia,
  proveedor,
  estado,
  tipo_bien,
  area
RESTART IDENTITY CASCADE;

-- ------------------------------------------------------------
-- 1) CATÁLOGOS
-- ------------------------------------------------------------

-- 1.1 Estados (incluye el 'Baja' requerido por triggers)
INSERT INTO estado (nombre, es_baja, es_operativo) VALUES
  ('Bueno', FALSE, TRUE),
  ('Regular', FALSE, TRUE),
  ('Malo', FALSE, TRUE),
  ('Mantenimiento', FALSE, FALSE),
  ('Proceso de Baja', FALSE, FALSE),
  ('Baja', TRUE, FALSE);

-- 1.2 Tipos de bien (50)
INSERT INTO tipo_bien (nombre)
SELECT 'Tipo ' || gs FROM generate_series(1,50) gs;

-- 1.3 Áreas (55: 11 padres + 4 hijos c/u)
WITH padres AS (
  INSERT INTO area (nombre, area_padre_id)
  VALUES
    ('Comando', NULL),
    ('Logística', NULL),
    ('Operaciones', NULL),
    ('Administración', NULL),
    ('Mantenimiento', NULL),
    ('Sanidad', NULL),
    ('Transporte', NULL),
    ('Comunicaciones', NULL),
    ('Intendencia', NULL),
    ('Seguridad', NULL),
    ('Bodega', NULL)             -- importante: usada como destino en compras
  RETURNING id, nombre
)
INSERT INTO area (nombre, area_padre_id)
SELECT p.nombre || ' - Sección ' || s, p.id
FROM padres p
CROSS JOIN generate_series(1,4) s;

-- 1.4 Proveedores (60)
INSERT INTO proveedor (ruc, razon_social, contacto, telefono, email)
SELECT
  LPAD((100000000000 + gs)::text, 13, '0') AS ruc,
  'Proveedor ' || gs                         AS razon_social,
  'Contacto '  || gs                         AS contacto,
  '(02) ' || (4000000 + gs)                  AS telefono,
  'prov' || gs || '@correo.com'              AS email
FROM generate_series(1,60) gs;

-- ------------------------------------------------------------
-- 2) EXISTENCIAS (80)  +  COMPRAS (100)  +  DETALLES (300)
--    Los triggers actualizan stock y existencia_area_stock
-- ------------------------------------------------------------

-- 2.1 Existencias (80)
WITH pref AS (SELECT id FROM proveedor ORDER BY id LIMIT 60)
INSERT INTO existencia (codigo, nombre, descripcion, unidad,
                        nivel_maximo, nivel_seguridad, nivel_minimo, nivel_critico,
                        stock_actual, proveedor_pref_id)
SELECT
  'EXI' || LPAD(gs::text, 4, '0')                               AS codigo,
  'Existencia ' || gs                                            AS nombre,
  'Descripción ' || gs                                           AS descripcion,
  (ARRAY['und','rollo','bot','paq','caja'])[ (random()*4)::int+1 ] AS unidad,
  200, 120, 60, 20,                                              -- niveles
  0,                                                             -- stock inicial (subirá con compras)
  (SELECT id FROM pref ORDER BY random() LIMIT 1)                 AS proveedor_pref_id
FROM generate_series(1,80) gs;

-- 2.2 Compras (100)
WITH bod AS (SELECT id FROM area WHERE nombre='Bodega' LIMIT 1)
INSERT INTO compra (fecha, proveedor_id, num_factura, total, area_id_destino)
SELECT
  CURRENT_DATE - ((random()*365)::int)                                       AS fecha,
  (SELECT id FROM proveedor ORDER BY random() LIMIT 1)                       AS proveedor_id,
  'F' || LPAD(gs::text, 6, '0')                                             AS num_factura,
  round((random()*1000+200)::numeric, 2)                                     AS total,
  (SELECT id FROM bod)                                                       AS area_id_destino
FROM generate_series(1,100) gs;

-- 2.3 Detalles de compra (~300 líneas; 3 por compra, cantidades 5..30)
INSERT INTO detalle_compra (compra_id, existencia_id, cantidad, costo_unitario)
SELECT
  c.id,
  (SELECT id FROM existencia ORDER BY random() LIMIT 1),
  5 + (random()*25)::int,
  round((random()*50+5)::numeric, 2)
FROM compra c
CROSS JOIN generate_series(1,3);

-- ------------------------------------------------------------
-- 3) ACTIVOS (120)  +  TRASLADOS (30)  +  BAJAS (20)
-- ------------------------------------------------------------

-- 3.1 Activos (120) con estado != 'Baja' al inicio
WITH estados_ok AS (SELECT id FROM estado WHERE lower(nombre) <> 'baja'),
     tipos AS (SELECT id FROM tipo_bien),
     areas AS (SELECT id FROM area),
     provs AS (SELECT id FROM proveedor),
     comp AS (SELECT id FROM compra)
INSERT INTO activo (codigo_inventario, nombre, tipo_id, descripcion, marca, modelo, serie,
                    material, estado_id, area_id, valor_unitario, fecha_compra,
                    proveedor_id, compra_id, vida_util_meses, depreciacion_mensual, documento_autorizacion,
                    garantia_meses, foto_url, observaciones)
SELECT
  'ACT' || LPAD(gs::text, 5, '0')                                      AS codigo_inventario,
  'Activo ' || gs                                                      AS nombre,
  (SELECT id FROM tipos ORDER BY random() LIMIT 1)                      AS tipo_id,
  'Descripción del activo ' || gs                                       AS descripcion,
  (ARRAY['HP','Lenovo','Dell','LG','Bosch','Makita','Samsung'])[(random()*6)::int+1] AS marca,
  'M-' || (100 + (random()*900)::int)                                   AS modelo,
  'S-' || (100000 + (random()*900000)::int)                             AS serie,
  (ARRAY['Acero','Aluminio','Plástico','Madera'])[(random()*3)::int+1]  AS material,
  (SELECT id FROM estados_ok ORDER BY random() LIMIT 1)                 AS estado_id,
  (SELECT id FROM areas ORDER BY random() LIMIT 1)                      AS area_id,
  round((random()*1500+200)::numeric, 2)                                AS valor_unitario,
  CURRENT_DATE - ((random()*1460)::int)                                 AS fecha_compra, -- 4 años
  (SELECT id FROM provs ORDER BY random() LIMIT 1)                      AS proveedor_id,
  (SELECT id FROM comp  ORDER BY random() LIMIT 1)                      AS compra_id,
  36 + (random()*60)::int                                               AS vida_util_meses,
  round((random()*40+5)::numeric, 2)                                    AS depreciacion_mensual,
  (random() > 0.7)                                                      AS documento_autorizacion,
  6 + (random()*24)::int                                                AS garantia_meses,
  NULL,                                                                 -- foto_url
  NULL                                                                  -- observaciones
FROM generate_series(1,120) gs;

-- 3.2 Traslados (30, a un área distinta)
WITH a AS (
  SELECT id, area_id AS origen FROM activo ORDER BY random() LIMIT 30
),
dest AS (
  SELECT id AS area_id FROM area ORDER BY random()
)
INSERT INTO traslado_activo (activo_id, area_origen_id, area_destino_id, fecha, observacion, usuario)
SELECT
  a.id,
  a.origen,
  (SELECT d.area_id FROM dest d WHERE d.area_id <> a.origen ORDER BY random() LIMIT 1),
  CURRENT_DATE - ((random()*180)::int),
  'Traslado por redistribución',
  'user@test'
FROM a;

-- 3.3 Bajas (20, activa el trigger que cambia el estado a “Baja”)
WITH cand AS (
  SELECT id FROM activo ORDER BY random() LIMIT 20
)
INSERT INTO baja_activo (activo_id, codigo_informe_tecnico, fecha_baja, responsable, observaciones)
SELECT
  id,
  'INF-' || LPAD((1000 + row_number() OVER ())::text, 6, '0'),
  CURRENT_DATE - ((random()*120)::int),
  'Responsable ' || (row_number() OVER ()),
  'Deterioro o reemplazo'
FROM cand;

-- ------------------------------------------------------------
-- 4) SALIDAS (consumo) – asegurando no exceder stock
-- ------------------------------------------------------------

-- Una salida por existencia con stock > 0 (hasta 2 por algunas)
-- Cantidad = entre 1 y MIN(10, stock_actual)
WITH exi AS (
  SELECT id, stock_actual FROM existencia WHERE stock_actual > 0
),
sal1 AS (
  INSERT INTO salida (fecha, existencia_id, cantidad, area_id, responsable, observacion)
  SELECT
    CURRENT_DATE - ((random()*120)::int),
    e.id,
    GREATEST(1, LEAST(10, e.stock_actual, 1 + (random()*9)::int)),
    (SELECT id FROM area ORDER BY random() LIMIT 1),
    'Usuario ' || (row_number() OVER ()),
    'Consumo regular'
  FROM exi e
  RETURNING 1
),
sal2 AS (
  INSERT INTO salida (fecha, existencia_id, cantidad, area_id, responsable, observacion)
  SELECT
    CURRENT_DATE - ((random()*60)::int),
    e.id,
    GREATEST(1, LEAST(5, e.stock_actual/2)),        -- más conservador
    (SELECT id FROM area ORDER BY random() LIMIT 1),
    'Usuario ' || (row_number() OVER ()),
    'Consumo adicional'
  FROM existencia e
  WHERE e.stock_actual > 6                             -- sólo donde alcanzó
  ORDER BY random()
  LIMIT 60
  RETURNING 1
)
SELECT 1;

-- ------------------------------------------------------------
-- 5) AUDITORÍA (80)
-- ------------------------------------------------------------
INSERT INTO auditoria (usuario, accion, entidad, id_entidad, detalle)
SELECT
  (ARRAY['mickel','sistemas','admin','operador'])[ (random()*3)::int+1 ],
  (ARRAY['Alta','Edicion','Baja','Traslado','Ingreso','Salida','Exportacion','Login'])[ (random()*7)::int+1 ],
  (ARRAY['activo','existencia','compra','salida'])[ (random()*3)::int+1 ],
  (random()*200)::int,
  jsonb_build_object('ip','10.0.'|| (random()*200)::int ||'.'|| (random()*200)::int,
                     'navegador','WPF-Client',
                     'obs','registro demo')
FROM generate_series(1,80);

COMMIT;
