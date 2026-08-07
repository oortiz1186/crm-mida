# Despliegue del MVP 1.0

## Objetivo

Publicar CRM MIDA para pruebas internas con PostgreSQL persistente, backend .NET y frontend compilado.

## Preparación

1. Crear una copia de `.env.example` llamada `.env`.
2. Reemplazar todos los valores de ejemplo.
3. Crear un directorio persistente para documentos.
4. Confirmar que los puertos elegidos no están ocupados.
5. Respaldar cualquier base existente antes de aplicar migraciones.

## Base de datos

```bash
docker compose up -d postgres
docker compose ps
docker compose logs --tail=100 postgres
```

La base debe aparecer saludable antes de iniciar la API.

## Migraciones

Desde la raíz del repositorio:

```bash
dotnet restore backend/src/CrmMida.Api/CrmMida.Api.csproj
dotnet ef database update --project backend/src/CrmMida.Infrastructure --startup-project backend/src/CrmMida.Api
```

Si la estructura real de proyectos exige otra ruta, usar el comando documentado por `dotnet ef migrations list` y registrar el ajuste antes de publicar.

## Backend

Validación previa:

```bash
dotnet build backend/src/CrmMida.Api/CrmMida.Api.csproj --configuration Release
dotnet test backend/tests/CrmMida.Domain.Tests/CrmMida.Domain.Tests.csproj --configuration Release
```

Publicación:

```bash
dotnet publish backend/src/CrmMida.Api/CrmMida.Api.csproj --configuration Release --output ./publish/api
```

El proceso del backend debe recibir las variables del `.env` mediante el servicio elegido: systemd, Docker, PM2 para procesos compatibles o el gestor del proveedor.

## Frontend

```bash
cd frontend
npm install
npm run build
```

El contenido generado en `frontend/dist` puede publicarse mediante el servidor estático seleccionado o integrarse al esquema de publicación del servidor.

`VITE_API_URL` debe estar definido antes de ejecutar `npm run build`.

## Cloudflare

Cuando se use Cloudflare Tunnel:

- ruta pública del frontend hacia el servicio web;
- ruta pública de la API hacia el puerto del backend;
- HTTPS activo;
- sin exponer PostgreSQL a Internet;
- `QuotePortal__PublicUrl` y `QuoteDelivery__PublicApiUrl` con las URLs finales.

## Verificaciones posteriores

1. Abrir `/login`.
2. Iniciar sesión con el administrador.
3. Cambiar la contraseña inicial.
4. Ejecutar `docs/smoke-test-mvp.md`.
5. Revisar logs del backend.
6. Confirmar persistencia de documentos después de reiniciar el servicio.
7. Confirmar que PostgreSQL no está accesible públicamente.

## Respaldo

Respaldar PostgreSQL antes de cada actualización:

```bash
docker exec crm-mida-postgres pg_dump -U crm_mida -d crm_mida > crm_mida_backup.sql
```

Respaldar también el directorio configurado en `Documents__StoragePath`.

## Reversión

Si una publicación falla:

1. detener backend y frontend nuevos;
2. restaurar la publicación anterior;
3. revisar si la migración es compatible hacia atrás;
4. restaurar el respaldo de PostgreSQL únicamente cuando sea necesario;
5. restaurar el volumen de documentos si fue afectado;
6. registrar el incidente antes de reintentar.

No ejecutar eliminaciones manuales de tablas para revertir una migración.

## Criterio de publicación

No fusionar ni etiquetar `v1.0.0` hasta que:

- CI esté en verde;
- migraciones funcionen en una base vacía;
- smoke test esté aprobado;
- acceso y permisos se prueben con al menos un administrador y un usuario de consulta;
- el respaldo y la reversión estén comprendidos.
