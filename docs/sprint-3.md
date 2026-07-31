# Sprint 3 — Prospectos

## Objetivo

Incorporar la primera etapa del proceso comercial del CRM MIDA: captura, consulta, calificación, seguimiento básico y conversión de prospectos a empresas.

## Alcance implementado

- Entidad `Prospect`.
- Estados: nuevo, contactado, calificado, no calificado, convertido y descartado.
- Calificación comercial: sin calificar, frío, tibio y caliente.
- Origen, interés, asesor, notas y datos de contacto.
- CRUD protegido por `prospects.read` y `prospects.manage`.
- Búsqueda, filtro por estado y paginación.
- Conversión a empresa sin duplicar RFC.
- Creación automática de contacto principal cuando existen datos de contacto.
- Migración `AddProspects`.
- Pruebas de normalización, conversión y baja lógica.
- Workspace React en `/prospects`.

## Endpoints

- `GET /api/v1/prospects`
- `GET /api/v1/prospects/{id}`
- `POST /api/v1/prospects`
- `PUT /api/v1/prospects/{id}`
- `DELETE /api/v1/prospects/{id}`
- `POST /api/v1/prospects/{id}/convert`

## Criterios de aceptación

- Un usuario autorizado puede registrar y editar prospectos.
- La búsqueda considera nombre, empresa, RFC y correo.
- Un prospecto convertido genera una empresa y conserva la trazabilidad.
- No se permite convertir a un RFC ya registrado.
- Un prospecto convertido no puede modificarse ni convertirse nuevamente.
- Backend, pruebas y frontend compilan en GitHub Actions.

## Siguiente bloque

Oportunidades comerciales, etapas de pipeline, actividades y vista Kanban.
