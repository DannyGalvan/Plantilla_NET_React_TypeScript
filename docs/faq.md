# Preguntas frecuentes

**¿Puedo usar solo el backend o solo el frontend?**
Sí. La API funciona sola (`Hosting:ServeSpa=false`) y la SPA puede consumir cualquier API que respete los mismos contratos.

**¿Cómo cambio de PostgreSQL a SQL Server en un proyecto existente?**
Cambia `Database:Provider` y la cadena. Las migraciones de los tres motores ya existen. Los datos se migran con
herramientas del motor; la plantilla no copia datos entre motores.

**¿Puedo empezar con `Mode=None` y pasar a SaaS después?**
Sí. Todas las entidades de negocio ya tienen `TenantId` (con el tenant por defecto). Cambia a `SharedDatabase` y crea
los nuevos tenants.

**¿Por qué los permisos no están en el token?**
Para que el token sea pequeño y los cambios de permisos apliquen sin volver a iniciar sesión. Se resuelven en el
servidor con caché ([ADR 0007](adr/0007-permisos-en-servidor.md)).

**¿Por qué Minimal APIs y no controllers?**
Menos código por endpoint y agrupación natural por módulo ([ADR 0003](adr/0003-minimal-apis.md)).

**¿Necesito escribir pruebas de aislamiento para cada módulo?**
Las garantías vienen de los filtros globales y del pipeline de guardado, que están probados. Agrega pruebas propias
cuando escribas SQL crudo.

**¿Puedo usar otro ORM?**
EF Core es la base de escritura. Para lecturas puedes usar Dapper o ADO.NET con `IDbConnectionFactory`.

**¿Cómo agrego un idioma?**
Ver [guias/i18n.md](guias/i18n.md).

**¿Hay pruebas end-to-end en navegador?**
No se incluyen. Las pruebas funcionales cubren la API real y las de componentes cubren la SPA. Puedes agregar Playwright
contra el stack de `docker-compose.prod.yml`.
