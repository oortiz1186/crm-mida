# Sprint 4 — Envío de cotizaciones

## Objetivo

Permitir que una cotización genere y entregue su PDF por correo electrónico o WhatsApp sin acoplar el dominio a un proveedor específico.

## Alcance

- Endpoint `POST /api/v1/quotes/{id}/send`.
- Canales `email` y `whatsapp`.
- PDF adjunto por SMTP.
- Enlace al PDF por Evolution API.
- Cambio automático de cotización `draft` a `sent` únicamente cuando el proveedor confirma el envío.
- Respuesta `409 Conflict` cuando el canal no está configurado.
- Respuesta de error real cuando el proveedor rechaza la solicitud.
- Configuración mediante variables de entorno o secretos del despliegue.

## Solicitud

```json
{
  "channel": "email",
  "recipient": "cliente@empresa.com",
  "message": "Compartimos la propuesta solicitada."
}
```

Para WhatsApp, `recipient` debe contener el número con código de país.

## Configuración SMTP

- `QuoteDelivery__Smtp__Host`
- `QuoteDelivery__Smtp__Port`
- `QuoteDelivery__Smtp__EnableSsl`
- `QuoteDelivery__Smtp__User`
- `QuoteDelivery__Smtp__Password`
- `QuoteDelivery__Smtp__From`

## Configuración Evolution API

- `QuoteDelivery__Evolution__BaseUrl`
- `QuoteDelivery__Evolution__Instance`
- `QuoteDelivery__Evolution__ApiKey`
- `QuoteDelivery__PublicApiUrl`

`PublicApiUrl` debe apuntar a la API publicada para que el destinatario pueda abrir `GET /api/v1/quotes/{id}/pdf`.

## Seguridad

- El endpoint requiere `quotes.manage`.
- Ninguna credencial debe almacenarse en Git.
- Los errores no se registran como envío exitoso.
- El sistema no marca la cotización como enviada si falta configuración.

## Pendientes del despliegue

- Configurar una cuenta SMTP real.
- Definir la instancia válida de Evolution API.
- Publicar la API bajo HTTPS.
- Evaluar un enlace público firmado para evitar compartir un endpoint protegido directamente.
- Incorporar historial persistente de entregas y reintentos en un sprint posterior.
