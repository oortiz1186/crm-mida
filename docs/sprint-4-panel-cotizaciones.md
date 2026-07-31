# Sprint 4 — Panel operativo de cotizaciones

## Objetivo

Completar la operación interna de cotizaciones desde una sola pantalla, reutilizando el historial de entregas y el portal público previamente implementados.

## Funcionalidades

- Envío por correo o WhatsApp.
- Mensaje personalizado.
- Historial de intentos y errores.
- Reintento al mismo canal y destinatario.
- Generación de enlaces públicos con vigencia configurable.
- Consulta del estado de apertura y respuesta.
- Revocación de accesos públicos activos.
- Descarga del PDF.

## API agregada

- `GET /api/v1/quotes/{id}/public-links`
- `POST /api/v1/quotes/{quoteId}/public-links/{accessId}/revoke`

Ambos endpoints requieren autenticación. La lectura requiere `quotes.read` y la revocación requiere `quotes.manage`.

## Interfaz

La pantalla `/quotes` incorpora un diálogo de operación por cotización con las secciones:

1. Enviar cotización.
2. Historial de entregas.
3. Acceso público.
4. Estado de apertura, vencimiento y decisión.

## Reglas

- Un enlace revocado deja de ser válido inmediatamente.
- Un enlace respondido no puede revocarse desde la interfaz.
- Los reintentos generan un nuevo registro y conservan el intento anterior.
- Los errores del proveedor se muestran y permanecen en el historial.
