# Sprint 5 · Dashboard y alertas de licencias

## Alcance

- Workspace React `/licenses`.
- Indicadores de licencias vencidas y con vencimiento a 30, 60 y 90 días.
- Alta de licencias desde la interfaz.
- Generación de renovación y oportunidad comercial.
- Historial de renovaciones por licencia.
- Endpoint de alertas comerciales configurable hasta 365 días.

## Endpoints

- `GET /api/v1/licenses/dashboard`
- `GET /api/v1/licenses/alerts?days=90`
- `GET /api/v1/licenses/{id}/renewals`

Todos requieren `licenses.read`; las acciones de renovación conservan `licenses.manage`.
