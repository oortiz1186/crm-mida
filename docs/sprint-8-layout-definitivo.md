# Sprint 8 — Layout definitivo y sesión compartida

## Alcance

- Sustituye la navegación flotante por un layout de aplicación.
- Agrega sidebar permanente en escritorio y Drawer temporal en móvil.
- Integra encabezado, búsqueda global, perfil y cierre de sesión.
- Filtra opciones de navegación de acuerdo con los permisos del usuario.
- Conserva el portal público de cotizaciones sin navegación privada.
- Agrega una capa de compatibilidad que persiste los inicios de sesión existentes en `sessionStorage`.

## Componentes

- `frontend/src/AppShell.tsx`
- `frontend/src/session.ts`
- `frontend/src/main.tsx`

## Sesión compartida

Mientras se migra cada módulo a un proveedor de autenticación único, `installAuthPersistence()` observa las respuestas exitosas de `/api/v1/auth/login` y conserva:

- `crm_access_token`
- `crm_current_user`

El layout escucha cambios de sesión y se activa sin exigir una recarga manual.

## Navegación por permisos

Las opciones se muestran cuando el usuario posee al menos el permiso de lectura del módulo. Administración, usuarios y auditoría requieren actualmente `companies.manage`, hasta introducir permisos administrativos separados.

## Pendiente

- Eliminar los encabezados y formularios de acceso duplicados dentro de cada módulo.
- Crear un `AuthProvider` único y rutas privadas declarativas.
- Separar permisos `users.read`, `users.manage`, `roles.manage` y `audit.read`.
- Agregar breadcrumb y títulos de página centralizados.
- Integración con Licencias MIDA y Soporte MIDA continúa fuera de alcance.
