# Sprint 8 · Inicio de sesión único y rutas protegidas

## Objetivo

Establecer una sola entrada pública de autenticación para CRM MIDA y evitar que los módulos privados puedan abrirse sin una sesión válida.

## Implementación

- Nueva ruta pública `/login`.
- Nuevo componente `LoginPage` conectado a `AuthProvider`.
- Nuevo componente `ProtectedRoute`.
- Todas las rutas internas se renderizan dentro de `ProtectedRoute` y `AppShell`.
- El destino solicitado se conserva y se recupera después de iniciar sesión.
- Una sesión activa que intenta abrir `/login` se redirige a `/dashboard`.
- El portal `/public/quotes/:token` permanece fuera de la autenticación y del layout privado.

## Flujo

1. El usuario intenta abrir una ruta privada.
2. Si no existe sesión, se redirige a `/login`.
3. Después de autenticarse, vuelve a la ruta originalmente solicitada.
4. El JWT y el usuario se conservan mediante el `AuthProvider` central.
5. Las respuestas `401` continúan limpiando la sesión mediante el cliente API compartido.

## Compatibilidad temporal

Empresas, Prospectos y otros módulos antiguos todavía contienen formularios locales y estados de sesión internos. La protección global evita el acceso anónimo, pero esos formularios se retirarán por módulo en entregas posteriores para reducir el riesgo de regresiones en archivos grandes.

## Fuera de alcance

- Integración con Licencias MIDA.
- Integración con Soporte MIDA.
- MFA.
- Refresh tokens.
- Revocación centralizada de sesiones.
- Migración completa de todos los módulos al cliente `apiFetch`.
