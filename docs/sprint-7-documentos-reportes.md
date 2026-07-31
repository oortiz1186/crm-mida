# Sprint 7 · Documentos y reportes básicos

## Alcance

Este bloque agrega documentos asociados a empresas y exportaciones operativas sin integrar Licencias MIDA ni Soporte MIDA.

## Documentos

Ruta interna: `/documents-reports`.

Endpoints:

- `GET /api/v1/companies/{companyId}/documents`
- `POST /api/v1/companies/{companyId}/documents`
- `GET /api/v1/companies/{companyId}/documents/{documentId}/download`
- `DELETE /api/v1/companies/{companyId}/documents/{documentId}`

Tipos permitidos: PDF, Excel, Word, PNG y JPEG. Tamaño máximo: 20 MB.

Los metadatos se almacenan en PostgreSQL y los binarios en almacenamiento local configurable mediante `Documents__StoragePath`. En producción debe montarse un volumen persistente y respaldado.

## Reportes

- `GET /api/v1/reports/pipeline.csv`
- `GET /api/v1/reports/quotes.csv`
- `GET /api/v1/reports/activities.csv`

Los CSV se generan bajo demanda, incluyen BOM UTF-8 y requieren autenticación.

## Seguridad

- Lectura y descarga: `companies.read`.
- Carga y baja lógica: `companies.manage`.
- Las descargas se realizan mediante JWT; no existen enlaces públicos.
- La subida y eliminación generan eventos en `audit_logs`.

## Pendiente

- Almacenamiento compatible con S3/MinIO.
- Versionado de documentos.
- Antivirus y análisis de contenido.
- Reportes PDF ejecutivos.
- Integraciones con Licencias MIDA y Soporte MIDA.
