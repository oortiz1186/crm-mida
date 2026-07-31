# Sprint 1 — Autenticación y seguridad base

## Objetivo

Entregar el primer vertical funcional del CRM MIDA con usuarios persistentes, roles, permisos, contraseñas seguras, autenticación JWT y acceso privado desde el frontend.

## Implementado

- Entidades `User`, `Role`, `Permission`, `UserRole` y `RolePermission`.
- PostgreSQL mediante Entity Framework Core.
- Contraseñas protegidas con BCrypt y factor de trabajo 12.
- Bloqueo temporal después de cinco intentos fallidos.
- Seeding idempotente de roles y permisos base.
- Creación opcional del administrador inicial mediante variables de entorno.
- `POST /api/v1/auth/login`.
- `GET /api/v1/auth/me` protegido con JWT.
- Swagger configurado para Bearer Token.
- Pantalla de inicio de sesión.
- Dashboard privado inicial con nombre, rol y permisos del usuario.
- Cierre de sesión en el cliente.

## Configuración local

Definir como mínimo:

```text
CRM_ADMIN_EMAIL=admin@mida.mx
CRM_ADMIN_PASSWORD=una_contraseña_segura
CRM_ADMIN_FIRST_NAME=Administrador
CRM_ADMIN_LAST_NAME=MIDA
Jwt__Secret=una_clave_segura_de_al_menos_32_caracteres
```

El administrador solo se crea cuando el correo y la contraseña están configurados y todavía no existe un usuario con ese correo.

## Endpoints

| Método | Ruta | Autorización | Descripción |
|---|---|---|---|
| GET | `/api/v1/health` | Pública | Estado de la API |
| POST | `/api/v1/auth/login` | Pública | Inicio de sesión |
| GET | `/api/v1/auth/me` | JWT | Identidad, roles y permisos actuales |

## Seguridad

- Los errores de login no revelan si el correo existe.
- Las contraseñas no se almacenan ni se devuelven en texto plano.
- El token incluye identificador, nombre, roles y permisos.
- La clave JWT productiva debe proporcionarse mediante variable de entorno.
- Después de cinco intentos fallidos, la cuenta queda bloqueada durante quince minutos.

## Siguiente vertical

1. Persistencia formal mediante migraciones versionadas.
2. Refresh token y cierre de sesiones del lado servidor.
3. Recuperación y cambio de contraseña.
4. Administración de usuarios, roles y permisos.
5. Auditoría de accesos y acciones sensibles.
6. Inicio del módulo de empresas y contactos.
