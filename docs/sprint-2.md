# Sprint 2 — Empresas y contactos

## Objetivo

Construir el primer vertical comercial utilizable del CRM MIDA sobre la autenticación del Sprint 1.

## Alcance completado

- Empresas con datos comerciales, fiscales, dirección, estado, etiquetas, asesor y referencia CONTPAQi.
- Contactos relacionados con empresa.
- Contacto principal único por empresa.
- Roles de contacto para compras, soporte técnico y facturación.
- Consentimiento para comunicaciones comerciales.
- Búsqueda por nombre comercial, razón social o RFC.
- Paginación y eliminación lógica.
- Interfaz React para listar, crear, editar y consultar empresas y contactos.
- Layout privado con navegación y cierre de sesión.
- Permisos diferenciados de lectura y administración.
- Migración inicial de Entity Framework para seguridad, empresas y contactos.
- Pruebas de dominio para normalización, desactivación y contacto principal.

## Endpoints

- `GET /api/v1/companies`
- `GET /api/v1/companies/{id}`
- `POST /api/v1/companies`
- `PUT /api/v1/companies/{id}`
- `DELETE /api/v1/companies/{id}`
- `GET /api/v1/companies/{companyId}/contacts`
- `POST /api/v1/companies/{companyId}/contacts`
- `PUT /api/v1/contacts/{id}`
- `DELETE /api/v1/contacts/{id}`

## Permisos

- `companies.read`
- `companies.manage`
- `contacts.read`
- `contacts.manage`

Las operaciones GET requieren permisos de lectura. Las operaciones POST, PUT y DELETE requieren permisos de administración.

## Base de datos

La aplicación utiliza `Database.MigrateAsync()` al iniciar. La migración inicial es idempotente para facilitar la transición desde instalaciones locales creadas previamente con `EnsureCreatedAsync()`.

## Validación

- Compilación backend en Release.
- Pruebas de dominio con xUnit.
- Compilación frontend con TypeScript y Vite.
- Validación automática mediante GitHub Actions.

## Siguiente vertical

Sprint 3: prospectos, conversión a empresa, oportunidades, etapas comerciales y actividades de seguimiento.
