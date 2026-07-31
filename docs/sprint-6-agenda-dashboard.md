# Sprint 6 · Agenda comercial y Dashboard

## Objetivo

Priorizar el trabajo diario de los asesores con información interna de CRM MIDA, sin depender de Licencias MIDA ni Soporte MIDA.

## Endpoints

- `GET /api/v1/workspace/dashboard`
- `GET /api/v1/workspace/agenda?from=&to=&status=`

## Dashboard

Incluye actividades vencidas, actividades de hoy, próximos siete días, oportunidades abiertas, pipeline ponderado y cotizaciones próximas a vencer.

## Agenda

Permite consultar actividades por rango de fechas y estado. Se reutiliza la entidad `Activity`, por lo que no se agrega una tabla duplicada.

## Frontend

Ruta `/dashboard` con indicadores y listas priorizadas.

## Fuera de alcance

- Sincronización con Licencias MIDA.
- Integración con Soporte MIDA.
- Calendarios externos.
- Notificaciones push.
