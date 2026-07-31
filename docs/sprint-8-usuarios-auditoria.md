# Sprint 8 · Usuarios, roles y auditoría transversal

## Alcance

Este bloque completa la administración de seguridad del MVP sin integrar Licencias MIDA ni Soporte MIDA.

## Administración de usuarios

Ruta web: `/users-audit`.

Operaciones disponibles:

- listar usuarios y sus roles;
- crear usuarios con contraseña inicial;
- editar nombre, correo y roles;
- activar y desactivar cuentas;
- impedir que un administrador desactive su propia cuenta;
- desbloquear cuentas;
- restablecer contraseñas.

Endpoints:

- `GET /api/v1/administration/roles`
- `GET /api/v1/administration/users`
- `POST /api/v1/administration/users`
- `PUT /api/v1/administration/users/{id}`
- `POST /api/v1/administration/users/{id}/status`
- `POST /api/v1/administration/users/{id}/unlock`
- `POST /api/v1/administration/users/{id}/password`

Todas las operaciones requieren `companies.manage` en esta primera versión.

## Auditoría transversal

El middleware `AuditTrailMiddleware` registra operaciones autenticadas `POST`, `PUT`, `PATCH` y `DELETE` que terminen con una respuesta exitosa dentro de `/api/v1`.

La bitácora registra:

- usuario y correo;
- método y ruta;
- acción normalizada;
- tipo e identificador de entidad cuando están disponibles;
- código de respuesta;
- identificador de traza;
- fecha UTC.

Las operaciones administrativas sensibles también generan eventos explícitos, como creación de usuarios, cambio de roles, activación, desactivación, desbloqueo y restablecimiento de contraseña.

## Pendiente

- permisos administrativos específicos separados de `companies.manage`;
- sesiones activas y revocación de tokens;
- segundo factor de autenticación;
- integración con directorio corporativo;
- conexión con Licencias MIDA;
- conexión con Soporte MIDA.
