# CRM MIDA

CRM comercial interno para MIDA. Centraliza empresas, contactos, prospectos, oportunidades, actividades, cotizaciones, licencias internas, renovaciones, documentos, reportes, usuarios y auditoría.

## Estado

La rama `release/mvp-1.0` contiene el candidato a la primera versión estable para pruebas internas.

No forman parte de este MVP las integraciones con Licencias MIDA, Soporte MIDA o CONTPAQi.

## Tecnologías

- Frontend: React 19, Vite, TypeScript y Material UI.
- Backend: .NET 8 y ASP.NET Core.
- Persistencia: PostgreSQL 16 y Entity Framework Core.
- Seguridad: JWT, roles, permisos, bloqueo de cuentas y auditoría.
- Infraestructura local: Docker Compose para PostgreSQL.
- Validación: GitHub Actions para backend, pruebas y frontend.

## Funciones del MVP

- Inicio de sesión único y rutas privadas.
- Empresas, contactos y Cliente 360°.
- Prospectos y conversión a empresa.
- Oportunidades, pipeline y actividades.
- Agenda y dashboard comercial.
- Catálogo de productos y servicios.
- Cotizaciones, PDF, entregas y portal público.
- Licencias internas, renovaciones y alertas.
- Documentos por empresa.
- Importación de empresas desde Excel.
- Reportes CSV.
- Usuarios, roles, permisos y auditoría.
- Búsqueda global y layout adaptable.

## Requisitos

- Git.
- Docker y Docker Compose.
- .NET SDK 8.
- Node.js 20 o superior.
- npm.

## Configuración

1. Copia `.env.example` como `.env`.
2. Cambia todas las contraseñas y secretos marcados como obligatorios.
3. Nunca subas el archivo `.env` al repositorio.

La guía completa de variables está en `docs/configuracion-mvp.md`.

## Ejecución local

### 1. Base de datos

```bash
docker compose up -d postgres
```

### 2. Backend

```bash
cd backend/src/CrmMida.Api
dotnet restore
dotnet ef database update
dotnet run
```

Si `dotnet ef` no está instalado:

```bash
dotnet tool install --global dotnet-ef
```

### 3. Frontend

En otra terminal:

```bash
cd frontend
npm install
npm run dev
```

Configura `VITE_API_URL` con la dirección pública o local del backend.

## Validación

```bash
dotnet build backend/src/CrmMida.Api/CrmMida.Api.csproj --configuration Release
dotnet test backend/tests/CrmMida.Domain.Tests/CrmMida.Domain.Tests.csproj --configuration Release
cd frontend
npm install
npm run build
```

## Documentación de entrega

- `docs/configuracion-mvp.md`: variables y secretos.
- `docs/despliegue-mvp.md`: despliegue y reversión.
- `docs/manual-usuario-mvp.md`: operación básica.
- `docs/smoke-test-mvp.md`: pruebas de aceptación.
- `docs/mvp-1.0-closure.md`: checklist de cierre.

## Seguridad

Los valores incluidos en `appsettings.json` son únicamente para desarrollo local. En producción deben reemplazarse con variables de entorno o secretos del servidor, especialmente:

- conexión PostgreSQL;
- secreto JWT;
- SMTP;
- Evolution API;
- URL pública del portal;
- contraseñas iniciales.

## Publicación

La versión `v1.0.0` se creará únicamente después de:

1. validar migraciones sobre una base vacía;
2. completar el smoke test;
3. probar el despliegue en el entorno real;
4. fusionar el PR final a `main`.
