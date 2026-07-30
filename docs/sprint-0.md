# Sprint 0 — Base técnica

## Objetivo

Establecer una base compilable, mantenible y preparada para el primer vertical funcional del CRM MIDA.

## Alcance

- Monorepo con backend y frontend.
- Capas Domain, Application, Infrastructure y API.
- Aplicación React con Material UI.
- PostgreSQL mediante Docker Compose.
- Endpoint de salud versionado.
- Flujo automático de compilación para Pull Requests.

## Siguiente vertical

1. Entidad de usuario.
2. Roles y permisos.
3. Autenticación JWT.
4. Inicio de sesión.
5. Layout privado y dashboard inicial.
6. Auditoría básica.

## Convenciones

- Identificadores `Guid`.
- Fechas internas en UTC.
- API bajo `/api/v1`.
- Configuración sensible mediante variables de entorno.
- Cambios funcionales mediante ramas y Pull Requests.
