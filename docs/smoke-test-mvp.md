# Smoke test · CRM MIDA MVP 1.0

Registrar fecha, ambiente, versión, navegador y responsable antes de comenzar.

## 1. Infraestructura

- [ ] PostgreSQL está saludable.
- [ ] La API inicia sin excepciones.
- [ ] El frontend carga mediante HTTPS en producción.
- [ ] `/login` está disponible.
- [ ] Una ruta privada sin sesión redirige a `/login`.
- [ ] PostgreSQL no está expuesto públicamente.

## 2. Autenticación y permisos

- [ ] El administrador puede iniciar sesión.
- [ ] Credenciales incorrectas muestran error.
- [ ] Cerrar sesión elimina el acceso privado.
- [ ] Una sesión vencida regresa a `/login`.
- [ ] Un usuario de consulta no ve acciones de edición.
- [ ] Un administrador sí ve las acciones autorizadas.

## 3. Empresa y contacto

- [ ] Crear una empresa de prueba con RFC único.
- [ ] Editar la empresa.
- [ ] Crear un contacto principal.
- [ ] Editar el contacto.
- [ ] Abrir la empresa desde Cliente 360°.

## 4. Prospecto

- [ ] Crear un prospecto.
- [ ] Cambiar su calificación y estado.
- [ ] Convertirlo en empresa.
- [ ] Confirmar que la empresa aparece en el listado.
- [ ] Confirmar que no se duplica un RFC existente.

## 5. Oportunidad y actividad

- [ ] Crear una oportunidad para una empresa.
- [ ] Moverla entre etapas.
- [ ] Crear una actividad asociada.
- [ ] Confirmar que aparece en Agenda y Dashboard.
- [ ] Marcar una oportunidad como perdida indicando motivo.

## 6. Cotización

- [ ] Crear una cotización con dos partidas.
- [ ] Confirmar subtotal, IVA, descuento y total.
- [ ] Editar la cotización.
- [ ] Descargar el PDF autenticado.
- [ ] Crear un enlace público.
- [ ] Abrir el enlace sin sesión privada.
- [ ] Aceptar o rechazar la cotización.
- [ ] Confirmar el cambio de estado en el CRM.
- [ ] Revocar un enlace y confirmar que deja de funcionar.

Los envíos SMTP y WhatsApp se prueban únicamente cuando sus proveedores estén configurados.

## 7. Catálogo

- [ ] Crear producto o servicio.
- [ ] Buscarlo por código o nombre.
- [ ] Editarlo.
- [ ] Desactivarlo.

## 8. Licencia y renovación

- [ ] Registrar una licencia interna.
- [ ] Confirmar su clasificación por vencimiento.
- [ ] Crear una renovación.
- [ ] Confirmar que se crea la oportunidad relacionada.
- [ ] Consultar el historial de renovaciones.

## 9. Documentos

- [ ] Subir un PDF permitido.
- [ ] Confirmar que aparece asociado a la empresa.
- [ ] Descargarlo.
- [ ] Reiniciar la API y confirmar que el documento persiste.
- [ ] Intentar subir un formato o tamaño no permitido y validar el error.

## 10. Importación Excel

- [ ] Importar un `.xlsx` válido.
- [ ] Confirmar creados, omitidos y errores.
- [ ] Repetir el archivo y confirmar que el RFC no se duplica.
- [ ] Confirmar el evento en auditoría.

## 11. Reportes

- [ ] Descargar pipeline CSV.
- [ ] Descargar cotizaciones CSV.
- [ ] Descargar actividades CSV.
- [ ] Abrir los archivos en Excel y comprobar caracteres y columnas.

## 12. Usuarios y auditoría

- [ ] Crear un usuario de prueba.
- [ ] Asignar un rol.
- [ ] Iniciar sesión con ese usuario.
- [ ] Desactivar y reactivar la cuenta.
- [ ] Probar bloqueo y desbloqueo cuando aplique.
- [ ] Restablecer contraseña.
- [ ] Confirmar eventos en auditoría.

## 13. Búsqueda y responsive

- [ ] Buscar con `Ctrl + K`.
- [ ] Abrir una empresa desde los resultados.
- [ ] Probar sidebar en escritorio.
- [ ] Probar menú en pantalla móvil.
- [ ] Confirmar que el portal público no muestra navegación privada.

## Resultado

- [ ] Todos los puntos críticos fueron aprobados.
- [ ] Los defectos encontrados tienen evidencia y prioridad.
- [ ] No existen bloqueadores para pruebas internas.

No marcar el PR como listo mientras exista un fallo crítico en autenticación, persistencia, migraciones, permisos, cotizaciones o documentos.
