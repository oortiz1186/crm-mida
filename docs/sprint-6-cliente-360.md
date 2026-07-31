# Sprint 6 · Cliente 360°

## Objetivo

Concentrar en una sola pantalla la relación comercial de una empresa usando únicamente información propia de CRM MIDA.

Las integraciones con Licencias MIDA y Soporte MIDA permanecen fuera de alcance y pendientes para una fase futura.

## API

`GET /api/v1/customers/{companyId}/360`

Requiere el permiso `companies.read`.

La respuesta incluye:

- datos generales de empresa;
- resumen comercial;
- contactos activos;
- oportunidades;
- cotizaciones;
- actividades;
- licencias registradas dentro del CRM;
- renovaciones pendientes asociadas a esas licencias.

## Frontend

Ruta: `/customers`

Permite seleccionar una empresa y consultar:

- indicadores principales;
- información general;
- contactos;
- oportunidades;
- cotizaciones;
- actividades;
- licencias y estado de renovación.

## Exclusiones explícitas

- No consume la API de Licencias MIDA.
- No consume la API de Soporte MIDA.
- No consulta tickets externos.
- No sincroniza certificados PDF.

## Próximos pasos

- buscador global;
- agenda comercial;
- documentos internos;
- reportes básicos;
- auditoría e importación desde Excel;
- integración con CONTPAQi en una fase separada.
