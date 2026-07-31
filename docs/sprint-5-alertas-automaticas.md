# Sprint 5 · Alertas automáticas de licencias

## Objetivo

Convertir vencimientos de licencias en actividades comerciales trazables, evitando avisos duplicados.

## Endpoints

- `POST /api/v1/licenses/alerts/process`
- `GET /api/v1/licenses/alerts/history`

## Procesamiento

El endpoint de proceso recibe un horizonte de 1 a 365 días. Para cada licencia vencida o próxima a vencer:

1. determina el nivel `expired`, `30_days`, `60_days` o `90_days`;
2. verifica si ese nivel ya fue procesado en la fecha actual;
3. crea una actividad comercial vinculada a la empresa y al asesor asignado;
4. registra el despacho en `license_alert_dispatches`.

La restricción única por licencia, tipo y fecha permite reejecutar el proceso sin generar duplicados.

## Seguridad

Ambos endpoints requieren autenticación y el permiso `licenses.manage`.

## Operación programada

El endpoint queda preparado para invocarse diariamente desde un job externo, cron, GitHub Actions, Hangfire o el futuro Workflow Engine. En esta entrega no se ejecuta automáticamente por reloj dentro de la API para evitar múltiples ejecuciones cuando existan varias réplicas del servicio.
