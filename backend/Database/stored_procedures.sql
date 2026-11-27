-- ============================================================================
-- Procedimientos / Funciones Almacenadas para Consultas de Estadísticas y Registros
-- Ejecutar en Supabase SQL Editor después de schema.sql
-- ============================================================================

-- 1. Totales acumulados
CREATE OR REPLACE FUNCTION sp_estadisticas_totales(
    p_fecha_inicio  DATE DEFAULT NULL,
    p_fecha_fin     DATE DEFAULT NULL,
    p_mes           INT DEFAULT NULL,
    p_anio          INT DEFAULT NULL,
    p_categoria_ids INT[] DEFAULT NULL
)
RETURNS TABLE (
    total_registros BIGINT,
    ml              NUMERIC,
    m2              NUMERIC,
    m3              NUMERIC,
    kg              NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        COUNT(*)::BIGINT AS total_registros,
        COALESCE(SUM(ro.ml), 0) AS ml,
        COALESCE(SUM(ro.m2), 0) AS m2,
        COALESCE(SUM(ro.m3), 0) AS m3,
        COALESCE(SUM(ro.kg), 0) AS kg
    FROM registros_operativos ro
    WHERE (p_fecha_inicio IS NULL OR ro.fecha >= p_fecha_inicio)
      AND (p_fecha_fin IS NULL OR ro.fecha <= p_fecha_fin)
      AND (p_mes IS NULL OR EXTRACT(MONTH FROM ro.fecha) = p_mes)
      AND (p_anio IS NULL OR EXTRACT(YEAR FROM ro.fecha) = p_anio)
      AND (p_categoria_ids IS NULL OR ro.categoria_id = ANY(p_categoria_ids));
$$;

-- 2. Desglose por categoría
CREATE OR REPLACE FUNCTION sp_estadisticas_por_categoria(
    p_fecha_inicio  DATE DEFAULT NULL,
    p_fecha_fin     DATE DEFAULT NULL,
    p_mes           INT DEFAULT NULL,
    p_anio          INT DEFAULT NULL,
    p_categoria_ids INT[] DEFAULT NULL
)
RETURNS TABLE (
    categoria_id    INT,
    categoria       VARCHAR,
    total_registros BIGINT,
    ml              NUMERIC,
    m2              NUMERIC,
    m3              NUMERIC,
    kg              NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        c.id AS categoria_id,
        c.nombre AS categoria,
        COUNT(ro.id)::BIGINT AS total_registros,
        COALESCE(SUM(ro.ml), 0) AS ml,
        COALESCE(SUM(ro.m2), 0) AS m2,
        COALESCE(SUM(ro.m3), 0) AS m3,
        COALESCE(SUM(ro.kg), 0) AS kg
    FROM registros_operativos ro
    INNER JOIN categorias c ON c.id = ro.categoria_id
    WHERE (p_fecha_inicio IS NULL OR ro.fecha >= p_fecha_inicio)
      AND (p_fecha_fin IS NULL OR ro.fecha <= p_fecha_fin)
      AND (p_mes IS NULL OR EXTRACT(MONTH FROM ro.fecha) = p_mes)
      AND (p_anio IS NULL OR EXTRACT(YEAR FROM ro.fecha) = p_anio)
      AND (p_categoria_ids IS NULL OR ro.categoria_id = ANY(p_categoria_ids))
    GROUP BY c.id, c.nombre
    ORDER BY kg DESC, total_registros DESC;
$$;

-- 3. Desglose por actividad
CREATE OR REPLACE FUNCTION sp_estadisticas_por_actividad(
    p_fecha_inicio  DATE DEFAULT NULL,
    p_fecha_fin     DATE DEFAULT NULL,
    p_mes           INT DEFAULT NULL,
    p_anio          INT DEFAULT NULL,
    p_categoria_ids INT[] DEFAULT NULL
)
RETURNS TABLE (
    actividad_id    INT,
    clave           VARCHAR,
    actividad       VARCHAR,
    total_registros BIGINT,
    ml              NUMERIC,
    m2              NUMERIC,
    m3              NUMERIC,
    kg              NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        ca.id AS actividad_id,
        ca.clave AS clave,
        ca.nombre AS actividad,
        COUNT(DISTINCT ro.id)::BIGINT AS total_registros,
        COALESCE(SUM(ro.ml), 0) AS ml,
        COALESCE(SUM(ro.m2), 0) AS m2,
        COALESCE(SUM(ro.m3), 0) AS m3,
        COALESCE(SUM(ro.kg), 0) AS kg
    FROM registros_operativos ro
    INNER JOIN registro_actividades ra ON ra.registro_id = ro.id
    INNER JOIN catalogo_actividades ca ON ca.id = ra.actividad_id
    WHERE (p_fecha_inicio IS NULL OR ro.fecha >= p_fecha_inicio)
      AND (p_fecha_fin IS NULL OR ro.fecha <= p_fecha_fin)
      AND (p_mes IS NULL OR EXTRACT(MONTH FROM ro.fecha) = p_mes)
      AND (p_anio IS NULL OR EXTRACT(YEAR FROM ro.fecha) = p_anio)
      AND (p_categoria_ids IS NULL OR ro.categoria_id = ANY(p_categoria_ids))
    GROUP BY ca.id, ca.clave, ca.nombre
    ORDER BY total_registros DESC, ca.nombre ASC;
$$;

-- 4. Series agrupadas por fecha
CREATE OR REPLACE FUNCTION sp_estadisticas_series_fecha(
    p_fecha_inicio  DATE DEFAULT NULL,
    p_fecha_fin     DATE DEFAULT NULL,
    p_mes           INT DEFAULT NULL,
    p_anio          INT DEFAULT NULL,
    p_categoria_ids INT[] DEFAULT NULL
)
RETURNS TABLE (
    fecha_dt        TIMESTAMP,
    total_registros BIGINT,
    ml              NUMERIC,
    m2              NUMERIC,
    m3              NUMERIC,
    kg              NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        ro.fecha::TIMESTAMP AS fecha_dt,
        COUNT(*)::BIGINT AS total_registros,
        COALESCE(SUM(ro.ml), 0) AS ml,
        COALESCE(SUM(ro.m2), 0) AS m2,
        COALESCE(SUM(ro.m3), 0) AS m3,
        COALESCE(SUM(ro.kg), 0) AS kg
    FROM registros_operativos ro
    WHERE (p_fecha_inicio IS NULL OR ro.fecha >= p_fecha_inicio)
      AND (p_fecha_fin IS NULL OR ro.fecha <= p_fecha_fin)
      AND (p_mes IS NULL OR EXTRACT(MONTH FROM ro.fecha) = p_mes)
      AND (p_anio IS NULL OR EXTRACT(YEAR FROM ro.fecha) = p_anio)
      AND (p_categoria_ids IS NULL OR ro.categoria_id = ANY(p_categoria_ids))
    GROUP BY ro.fecha
    ORDER BY ro.fecha ASC;
$$;

-- 5. Series agrupadas por mes
CREATE OR REPLACE FUNCTION sp_estadisticas_series_mes(
    p_fecha_inicio  DATE DEFAULT NULL,
    p_fecha_fin     DATE DEFAULT NULL,
    p_mes           INT DEFAULT NULL,
    p_anio          INT DEFAULT NULL,
    p_categoria_ids INT[] DEFAULT NULL
)
RETURNS TABLE (
    anio            INT,
    mes             INT,
    periodo         TEXT,
    total_registros BIGINT,
    ml              NUMERIC,
    m2              NUMERIC,
    m3              NUMERIC,
    kg              NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        EXTRACT(YEAR FROM ro.fecha)::INT AS anio,
        EXTRACT(MONTH FROM ro.fecha)::INT AS mes,
        TO_CHAR(DATE_TRUNC('month', ro.fecha), 'YYYY-MM') AS periodo,
        COUNT(*)::BIGINT AS total_registros,
        COALESCE(SUM(ro.ml), 0) AS ml,
        COALESCE(SUM(ro.m2), 0) AS m2,
        COALESCE(SUM(ro.m3), 0) AS m3,
        COALESCE(SUM(ro.kg), 0) AS kg
    FROM registros_operativos ro
    WHERE (p_fecha_inicio IS NULL OR ro.fecha >= p_fecha_inicio)
      AND (p_fecha_fin IS NULL OR ro.fecha <= p_fecha_fin)
      AND (p_mes IS NULL OR EXTRACT(MONTH FROM ro.fecha) = p_mes)
      AND (p_anio IS NULL OR EXTRACT(YEAR FROM ro.fecha) = p_anio)
      AND (p_categoria_ids IS NULL OR ro.categoria_id = ANY(p_categoria_ids))
    GROUP BY anio, mes, periodo
    ORDER BY anio ASC, mes ASC;
$$;

-- 6. Consulta paginada de registros operativos con sus actividades
CREATE OR REPLACE FUNCTION sp_listar_registros(
    p_fecha_inicio  DATE DEFAULT NULL,
    p_fecha_fin     DATE DEFAULT NULL,
    p_mes           INT DEFAULT NULL,
    p_anio          INT DEFAULT NULL,
    p_categoria_ids INT[] DEFAULT NULL,
    p_limite        INT DEFAULT 50,
    p_offset        INT DEFAULT 0
)
RETURNS TABLE (
    id              BIGINT,
    carga_id        INT,
    fecha_dt        TIMESTAMP,
    no_cuadrilla    SMALLINT,
    ubicacion       TEXT,
    solicitado_raw  VARCHAR,
    numero_oficio   VARCHAR,
    categoria       VARCHAR,
    actividades     TEXT,
    ml              NUMERIC,
    m2              NUMERIC,
    m3              NUMERIC,
    kg              NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        ro.id,
        ro.carga_id,
        ro.fecha::TIMESTAMP AS fecha_dt,
        cu.numero AS no_cuadrilla,
        ro.ubicacion,
        ro.solicitado_raw,
        ro.numero_oficio,
        c.nombre AS categoria,
        COALESCE(STRING_AGG(ca.clave, ', ' ORDER BY ca.id), '') AS actividades,
        ro.ml,
        ro.m2,
        ro.m3,
        ro.kg
    FROM registros_operativos ro
    INNER JOIN categorias c ON c.id = ro.categoria_id
    LEFT JOIN cuadrillas cu ON cu.id = ro.cuadrilla_id
    LEFT JOIN registro_actividades ra ON ra.registro_id = ro.id
    LEFT JOIN catalogo_actividades ca ON ca.id = ra.actividad_id
    WHERE (p_fecha_inicio IS NULL OR ro.fecha >= p_fecha_inicio)
      AND (p_fecha_fin IS NULL OR ro.fecha <= p_fecha_fin)
      AND (p_mes IS NULL OR EXTRACT(MONTH FROM ro.fecha) = p_mes)
      AND (p_anio IS NULL OR EXTRACT(YEAR FROM ro.fecha) = p_anio)
      AND (p_categoria_ids IS NULL OR ro.categoria_id = ANY(p_categoria_ids))
    GROUP BY ro.id, ro.carga_id, ro.fecha, cu.numero, ro.ubicacion, ro.solicitado_raw, ro.numero_oficio, c.nombre, ro.ml, ro.m2, ro.m3, ro.kg
    ORDER BY ro.fecha DESC, ro.id DESC
    LIMIT GREATEST(1, LEAST(p_limite, 500))
    OFFSET GREATEST(0, p_offset);
$$;
