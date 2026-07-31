# Sprint 6 · CONTPAQi Comercial Premium · Fase 1

## Alcance

Esta fase agrega únicamente diagnóstico de conexión en modo lectura. No sincroniza ni modifica información de CONTPAQi.

## Configuración

```text
Contpaqi__ConnectionString=Server=SERVIDOR\\INSTANCIA;Database=adEMPRESA;User Id=usuario_lectura;Password=secreto;TrustServerCertificate=True;
Contpaqi__ConnectTimeoutSeconds=8
```

Se recomienda usar un usuario SQL con permisos exclusivamente de lectura sobre la base de la empresa.

## Endpoints

```http
GET  /api/v1/integrations/contpaqi/status
POST /api/v1/integrations/contpaqi/test
```

Ambos requieren el permiso `integration.manage`.

## Diagnóstico

La prueba devuelve:

- servidor e instancia;
- base seleccionada;
- versión de SQL Server;
- tablas conocidas detectadas;
- indicador de estructura compatible con Comercial Premium.

Tablas verificadas:

- `admClientes`
- `admProductos`
- `admDocumentos`
- `admMovimientos`
- `admMovimientosSerie`
- `admConceptos`

## Seguridad

- La cadena de conexión no se devuelve al frontend.
- Las credenciales se reciben mediante configuración externa.
- No se ejecutan operaciones `INSERT`, `UPDATE` o `DELETE` en CONTPAQi.
- Las integraciones con Licencias MIDA y Soporte MIDA permanecen pendientes.

## Siguiente fase

Una vez validada la conexión real, se agregará sincronización auditable e incremental de clientes y productos hacia CRM MIDA.
