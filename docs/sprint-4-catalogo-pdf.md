# Sprint 4 — Catálogo y PDF de cotizaciones

## Objetivo

Extender el flujo de cotizaciones con un catálogo reutilizable de productos y servicios y una representación PDF profesional descargable.

## Alcance implementado

- Entidad `CatalogItem` para productos y servicios.
- Código único, nombre, tipo, descripción, precio unitario e IVA.
- Alta, consulta, edición y desactivación lógica.
- Permisos `catalog.read` y `catalog.manage`.
- Workspace React en `/catalog`.
- Generación de PDF con QuestPDF.
- Endpoint protegido de descarga de PDF.
- Migración `AddCatalogItems`.
- Pruebas de dominio para normalización, impuestos y baja lógica.

## Endpoints

- `GET /api/v1/catalog`
- `POST /api/v1/catalog`
- `PUT /api/v1/catalog/{id}`
- `DELETE /api/v1/catalog/{id}`
- `GET /api/v1/quotes/{id}/pdf`

## Decisión de arquitectura para envíos

La generación del documento queda separada del envío. El siguiente bloque incorporará un adaptador de notificaciones que podrá usar SMTP para correo y Evolution API para WhatsApp. Las credenciales y URLs de proveedores deberán configurarse mediante variables de entorno y no almacenarse en el dominio ni en el repositorio.

## Criterios de aceptación

- Un administrador puede administrar el catálogo.
- El código del catálogo es único.
- Los elementos inactivos no aparecen en la consulta estándar.
- Un usuario con `quotes.read` puede descargar una cotización en PDF.
- El PDF muestra empresa, folio, vigencia, partidas, impuestos, descuento y total.
- Backend, pruebas y frontend compilan en GitHub Actions.
