# Sistema de Gestión y Estadísticas - Jefatura de Limpieza de Calles y Descacharrización

Aplicación web para la captura, validación y análisis estadístico de reportes operativos diarios de las 15 cuadrillas a partir de archivos mensuales de Excel divididos en hojas por día[cite: 1].

## Enlaces Desplegados
* **Frontend (Producción):** [Pendiente de despliegue]
* **Backend API (Swagger):** [Pendiente de despliegue]

## Stack Tecnológico
* **Frontend:** React 19, TypeScript, Vite, Tailwind CSS, Recharts, Lucide React
* **Backend:** ASP.NET Core 10 Web API, ExcelDataReader, Npgsql / EF Core, Swagger (Swashbuckle)
* **Base de Datos:** PostgreSQL (Supabase)
* **CI/CD:** GitHub Actions

## Roles del Sistema
* **Capturador:** Sube el archivo Excel mensual, revisa y edita los registros procesados en una tabla interactiva antes de confirmarlos para almacenarlos en la base de datos[cite: 1].
* **Coordinador:** Consulta el análisis estadístico de las categorías (recorrido diario, CIGA, OFICIO, etc.) de forma individual o conjunta, filtrado por fechas personalizadas, meses o años[cite: 1].
* **Desarrollador:** Acceso total a todas las acciones dentro de la aplicación[cite: 1].

## Flujo de Procesamiento
1. El frontend envía el archivo `.xlsx` mensual al backend.
2. El backend extrae los registros por hoja diaria mediante `ExcelDataReader`, clasifica la categoría y calcula los kilogramos (`kg`) a partir de los metros cúbicos (`m³`)[cite: 1].
3. El frontend despliega los datos en una tabla editable para su validación y recálculo en tiempo real antes de guardarlos definitivamente en PostgreSQL.

## Ejecución Local

### Prerrequisitos
* .NET SDK (v10.0 o superior)
* Node.js (v20 o superior)
* Proyecto activo en Supabase (PostgreSQL)

### 1. Clonar el repositorio
```bash
git clone [https://github.com/TU_USUARIO/sistema-limpieza-urbana.git](https://github.com/TU_USUARIO/sistema-limpieza-urbana.git)
cd sistema-limpieza-urbana
```

### 2. Configurar y ejecutar el Backend (.NET 10)
```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "Host=aws-1-us-west-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fxgcxejlziscyhlsqqxz;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;"
dotnet restore
dotnet run
```
La documentación interactiva de la API estará disponible en `http://localhost:PUERTO/swagger` (según el puerto indicado en consola).

### 3. Configurar y ejecutar el Frontend (React)
En otra terminal:
```bash
cd frontend
cp .env.example .env
# Configurar VITE_API_URL en .env
npm install
npm run dev
```
La aplicación estará disponible en `http://localhost:5173`.

## Equipo y Responsabilidades
* **Reynaldo Daniel Reyes Parra:** Frontend y Administración del Repositorio de GitHub[cite: 2]
* **Hiram García Guerra:** Backend y Base de Datos[cite: 2]