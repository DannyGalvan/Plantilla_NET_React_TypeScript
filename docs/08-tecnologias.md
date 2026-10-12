# Tecnologías

Todas las dependencias tienen licencias permisivas (MIT, Apache 2.0 o BSD), compatibles con uso comercial y con la
licencia MIT de la plantilla. Agregar una dependencia GPL, AGPL o comercial requiere un ADR.

## Backend

| Tecnología | Versión | Uso | Licencia |
|---|---|---|---|
| .NET / ASP.NET Core | 10 | Runtime, Minimal APIs, autenticación JWT, rate limiting, output cache | MIT |
| EF Core | 10 | ORM, migraciones | MIT |
| Npgsql EF Core | 10 | PostgreSQL | PostgreSQL License |
| MySql.EntityFrameworkCore (Oracle) | 10 | MySQL (Pomelo no tiene versión estable para EF Core 10) | GPLv2 con excepción FOSS |
| Dapper | 2 | SQL de alto rendimiento y procedimientos | Apache 2.0 |
| Mediator (martinothamar) | 3 | CQRS con generador de código | MIT |
| FluentValidation | 12 | Validación | Apache 2.0 |
| Riok.Mapperly | 4 | Mapeos generados en compilación | Apache 2.0 |
| BCrypt.Net-Next | 4 | Hash de contraseñas | MIT |
| Microsoft.Extensions.Caching.Hybrid | 10 | Caché en memoria + distribuida | MIT |
| Serilog | 4 | Logs estructurados | Apache 2.0 |
| OpenTelemetry | 1.19 | Trazas, métricas, exportación OTLP | Apache 2.0 |
| Scalar | 2 | Referencia OpenAPI en desarrollo | MIT |

Nota sobre MySQL: el conector oficial de Oracle se distribuye con GPLv2 más la excepción FOSS de Oracle, que permite
usarlo desde aplicaciones con otras licencias. Si tu política lo prohíbe, usa PostgreSQL o SQL Server, o reemplaza el
proveedor por Pomelo cuando publique una versión para EF Core 10 (solo cambia `MySqlProvider`).

## Frontend

| Tecnología | Uso | Licencia |
|---|---|---|
| React 19 | UI | MIT |
| Vite 7 | Desarrollo y build | MIT |
| TypeScript 5 | Tipado | Apache 2.0 |
| React Router 7 | Rutas con carga diferida | MIT |
| TanStack Query 5 y Table 8 | Datos del servidor y tablas | MIT |
| React Hook Form + Zod 4 | Formularios y esquemas (única fuente de tipos) | MIT |
| Zustand | Estado de sesión y plataforma | MIT |
| Radix UI | Primitivas accesibles | MIT |
| Tailwind CSS 4 | Estilos con tokens | MIT |
| i18next | Traducciones | MIT |
| lucide-react, sonner | Iconos, notificaciones | ISC, MIT |

## Pruebas y calidad

| Tecnología | Uso | Licencia |
|---|---|---|
| xUnit v3, Shouldly, NSubstitute | Pruebas .NET | Apache 2.0, BSD, BSD |
| NetArchTest | Pruebas de arquitectura | MIT |
| Testcontainers | Bases reales en las pruebas | MIT |
| Vitest, Testing Library, MSW | Pruebas del frontend | MIT |
| ESLint, Prettier | Calidad y formato | MIT |

## Alternativas y cómo reemplazarlas

| Componente | Reemplazo | Dónde |
|---|---|---|
| Redis | Cualquier `IDistributedCache` | `AddInfrastructure` |
| Bus en proceso | RabbitMQ, Azure Service Bus | Implementar `IEventBus` |
| Correo SMTP | SendGrid, SES | Implementar `IEmailSender` |
| Secretos en configuración | Azure Key Vault, AWS Secrets Manager, Vault | Implementar `ISecretStore` |
| Motor de base de datos | Otro motor soportado por EF Core | Nueva clase `IDatabaseProvider` y proyecto de migraciones |
