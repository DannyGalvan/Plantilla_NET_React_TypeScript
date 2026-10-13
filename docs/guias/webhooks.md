# Webhooks

**Para qué sirve este documento:** publicar eventos de un módulo hacia sistemas externos y verificar las entregas del
lado receptor.
**Requisitos previos:** módulo `Webhooks` activo (por defecto).
**Tiempo estimado:** 15 minutos.

## Publicar un evento

Un evento interno no sale de la aplicación hasta que su módulo lo declara con un nombre público estable:

```csharp
services.AddWebhookEvent<EmployeeHired>("hr.employee.hired", "Se contrató a una persona.");
```

El cuerpo de la entrega es el evento serializado en JSON. Publica solo eventos cuyos campos puedan salir de la
aplicación.

Eventos incluidos:

| Nombre | Módulo | Cuándo |
|---|---|---|
| `records.changed` | Audit | Se creó, cambió, borró o restauró un registro de cualquier módulo |
| `workflows.approval.completed` | Workflows | Una solicitud se aprobó, rechazó o canceló |
| `webhooks.ping` | Webhooks | Prueba manual desde la página *Webhooks* |

## Suscribirse

En *Webhooks* (permiso `webhooks.subscriptions.manage`) crea una suscripción con su URL `https` y sus eventos. Al
crearla se muestra **una sola vez** el secreto de firma (`whsec_...`); se guarda cifrado.

## Formato de la entrega

```http
POST /tu-endpoint HTTP/1.1
Content-Type: application/json
X-Webhook-Id: 0199c2d4-...
X-Webhook-Event: records.changed
X-Webhook-Signature: t=1760313600,v1=5f2b...

{"id":"0199c2d4-...","event":"records.changed","occurredAt":"2026-10-13T00:00:00+00:00","data":{...}}
```

- **Firma.** `v1` es `HMAC-SHA256(secreto, "<t>.<cuerpo>")` en hexadecimal.
- **Repeticiones.** Rechaza firmas con un `t` de más de 5 minutos de antigüedad.
- **Duplicados.** La entrega es al menos una vez. Usa `X-Webhook-Id`, que es el mismo en cada reintento, para no
  procesar dos veces.
- **Respuesta.** Responde `2xx` en menos de 10 segundos. Cualquier otra respuesta se reintenta con espera exponencial
  (1, 2, 4, 8... minutos) hasta 8 intentos. Después la entrega queda como *Fallido* y se puede reintentar a mano.

## Verificar la firma

C#:

```csharp
var parts = signature.Split(',').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p[1]);
var expected = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{parts["t"]}.{body}")));
var valid = CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(parts["v1"]))
    && DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts["t"])) < TimeSpan.FromMinutes(5);
```

Node.js:

```js
import { createHmac, timingSafeEqual } from 'node:crypto';

const { t, v1 } = Object.fromEntries(signature.split(',').map((p) => p.split('=')));
const expected = createHmac('sha256', secret).update(`${t}.${rawBody}`).digest('hex');
const valid = timingSafeEqual(Buffer.from(expected), Buffer.from(v1)) && Date.now() / 1000 - Number(t) < 300;
```

Calcula la firma sobre el cuerpo **sin modificar**: lee los bytes crudos antes de parsear el JSON.

## Seguridad (SSRF)

- Solo URL `https`, sin credenciales en la URL.
- La conexión solo se abre hacia direcciones públicas. La IP se comprueba **después** de resolver el DNS, así que un
  dominio que apunta a `10.x`, `169.254.169.254` o `localhost` se rechaza.
- No se siguen redirecciones.

Para probar contra un servidor local en desarrollo usa `Webhooks:AllowPrivateNetworks=true`. Nunca lo actives en
producción.

## Cómo verificar que funcionó

En *Webhooks* pulsa **Enviar prueba**. En *Entregas* debe aparecer `webhooks.ping` como *Entregado*, con el código de
respuesta de tu endpoint.

## Problemas comunes

| Síntoma | Causa |
|---|---|
| `The URL points to a private network` al guardar | La URL es una IP privada o `localhost`, y `AllowPrivateNetworks` está apagado |
| Entregas *Pendiente* con error `resolves to a private network` | El dominio resuelve a una IP privada |
| Firma inválida en el receptor | El receptor firmó el JSON reformateado en vez del cuerpo original |
