# Sistema de Gestión y Estadísticas — Jefatura de Limpieza de Calles y Descacharrización

Aplicación web full-stack para la captura, validación y análisis estadístico de los reportes operativos diarios de las 15 cuadrillas de limpieza urbana, automatizando el procesamiento de archivos mensuales de Excel divididos en hojas por día.

---

## Enlaces Desplegados

* **Frontend (Producción - Vercel):** [https://nexxus-jet.vercel.app/](https://nexxus-jet.vercel.app/)
* **Backend API (Producción - Render):** [https://sistema-limpieza-urbana.onrender.com/](https://sistema-limpieza-urbana.onrender.com/)

> **Nota:** El backend está alojado en el plan gratuito de Render; si permanece inactivo por más de 15 minutos, la primera petición puede tardar entre 45 y 60 segundos mientras el contenedor vuelve a activarse.

---

## Stack Tecnológico

* **Frontend:** React 19, TypeScript, Vite, Tailwind CSS, React Router DOM, Recharts, Lucide React
* **Backend:** ASP.NET Core 10 Web API, ExcelDataReader, Npgsql / Entity Framework Core / Dapper, Swagger (Swashbuckle)
* **Base de Datos:** PostgreSQL (Supabase)
* **Infraestructura y CI/CD:** Docker (Render), Vercel, GitHub Actions

---

## Roles del Sistema

* **Capturador:** Carga el archivo mensual de Excel (`.xlsx`), revisa y edita los registros extraídos en una tabla interactiva antes de confirmarlos y los envía para su almacenamiento definitivo en la base de datos.
* **Coordinador:** Consulta el análisis estadístico de las categorías (`Recorrido diario`, `CIGA`, `OFICIO`, entre otras) de forma individual o conjunta, filtrando por rangos de fechas personalizados, meses o años.
* **Desarrollador:** Acceso total a los módulos de captura, consulta estadística y administración del sistema.

---

## Flujo de Procesamiento

1. **Carga del archivo:** El usuario con rol *Capturador* selecciona el libro de Excel mensual desde el frontend.
2. **Extracción y cálculo en Backend:** La API en .NET 10 lee cada hoja diaria mediante `ExcelDataReader`, extrae número de cuadrilla, ubicación, actividades marcadas (`✓`), clasificación de solicitud y cantidades (`ml`, `m²`, `m³`), calculando directamente los kilogramos (`kg`) a partir de los metros cúbicos (`m³`).
3. **Validación y persistencia:** El frontend muestra los registros en una tabla editable con recálculo en tiempo real; una vez validados, se envían al backend para distribuirlos y guardarlos en PostgreSQL (Supabase).

---

## Flujo de Trabajo en Git y CI/CD

El repositorio sigue la estrategia **GitFlow** con integración continua automatizada:

* `main`: Rama de producción estable (despliegue automático en Vercel y Render).
* `develop`: Rama de integración de cambios.
* `feature/*`: Ramas individuales para el desarrollo de nuevas funcionalidades.

Todas las integraciones se realizan mediante **Pull Requests** (`feature/*` $\rightarrow$ `develop` y `develop` $\rightarrow$ `main`), validando automáticamente la compilación del frontend y del backend mediante **GitHub Actions** (`.github/workflows/ci.yml`).

---

## Ejecución Local

### Prerrequisitos

* [.NET SDK 10.0](https://dotnet.microsoft.com/) o superior
* [Node.js 20](https://nodejs.org/) o superior
* Instancia activa en [Supabase](https://supabase.com/) (PostgreSQL)

### 1. Clonar el repositorio

```bash
git clone [https://github.com/RDanielParra/sistema-limpieza-urbana.git](https://github.com/RDanielParra/sistema-limpieza-urbana.git)
cd sistema-limpieza-urbana
```

### 2. Configurar y ejecutar el Backend (.NET 10)

Las cadenas de conexión se gestionan fuera del repositorio mediante **User Secrets** en desarrollo y variables de entorno en producción:

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "Host=aws-1-us-west-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.TU_PROYECTO;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;"
dotnet restore
dotnet run
```

La documentación local de la API estará disponible en `http://localhost:PUERTO/swagger` (según el puerto indicado en consola).

### 3. Configurar y ejecutar el Frontend (React + Vite)

En una nueva terminal:

```bash
cd frontend
cp .env.example .env
# Edita .env y configura VITE_API_URL=http://localhost:PUERTO/api
npm install
npm run dev
```

La aplicación web estará disponible en `http://localhost:5173`.

---

## Equipo y Responsabilidades

* **Reynaldo Daniel Reyes Parra:** Desarrollo Frontend y Administración del Repositorio de GitHub
* **Hiram García Guerra:** Desarrollo Backend y Base de Datos
