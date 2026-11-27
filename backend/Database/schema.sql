-- ============================================================================
-- Sistema de Gestión y Estadísticas - Jefatura de Limpieza de Calles y Descacharrización
-- Esquema normalizado para PostgreSQL (Supabase)
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ----------------------------------------------------------------------------
-- 1. Usuarios y Control de Acceso
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS usuarios (
    id              SERIAL PRIMARY KEY,
    nombre_completo VARCHAR(150) NOT NULL,
    correo          VARCHAR(150) NOT NULL UNIQUE,
    password_hash   TEXT NOT NULL,
    rol             VARCHAR(30) NOT NULL CHECK (rol IN ('Capturador', 'Coordinador', 'Desarrollador')),
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Usuario inicial Desarrollador (Cambiar contraseña después del primer inicio de sesión)
-- Credenciales iniciales: dev@limpiezaurbana.gob.mx / AdminLimpieza2026!
INSERT INTO usuarios (nombre_completo, correo, password_hash, rol, activo)
VALUES (
    'Administrador Desarrollador',
    'dev@limpiezaurbana.gob.mx',
    crypt('AdminLimpieza2026!', gen_salt('bf', 11)),
    'Desarrollador',
    TRUE
)
ON CONFLICT (correo) DO NOTHING;

-- ----------------------------------------------------------------------------
-- 2. Catálogos Operativos
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS cuadrillas (
    id      SERIAL PRIMARY KEY,
    numero  SMALLINT NOT NULL UNIQUE CHECK (numero BETWEEN 1 AND 15),
    nombre  VARCHAR(100) NOT NULL,
    activa  BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO cuadrillas (numero, nombre)
SELECT n, 'Cuadrilla ' || n
FROM generate_series(1, 15) AS n
ON CONFLICT (numero) DO NOTHING;

CREATE TABLE IF NOT EXISTS categorias (
    id          SERIAL PRIMARY KEY,
    nombre      VARCHAR(100) NOT NULL UNIQUE,
    descripcion TEXT
);

INSERT INTO categorias (nombre, descripcion) VALUES
    ('Recorrido Diario', 'Actividades asignadas en recorrido diario (nombre de licenciado en Solicitado)'),
    ('CIGA', 'Reportes canalizados mediante CIGA'),
    ('OFICIO', 'Solicitudes formales mediante oficio'),
    ('Otros', 'Otras categorías operativas')
ON CONFLICT (nombre) DO NOTHING;

CREATE TABLE IF NOT EXISTS catalogo_actividades (
    id      SERIAL PRIMARY KEY,
    clave   VARCHAR(50) NOT NULL UNIQUE,
    nombre  VARCHAR(120) NOT NULL UNIQUE,
    activo  BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO catalogo_actividades (clave, nombre) VALUES
    ('BM', 'Barrido Manual (BM)'),
    ('CZ', 'Chapoleo / Desmalezado (CZ)'),
    ('PB', 'Papeleo / Barrido (PB)'),
    ('LB', 'Limpieza de Bordo / Basura (LB)'),
    ('LE', 'Levantamiento de Escombro (LE)'),
    ('LT', 'Levantamiento de Tierra (LT)'),
    ('LR', 'Levantamiento de Rama (LR)')
ON CONFLICT (clave) DO NOTHING;

-- ----------------------------------------------------------------------------
-- 3. Cargas Mensuales y Registros Operativos
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS cargas_mensuales (
    id              SERIAL PRIMARY KEY,
    mes             SMALLINT NOT NULL CHECK (mes BETWEEN 1 AND 12),
    anio            SMALLINT NOT NULL CHECK (anio >= 2020),
    nombre_archivo  VARCHAR(255) NOT NULL,
    usuario_id      INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE RESTRICT,
    fecha_carga     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS registros_operativos (
    id              BIGSERIAL PRIMARY KEY,
    carga_id        INTEGER NOT NULL REFERENCES cargas_mensuales(id) ON DELETE CASCADE,
    fecha           DATE NOT NULL,
    cuadrilla_id    INTEGER NULL REFERENCES cuadrillas(id) ON DELETE SET NULL,
    ubicacion       TEXT NOT NULL,
    solicitado_raw  VARCHAR(200) NOT NULL,
    numero_oficio   VARCHAR(100) NULL,
    categoria_id    INTEGER NOT NULL REFERENCES categorias(id) ON DELETE RESTRICT,
    ml              NUMERIC(12, 2) NOT NULL DEFAULT 0 CHECK (ml >= 0),
    m2              NUMERIC(12, 2) NOT NULL DEFAULT 0 CHECK (m2 >= 0),
    m3              NUMERIC(12, 2) NOT NULL DEFAULT 0 CHECK (m3 >= 0),
    kg              NUMERIC(12, 2) NOT NULL DEFAULT 0 CHECK (kg >= 0),
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS registro_actividades (
    registro_id     BIGINT NOT NULL REFERENCES registros_operativos(id) ON DELETE CASCADE,
    actividad_id    INTEGER NOT NULL REFERENCES catalogo_actividades(id) ON DELETE RESTRICT,
    PRIMARY KEY (registro_id, actividad_id)
);

-- Asegurar columna numero_oficio en bases ya creadas
ALTER TABLE registros_operativos ADD COLUMN IF NOT EXISTS numero_oficio VARCHAR(100) NULL;

-- ----------------------------------------------------------------------------
-- 4. Índices para consultas estadísticas
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_registros_fecha ON registros_operativos(fecha);
CREATE INDEX IF NOT EXISTS idx_registros_categoria ON registros_operativos(categoria_id);
CREATE INDEX IF NOT EXISTS idx_registros_carga ON registros_operativos(carga_id);
CREATE INDEX IF NOT EXISTS idx_cargas_anio_mes ON cargas_mensuales(anio, mes);
