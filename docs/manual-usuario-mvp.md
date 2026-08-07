# Manual breve de usuario · CRM MIDA MVP 1.0

## Acceso

1. Abre `/login`.
2. Ingresa correo y contraseña.
3. El menú mostrará únicamente los módulos permitidos para tu cuenta.
4. Para cerrar sesión usa el perfil del encabezado.

## Dashboard

La pantalla inicial muestra:

- actividades vencidas;
- actividades de hoy;
- próximos seguimientos;
- oportunidades abiertas;
- pipeline ponderado;
- cotizaciones próximas a vencer.

## Empresas y contactos

En **Empresas** puedes:

- buscar por nombre, razón social o RFC;
- registrar y editar empresas;
- agregar contactos;
- identificar contactos principales, técnicos, de compras o facturación;
- desactivar registros sin eliminarlos físicamente.

## Prospectos

En **Prospectos** puedes:

- registrar interesados;
- indicar origen, interés y calificación;
- actualizar su estado;
- convertir un prospecto calificado en empresa.

La conversión puede crear también un contacto principal cuando existen datos suficientes.

## Cliente 360°

Selecciona una empresa para consultar:

- resumen comercial;
- contactos;
- oportunidades;
- cotizaciones;
- actividades;
- licencias internas y renovaciones.

## Pipeline

Las oportunidades se organizan por etapa:

- prospección;
- calificación;
- diagnóstico;
- cotización;
- negociación;
- ganada;
- perdida.

Cada oportunidad puede contener monto, probabilidad, fecha estimada, notas y actividades.

## Cotizaciones

Una cotización permite:

- seleccionar empresa;
- agregar partidas;
- definir cantidad, precio e IVA;
- aplicar descuento;
- establecer vigencia;
- descargar PDF;
- enviar cuando SMTP o Evolution estén configurados;
- crear un enlace público para aceptación o rechazo;
- consultar intentos y revocar enlaces.

## Catálogo

Registra productos y servicios con:

- código;
- nombre;
- descripción;
- precio;
- IVA;
- estado activo.

## Licencias y renovaciones

Las licencias internas guardan empresa, producto, serie, versión, usuarios y vigencia.

El dashboard clasifica vencidas y próximas a vencer. Desde una licencia puede generarse una renovación vinculada a una oportunidad comercial.

Este módulo no es todavía la integración con el sistema externo Licencias MIDA.

## Actividades y agenda

Las actividades pueden ser llamadas, reuniones, correos, tareas o demostraciones. Deben incluir fecha, prioridad y estado.

La agenda permite consultar vencidos, hoy y próximos seguimientos.

## Documentos y reportes

En **Documentos y reportes** puedes:

- seleccionar una empresa;
- subir archivos permitidos;
- clasificar y describir documentos;
- descargar archivos autenticados;
- exportar pipeline, cotizaciones y actividades en CSV.

## Importación Excel

La importación de empresas requiere como mínimo:

- Nombre comercial;
- Razón social;
- RFC.

El resultado informa registros creados, omitidos y con error. Los RFC existentes no se duplican.

## Usuarios y auditoría

Los administradores pueden:

- crear usuarios;
- asignar roles;
- activar o desactivar cuentas;
- desbloquear usuarios;
- restablecer contraseñas;
- consultar eventos de auditoría.

## Búsqueda global

Usa el botón **Buscar** o:

```text
Ctrl + K
```

En macOS usa `Cmd + K`.

## Buenas prácticas

- No compartir contraseñas.
- Cerrar sesión en equipos compartidos.
- No subir documentos con información innecesaria.
- Registrar actividades con fecha y responsable.
- Evitar crear empresas duplicadas; buscar primero por RFC.
- Reportar errores con captura, hora aproximada y acción realizada.
