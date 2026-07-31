# Sprint 8 · Búsqueda global y navegación

## Objetivo

Reducir el tiempo necesario para localizar información comercial y abrir el módulo correspondiente desde cualquier pantalla privada del CRM.

## Endpoint

`GET /api/v1/search?q={texto}&limit={cantidad}`

- Requiere autenticación.
- Exige al menos dos caracteres.
- Limita cada grupo entre 1 y 20 resultados.
- Realiza búsquedas sin distinguir mayúsculas y minúsculas.

## Entidades incluidas

- Empresas por nombre comercial, razón social o RFC.
- Contactos por nombre, correo, teléfono o móvil.
- Prospectos por nombre, empresa, correo o RFC.
- Oportunidades por nombre, producto o servicio.
- Cotizaciones por folio o título.
- Catálogo por código o nombre.

## Interfaz

El botón **Buscar** está disponible en la esquina superior derecha de las pantallas privadas.

Atajo de teclado:

`Ctrl + K` o `Cmd + K`

La búsqueda aplica una espera de 300 ms para evitar solicitudes por cada pulsación, cancela solicitudes anteriores y agrupa los resultados por tipo.

Cada resultado incluye una ruta interna para abrir el módulo relacionado. Las empresas y contactos abren Cliente 360° con la empresa seleccionada mediante `companyId`.

## Alcance pendiente

- Licencias registradas mediante SQL controlado.
- Documentos y contenido interno de archivos.
- Historial de búsquedas recientes.
- Navegación lateral definitiva y menú adaptable para móvil.
- Integraciones con Licencias MIDA y Soporte MIDA.
