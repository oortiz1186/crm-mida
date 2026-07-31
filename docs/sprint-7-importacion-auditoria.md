# Sprint 7 — Importación desde Excel y auditoría básica

## Objetivo

Cerrar los dos elementos restantes de la primera entrega funcional del MVP sin depender de Licencias MIDA ni Soporte MIDA.

## Importación de empresas

Endpoint protegido:

```http
POST /api/v1/import/companies
```

Formato: `multipart/form-data`, campo `file`, archivo `.xlsx`.

Columnas obligatorias:

- Nombre comercial
- Razón social
- RFC

Columnas opcionales:

- Tipo cliente
- Régimen fiscal
- Código postal
- Correo
- Teléfono
- Sitio web
- Dirección
- Ciudad
- Estado
- Etiquetas

Los encabezados no distinguen mayúsculas, espacios ni acentos. Los RFC existentes se omiten para evitar duplicados. Las filas inválidas se devuelven con número de fila y descripción.

## Historial de importaciones

```http
GET /api/v1/import/jobs
```

Registra archivo, estado, altas, omitidos, errores, inicio, fin y mensaje de error.

## Auditoría

```http
GET /api/v1/audit?action={action}&entityType={entityType}&limit=100
```

La primera versión registra importaciones correctas y fallidas, usuario, entidad, detalle JSON y fecha UTC. La estructura queda preparada para extender la auditoría a cambios de empresas, oportunidades, cotizaciones, renovaciones y configuración.

## Frontend

Ruta:

```text
/administration
```

Incluye carga de Excel, resumen del procesamiento, historial de importaciones y bitácora.

## Seguridad

Todas las operaciones requieren `companies.manage`, reservado inicialmente al rol Administrador.

## Fuera de alcance

- Sincronización con Licencias MIDA.
- Sincronización con Soporte MIDA.
- Importación de contactos, oportunidades o licencias.
- Reversión automática de importaciones.
