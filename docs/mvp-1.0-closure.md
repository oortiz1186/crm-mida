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
- [ ] Resto de módulos sin login o sesión local duplicada.
- [ ] Respuestas 401 regresan a `/login`.
- [ ] Opciones y acciones respetan permisos.

### Funcionalidad

- [ ] Flujo Empresa → Contacto → Oportunidad → Cotización funciona completo.
- [ ] Flujo Prospecto → Empresa funciona completo.
- [ ] Flujo Licencia → Renovación → Oportunidad funciona completo.
- [ ] PDF y portal público de cotización funcionan.
- [ ] Documentos se cargan y descargan con autenticación.
- [ ] Importación Excel registra resultado y auditoría.

### Calidad

- [ ] Backend compila en Release.
- [ ] Pruebas de dominio pasan.
- [ ] Frontend compila con TypeScript.
- [ ] No hay secretos en el repositorio.
- [ ] Configuración de producción documentada.
- [ ] Migraciones aplican sobre una base vacía.
- [ ] Smoke test manual documentado.

### Entrega

- [ ] README de instalación actualizado.
- [ ] Variables de entorno documentadas.
- [ ] Manual breve de usuario.
- [ ] Checklist de despliegue.
- [ ] PR final aprobado.
- [ ] Tag `v1.0.0` después de fusionar.
