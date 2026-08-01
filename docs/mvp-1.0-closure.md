# CRM MIDA · Cierre MVP 1.0

## Objetivo

Estabilizar el núcleo comercial existente y producir una versión utilizable para pruebas internas en MIDA, sin incorporar integraciones externas nuevas.

## Incluido

- Autenticación única y rutas protegidas.
- Empresas, contactos y prospectos usando sesión central.
- Oportunidades, pipeline, actividades y agenda.
- Cotizaciones, PDF, envío configurable, historial y portal público.
- Catálogo de productos y servicios.
- Licencias internas y renovaciones.
- Cliente 360°.
- Dashboard comercial.
- Documentos por empresa.
- Importación de empresas desde Excel.
- Reportes CSV.
- Usuarios, roles, permisos y auditoría.
- Búsqueda global y layout responsive.

## Pendiente expresamente para versión posterior

- Integración con Licencias MIDA.
- Integración con Soporte MIDA.
- Integración con CONTPAQi.
- WhatsApp operativo fuera de las configuraciones existentes.
- IA, marketing, portal general de clientes y app móvil.

## Criterios de aceptación

### Autenticación y seguridad

- [x] Una sola ruta pública de acceso.
- [x] Rutas privadas protegidas.
- [x] AuthProvider central.
- [x] Empresas sin login local.
- [x] Prospectos sin login local.
- [x] Oportunidades y Pipeline sin login local.
- [x] Catálogo sin login local.
- [x] Cotizaciones sin login o sesión local duplicada.
- [x] Licencias y Cliente 360° sin sesión local duplicada.
- [x] Administración, documentos y usuarios usan cliente API compartido.
- [x] Respuestas 401 regresan a `/login` conservando la ruta solicitada.
- [x] Acciones principales respetan permisos de gestión.

### Funcionalidad

- [ ] Flujo Empresa → Contacto → Oportunidad → Cotización validado mediante smoke test.
- [ ] Flujo Prospecto → Empresa validado mediante smoke test.
- [ ] Flujo Licencia → Renovación → Oportunidad validado mediante smoke test.
- [ ] PDF autenticado y portal público de cotización validados en entorno ejecutable.
- [ ] Documentos cargados y descargados en entorno ejecutable.
- [ ] Importación Excel registra resultado y auditoría en entorno ejecutable.

### Calidad

- [x] Backend compila en Release.
- [x] Pruebas de dominio pasan.
- [x] Frontend compila con TypeScript.
- [ ] No hay secretos en el repositorio.
- [ ] Configuración de producción documentada.
- [ ] Migraciones aplican sobre una base vacía.
- [ ] Smoke test manual documentado y ejecutado.

### Entrega

- [ ] README de instalación actualizado.
- [ ] Variables de entorno documentadas.
- [ ] Manual breve de usuario.
- [ ] Checklist de despliegue.
- [ ] PR final aprobado.
- [ ] Tag `v1.0.0` después de fusionar.
