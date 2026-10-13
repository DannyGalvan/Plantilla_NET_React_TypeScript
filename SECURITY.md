# Política de seguridad

## Versiones soportadas

Se corrigen vulnerabilidades en la última versión publicada de la rama `master`.

## Reportar una vulnerabilidad

No abras un issue público. Usa el [aviso privado de seguridad de GitHub](https://github.com/DannyGalvan/Plantilla_NET_React_TypeScript/security/advisories/new)
e incluye la versión, los pasos para reproducir y el impacto. Recibirás respuesta en un máximo de 5 días hábiles.

## Controles incluidos en la plantilla

Resumen en [docs/guias/seguridad.md](docs/guias/seguridad.md): JWT de vida corta, refresh token rotativo con detección
de reuso, CSRF de doble envío, bloqueo por intentos fallidos, historial de contraseñas, permisos evaluados en el
servidor, endpoints fail-closed, aislamiento por tenant y por unidad, cabeceras de seguridad, rate limiting y errores
sin detalles internos.
