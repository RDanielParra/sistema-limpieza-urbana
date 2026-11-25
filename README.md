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
2. **Extracción y cálculo
