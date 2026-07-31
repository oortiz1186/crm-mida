# Sprint 8 · Empresas con autenticación central

## Objetivo

Retirar del módulo de Empresas el formulario de acceso, token, encabezado y cierre de sesión locales.

## Cambios

- Nuevo `CompaniesApp` conectado a `AuthProvider`.
- Uso de `apiFetch` y `apiJson` para todas las operaciones autenticadas.
- La ruta `/` utiliza el nuevo workspace.
- Permisos de edición controlados con `companies.manage`.
- Se conservan listado, búsqueda, alta, edición y desactivación de empresas.
- Se conservan alta, edición y desactivación de contactos.

## Compatibilidad

El archivo heredado `App.tsx` permanece temporalmente en el repositorio, pero ya no se utiliza como ruta principal. Se retirará cuando termine la migración de los módulos restantes.

## Fuera de alcance

- Integración con Licencias MIDA.
- Integración con Soporte MIDA.
- Migración del módulo Prospectos, que se realizará en el siguiente bloque.
