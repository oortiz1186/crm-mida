# Sprint 4 · Historial y portal público de cotizaciones

## Alcance

Este bloque agrega trazabilidad de envíos y un portal público para que el cliente consulte, descargue, acepte o rechace una cotización sin iniciar sesión en el CRM.

## Historial de envíos

Endpoints protegidos:

- `GET /api/v1/quotes/{id}/deliveries`
- `POST /api/v1/quotes/{id}/deliveries/send`

Cada intento registra canal, destinatario, estado, referencia del proveedor, error, número de intento y fechas de ejecución. Los fallos se conservan para permitir análisis y reintentos posteriores.

## Enlace público

Endpoint protegido:

- `POST /api/v1/quotes/{id}/public-link`

El token se entrega una sola vez al usuario que crea el enlace. En la base de datos solo se conserva su hash SHA-256. La vigencia puede configurarse entre 1 y 60 días.

Variables:

- `QuotePortal__PublicUrl=https://crm.mida.mx/public/quotes`

## Portal sin autenticación

- `GET /api/public/quotes/{token}`
- `GET /api/public/quotes/{token}/pdf`
- `POST /api/public/quotes/{token}/decision`

La decisión aceptada o rechazada actualiza la cotización y registra comentario y fecha. Un enlace vencido, revocado o ya respondido no permite una segunda decisión.

## Frontend

Ruta:

- `/public/quotes/:token`

Muestra la propuesta, partidas, importes, vigencia, descarga PDF y controles de aceptación o rechazo.

## Seguridad

- Tokens aleatorios de 256 bits.
- Persistencia exclusiva del hash.
- Vigencia limitada.
- Una sola respuesta por enlace.
- Los endpoints internos requieren `quotes.read` o `quotes.manage`.

## Pendiente

- Panel interno para revocar enlaces.
- Reintento manual desde la interfaz de cotizaciones.
- Notificación al asesor cuando el cliente responde.
- Firma electrónica y evidencia de IP/agente de usuario.
