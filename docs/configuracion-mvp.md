# Configuración del MVP 1.0

## Principio de seguridad

`appsettings.json` contiene únicamente valores para desarrollo local. Producción debe usar variables de entorno o el gestor de secretos del servidor.

Nunca deben confirmarse en Git:

- `.env`;
- contraseñas reales;
- secretos JWT;
- claves SMTP;
- API keys de Evolution;
- cadenas de conexión de producción.

## Variables obligatorias

### PostgreSQL

- `POSTGRES_DB`: nombre de la base.
- `POSTGRES_USER`: usuario de PostgreSQL.
- `POSTGRES_PASSWORD`: contraseña segura.
- `POSTGRES_PORT`: puerto publicado por Docker.
- `ConnectionStrings__DefaultConnection`: cadena usada por ASP.NET Core.

### Administrador inicial

- `CRM_ADMIN_EMAIL`.
- `CRM_ADMIN_PASSWORD`.
- `CRM_ADMIN_FIRST_NAME`.
- `CRM_ADMIN_LAST_NAME`.

La contraseña inicial debe cambiarse después del primer acceso.

### JWT

- `Jwt__Issuer`.
- `Jwt__Audience`.
- `Jwt__Secret`: secreto aleatorio de al menos 32 caracteres.
- `Jwt__ExpirationMinutes`.

Cambiar `Jwt__Secret` invalida las sesiones activas.

### Frontend

- `VITE_API_URL`: URL accesible del backend, sin `/api` al final.

Ejemplo local:

```text
http://localhost:8080
```

## Documentos

`Documents__StoragePath` define dónde se guardan los archivos físicos. En producción debe apuntar a un volumen persistente con permisos de lectura y escritura para el proceso de la API.

Ejemplo:

```text
/var/lib/crm-mida/documents
```

## Cotizaciones

- `QuotePortal__PublicUrl`: base del portal que recibe el token público.
- `QuoteDelivery__PublicApiUrl`: URL pública del backend usada para recursos compartidos.

## SMTP opcional

El envío por correo permanece deshabilitado funcionalmente hasta configurar:

- `QuoteDelivery__Smtp__Host`.
- `QuoteDelivery__Smtp__Port`.
- `QuoteDelivery__Smtp__EnableSsl`.
- `QuoteDelivery__Smtp__User`.
- `QuoteDelivery__Smtp__Password`.
- `QuoteDelivery__Smtp__From`.

## Evolution API opcional

- `QuoteDelivery__Evolution__BaseUrl`.
- `QuoteDelivery__Evolution__Instance`.
- `QuoteDelivery__Evolution__ApiKey`.

No habilitar WhatsApp hasta validar la instancia y las políticas de uso del número conectado.

## Alertas de licencias

El job está desactivado por defecto:

```text
LicenseAlerts__Enabled=false
```

Configuración disponible:

- `LicenseAlerts__TimeZone=America/Mexico_City`.
- `LicenseAlerts__Hour=8`.
- `LicenseAlerts__Days=90`.
- `LicenseAlerts__NotifyEmail`.
- `LicenseAlerts__NotifyWhatsApp`.

En despliegues con varias instancias de la API, habilitar el job solo en una.

## Comprobación previa

Antes de arrancar producción:

1. comprobar que no queda ningún valor `change_me`;
2. comprobar que el secreto JWT no es el de desarrollo;
3. comprobar acceso de escritura al volumen de documentos;
4. comprobar que `VITE_API_URL` apunta a la API pública;
5. mantener SMTP, Evolution y alertas deshabilitados hasta probarlos individualmente.
