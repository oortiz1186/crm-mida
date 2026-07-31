# Sprint 8 · AuthProvider central

## Objetivo

Centralizar el estado de autenticación del frontend para dejar de administrar token, usuario, permisos y cierre de sesión desde componentes independientes.

## Implementación

- `AuthProvider` mantiene token, usuario y estado de autenticación.
- Expone `login`, `logout`, `hasPermission` y `hasAnyPermission`.
- `AppShell` consume el contexto para perfil, permisos y cierre de sesión.
- `apiFetch` agrega automáticamente el JWT y limpia la sesión ante respuestas 401.
- Se mantiene temporalmente `installAuthPersistence` para compatibilidad con los formularios de acceso heredados.

## Migración gradual

Los módulos existentes aún pueden conservar temporalmente su estado local de formulario. La siguiente fase consiste en reemplazar esos formularios por una pantalla única y mover las solicitudes a `apiFetch`/`apiJson`.

## Fuera de alcance

- Integración con Licencias MIDA.
- Integración con Soporte MIDA.
- MFA.
- Refresh tokens.
- Revocación centralizada de sesiones.
