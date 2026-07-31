# Sprint 5 · Job y notificaciones de licencias

## Objetivo

Ejecutar automáticamente el procesamiento de alertas y avisar a los responsables sin duplicar actividades durante el mismo día.

## Programación

El servicio `LicenseAlertBackgroundService` revisa la configuración cada cinco minutos y ejecuta una sola vez por fecha local al alcanzar la hora configurada.

```text
LicenseAlerts__Enabled=true
LicenseAlerts__TimeZone=America/Mexico_City
LicenseAlerts__Hour=8
LicenseAlerts__Days=90
LicenseAlerts__NotifyEmail=true
LicenseAlerts__NotifyWhatsApp=false
```

El job permanece desactivado de forma predeterminada.

## Destinatarios

- Correo: asesor asignado a la empresa de la licencia.
- WhatsApp: móvil o teléfono del contacto principal activo de la empresa.

## Proveedores

Las notificaciones reutilizan la configuración existente:

```text
QuoteDelivery__Smtp__Host
QuoteDelivery__Smtp__Port
QuoteDelivery__Smtp__EnableSsl
QuoteDelivery__Smtp__User
QuoteDelivery__Smtp__Password
QuoteDelivery__Smtp__From

QuoteDelivery__Evolution__BaseUrl
QuoteDelivery__Evolution__Instance
QuoteDelivery__Evolution__ApiKey
```

## Seguridad y operación

- No existen credenciales dentro del repositorio.
- El registro `license_alert_dispatches` evita duplicar alertas por licencia, nivel y fecha.
- Una falla del proveedor se registra en logs y no detiene el procesamiento de las demás licencias.
- Para despliegues con varias instancias debe habilitarse el job en una sola instancia o migrarse posteriormente a un scheduler distribuido.
