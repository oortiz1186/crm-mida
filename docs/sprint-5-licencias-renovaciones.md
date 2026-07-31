# Sprint 5 · Licencias y renovaciones

## Alcance de este bloque

Primer vertical del módulo de licencias conectado al CRM comercial.

### Licencias

- Empresa propietaria.
- Producto CONTPAQi o producto licenciado.
- Número de serie único y normalizado.
- Versión y tipo de licencia.
- Usuarios y empresas permitidas.
- Inicio y fin de vigencia.
- Estado calculado: `active`, `expiring` o `expired`.
- Búsqueda por empresa, producto o serie.
- Filtro de vencimientos próximos.

### Renovaciones

- Creación de una renovación pendiente desde una licencia.
- Prevención de renovaciones pendientes duplicadas.
- Creación automática de una oportunidad comercial vinculada.
- Monto estimado y fecha objetivo.
- Renovación efectiva de la vigencia.
- Cierre automático del registro de renovación.

## Endpoints

```text
GET  /api/v1/licenses
GET  /api/v1/licenses/{id}
POST /api/v1/licenses
PUT  /api/v1/licenses/{id}
POST /api/v1/licenses/{id}/renewals
POST /api/v1/licenses/{id}/renew
```

## Permisos

```text
licenses.read
licenses.manage
```

## Persistencia

La migración crea:

- `licenses`
- `renewal_opportunities`

La serie tiene índice único. Las consultas principales cuentan con índices por empresa, estado y fecha de vencimiento.

## Siguiente bloque

- Workspace React de licencias.
- Dashboard de vencimientos a 30, 60 y 90 días.
- Historial de renovaciones.
- Alertas y actividades automáticas.
- Importación inicial desde certificados y desde el sistema Licencias MIDA.
