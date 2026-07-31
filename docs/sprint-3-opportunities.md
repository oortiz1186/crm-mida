# Sprint 3 — Oportunidades, actividades y pipeline

## Objetivo

Completar el núcleo comercial inicial con oportunidades vinculadas a empresas, seguimiento mediante actividades y una vista Kanban por etapas.

## Alcance

- Entidad `Opportunity`.
- Entidad `Activity`.
- Etapas: prospección, calificación, diagnóstico, cotización, negociación, ganada y perdida.
- Monto estimado, probabilidad y fecha esperada de cierre.
- Vinculación con empresa, contacto, prospecto y asesor.
- Motivo obligatorio al marcar una oportunidad como perdida.
- Actividades de llamada, correo, reunión, tarea y demostración.
- Prioridad, vencimiento, responsable y estado de actividad.
- Permisos `opportunities.read`, `opportunities.manage`, `activities.read` y `activities.manage`.
- Migración `AddOpportunitiesAndActivities`.
- Pruebas de dominio.
- Workspace React en `/opportunities`.
- Pipeline Kanban con cambio de etapa.

## Endpoints

- `GET /api/v1/opportunities`
- `GET /api/v1/opportunities/{id}`
- `POST /api/v1/opportunities`
- `PUT /api/v1/opportunities/{id}`
- `PATCH /api/v1/opportunities/{id}/stage`
- `DELETE /api/v1/opportunities/{id}`
- `GET /api/v1/activities`
- `POST /api/v1/activities`
- `PUT /api/v1/activities/{id}`
- `PATCH /api/v1/activities/{id}/status`
- `DELETE /api/v1/activities/{id}`

## Criterios de aceptación

- Un usuario autorizado puede crear y editar oportunidades.
- El pipeline muestra oportunidades agrupadas por etapa.
- Cambiar una tarjeta de etapa actualiza el estado comercial.
- Ganada establece probabilidad de 100 % y perdida de 0 %.
- Marcar como perdida requiere un motivo.
- Una oportunidad puede registrar actividades y seguimientos.
- Backend, pruebas y frontend compilan en GitHub Actions.
