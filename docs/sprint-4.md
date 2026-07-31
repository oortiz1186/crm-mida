# Sprint 4 — Cotizaciones

## Objetivo

Incorporar cotizaciones comerciales vinculadas con empresas, contactos y oportunidades, con cálculo automático de importes y control de estados.

## Alcance

- Entidades `Quote` y `QuoteItem`.
- Folio `COT-AÑO-000000`.
- Moneda, vigencia, descuento, subtotal, impuestos y total.
- Partidas con cantidad, precio unitario y tasa de impuesto.
- Estados: borrador, enviada, aceptada, rechazada y cancelada.
- Relación con empresa, contacto y oportunidad.
- Permisos `quotes.read` y `quotes.manage`.
- API protegida.
- Migración PostgreSQL.
- Pruebas de cálculo y reglas de estado.
- Workspace React en `/quotes`.

## Endpoints

- `GET /api/v1/quotes`
- `GET /api/v1/quotes/{id}`
- `POST /api/v1/quotes`
- `PUT /api/v1/quotes/{id}`
- `PATCH /api/v1/quotes/{id}/status`

## Reglas

- Toda cotización pertenece a una empresa activa.
- Para enviarla debe tener al menos una partida.
- Una cotización aceptada o cancelada ya no puede editarse.
- Los importes se calculan en backend.
- El descuento nunca produce un total negativo.

## Criterios de aceptación

- Un usuario autorizado puede crear, consultar y editar borradores.
- El sistema calcula subtotal, impuesto y total.
- Se puede enviar, aceptar, rechazar o cancelar una cotización.
- Las cotizaciones se consultan por folio, título o empresa.
- Backend, pruebas y frontend compilan en GitHub Actions.

## Siguiente bloque

Generación de PDF, envío por correo y WhatsApp, catálogo de productos/servicios y flujo de aprobación comercial.
