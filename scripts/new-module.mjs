#!/usr/bin/env node
// Scaffolds a full-stack module: four backend projects (Domain, Application, Infrastructure, Contracts) with a sample
// CRUD entity, registration in AppModules, migration factories for the three engines and the frontend module.
//
// Usage:   node scripts/new-module.mjs <Module> <Entity> [--org-scoped]
// Example: node scripts/new-module.mjs Inventory Warehouse
// Then:    ./scripts/add-migration.sh Inventory Initial     (Windows: ./scripts/add-migration.ps1 -Module Inventory -Name Initial)
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';

const [Module, Entity, ...flags] = process.argv.slice(2);
if (!Module || !Entity || !/^[A-Z][A-Za-z0-9]+$/.test(Module) || !/^[A-Z][A-Za-z0-9]+$/.test(Entity)) {
  console.error('Usage: node scripts/new-module.mjs <Module> <Entity> [--org-scoped]   (PascalCase, for example: Inventory Warehouse)');
  process.exit(1);
}

const orgScoped = flags.includes('--org-scoped');
const root = resolve(new URL('..', import.meta.url).pathname);
const module = Module.toLowerCase();
const entity = Entity.charAt(0).toLowerCase() + Entity.slice(1);
const plural = `${Entity}s`;
const resource = `${entity.replace(/[A-Z]/g, (c) => `-${c.toLowerCase()}`)}s`;
const base = join(root, 'src', 'Modules', Module);

if (existsSync(base)) {
  console.error(`The module already exists: ${base}`);
  process.exit(1);
}

const write = (relative, content) => {
  const path = join(base, relative);
  mkdirSync(join(path, '..'), { recursive: true });
  writeFileSync(path, content);
};

const layer = (name) => `Project.Modules.${Module}.${name}`;
const csproj = (description, items) => `<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <Description>${description}</Description>
  </PropertyGroup>

${items}
</Project>
`;

// ----- Contracts -----
write(`${layer('Contracts')}/${layer('Contracts')}.csproj`, csproj(`Public API of the ${Module} module.`, `  <ItemGroup>
    <ProjectReference Include="..\\..\\..\\BuildingBlocks\\Project.SharedKernel\\Project.SharedKernel.csproj" />
  </ItemGroup>
`));
write(`${layer('Contracts')}/${Module}Permissions.cs`, `namespace ${layer('Contracts')};

/// <summary>Permission keys of the ${Module} module.</summary>
public static class ${Module}Permissions
{
    public const string Module = "${module}";

    public static class ${plural}
    {
        public const string Prefix = "${module}.${resource}";
        public const string Read = Prefix + ".read";
    }

    public static readonly string[] CrudActions = ["read", "create", "update", "delete", "restore"];
}
`);

// ----- Domain -----
write(`${layer('Domain')}/${layer('Domain')}.csproj`, csproj(`Domain of the ${Module} module.`, `  <ItemGroup>
    <ProjectReference Include="..\\..\\..\\BuildingBlocks\\Project.SharedKernel\\Project.SharedKernel.csproj" />
  </ItemGroup>
`));
write(`${layer('Domain')}/${Entity}.cs`, `using Project.SharedKernel.Domain;

namespace ${layer('Domain')};

public sealed class ${Entity} : AuditableAggregateRoot, IMultiTenant${orgScoped ? ', IOrganizationScoped' : ''}
{
    public Guid TenantId { get; set; }
${orgScoped ? '\n    public Guid OrganizationUnitId { get; set; }\n' : ''}
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
`);

// ----- Application -----
write(`${layer('Application')}/${layer('Application')}.csproj`, csproj(`Use cases of the ${Module} module.`, `  <ItemGroup>
    <PackageReference Include="Riok.Mapperly" PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\..\\..\\BuildingBlocks\\Project.Application\\Project.Application.csproj" />
    <ProjectReference Include="..\\${layer('Domain')}\\${layer('Domain')}.csproj" />
    <ProjectReference Include="..\\${layer('Contracts')}\\${layer('Contracts')}.csproj" />
  </ItemGroup>
`));
write(`${layer('Application')}/${plural}.cs`, `using FluentValidation;
using Project.Application.Crud;
using Project.Application.Querying;
using Project.Application.Security;
using ${layer('Contracts')};
using ${layer('Domain')};
using Riok.Mapperly.Abstractions;

namespace ${layer('Application')};

public sealed record ${Entity}Dto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, Guid ConcurrencyStamp);

public sealed record Create${Entity}Request(string Name, string? Description);

public sealed record Update${Entity}Request(string Name, string? Description, Guid? ConcurrencyStamp) : IConcurrencyAware;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public sealed partial class ${Entity}Mapper : ICrudMapper<${Entity}, ${Entity}Dto, Create${Entity}Request, Update${Entity}Request>
{
    public partial ${Entity}Dto ToDto(${Entity} entity);

    public partial IQueryable<${Entity}Dto> ProjectToDto(IQueryable<${Entity}> query);

    public partial ${Entity} ToEntity(Create${Entity}Request input);

    [MapperIgnoreTarget(nameof(${Entity}.ConcurrencyStamp))]
    public partial void Apply(Update${Entity}Request input, [MappingTarget] ${Entity} entity);

    public partial Update${Entity}Request ToUpdate(${Entity} entity);
}

public sealed class ${Entity}QueryPolicy : QueryPolicy<${Entity}>
{
    public ${Entity}QueryPolicy()
    {
        Field(e => e.Name).Field(e => e.CreatedAt);
        DefaultOrder(e => e.Name);
    }
}

public sealed class Create${Entity}Validator : AbstractValidator<Create${Entity}Request>
{
    public Create${Entity}Validator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(512);
    }
}

public sealed class Update${Entity}Validator : AbstractValidator<Update${Entity}Request>
{
    public Update${Entity}Validator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(512);
    }
}

public sealed class ${Module}PermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
        ${Module}Permissions.CrudActions.Select(action =>
            new PermissionDefinition($"{${Module}Permissions.${plural}.Prefix}.{action}", ${Module}Permissions.Module, $"permissions.${module}.${resource}.{action}", "${resource}"));

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("${module}.${resource}", ${Module}Permissions.Module, "${module}:${resource.replace(/-/g, '')}.title", "/${module}/${resource}", "layers", ${Module}Permissions.${plural}.Read, 500),
    ];
}
`);

// ----- Infrastructure -----
write(`${layer('Infrastructure')}/${layer('Infrastructure')}.csproj`, csproj(`Infrastructure and endpoints of the ${Module} module.`, `  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\..\\..\\BuildingBlocks\\Project.Infrastructure\\Project.Infrastructure.csproj" />
    <ProjectReference Include="..\\..\\..\\BuildingBlocks\\Project.Web\\Project.Web.csproj" />
    <ProjectReference Include="..\\${layer('Application')}\\${layer('Application')}.csproj" />
  </ItemGroup>
`));
write(`${layer('Infrastructure')}/${Module}Module.cs`, `using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using ${layer('Application')};
using ${layer('Contracts')};
using ${layer('Domain')};
using Project.Web.Crud;
using Project.Web.Modules;

namespace ${layer('Infrastructure')};

public sealed class ${Module}DbContext(DbContextOptions<${Module}DbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "${module}";

    public DbSet<${Entity}> ${plural} => Set<${Entity}>();
}

internal sealed class ${Entity}Configuration : IEntityTypeConfiguration<${Entity}>
{
    public void Configure(EntityTypeBuilder<${Entity}> builder)
    {
        builder.ToTable("${plural}");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(512);
        builder.Property(e => e.CreatedBy).HasMaxLength(64);
        builder.Property(e => e.UpdatedBy).HasMaxLength(64);
        builder.Property(e => e.DeletedBy).HasMaxLength(64);
        builder.HasIndex(e => new { e.TenantId, e.Name });
    }
}

public sealed class ${Module}Module : IModule
{
    public string Name => "${Module}";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<${Module}DbContext>(Name, DataScope);
        services.AddRepositoriesFor<${Module}DbContext>();
        services.AddModuleConventions(typeof(${Module}PermissionProvider).Assembly);
    }

    public void MapEndpoints(IEndpointRouteBuilder api) =>
        api.MapCrud<Guid, ${Entity}Dto, Create${Entity}Request, Update${Entity}Request>("${module}/${resource}", ${Module}Permissions.${plural}.Prefix);
}
`);

// ----- Registration: solution, composition, migrations -----
const edit = (relative, transform) => {
  const path = join(root, relative);
  writeFileSync(path, transform(readFileSync(path, 'utf8')));
};

const projects = ['Application', 'Contracts', 'Domain', 'Infrastructure']
  .map((name) => `    <Project Path="src/Modules/${Module}/${layer(name)}/${layer(name)}.csproj" />`)
  .join('\n');
edit('Project.slnx', (s) => s.replace('  <Folder Name="/src/Migrations/">', `  <Folder Name="/src/Modules/${Module}/">\n${projects}\n  </Folder>\n  <Folder Name="/src/Migrations/">`));

const infraReference = `..\\..\\Modules\\${Module}\\${layer('Infrastructure')}\\${layer('Infrastructure')}.csproj`;
edit('src/Host/Project.Composition/Project.Composition.csproj', (s) =>
  s.replace('  <ItemGroup>\n', `  <ItemGroup>\n    <ProjectReference Include="${infraReference}" />\n`),
);
edit('src/Host/Project.Composition/AppModules.cs', (s) =>
  s
    .replace('using Project.Web.Modules;', `using ${layer('Infrastructure')};\nusing Project.Web.Modules;`)
    .replace(/(\s+)\];\n\}/, `$1    new ${Module}Module(),$1];\n}`),
);

for (const provider of ['SqlServer', 'PostgreSql', 'MySql']) {
  const project = `src/Migrations/Project.Migrations.${provider}`;
  edit(`${project}/Project.Migrations.${provider}.csproj`, (s) =>
    s.replace('  <ItemGroup>\n    <ProjectReference', `  <ItemGroup>\n    <ProjectReference Include="${infraReference}" />\n    <ProjectReference`),
  );
  edit(`${project}/DesignTimeFactories.cs`, (s) =>
    s.replace('using Project.Infrastructure.Persistence.Providers;', `using Project.Infrastructure.Persistence.Providers;\nusing ${layer('Infrastructure')};`) +
    `
public sealed class ${Module}DbContextFactory : IDesignTimeDbContextFactory<${Module}DbContext>
{
    public ${Module}DbContext CreateDbContext(string[] args) => DesignTime.Create<${Module}DbContext>(new ${provider}Provider(), "${Module}");
}
`,
  );
}

// ----- Frontend -----
execFileSync('node', [join(root, 'src/project.client/scripts/new-module.mjs'), module, resource], { stdio: 'inherit' });

console.log(`
OK: module ${Module} created.
Next steps:
  1. ./scripts/add-migration.sh ${Module} Initial      (PowerShell: ./scripts/add-migration.ps1 -Module ${Module} -Name Initial)
  2. dotnet build Project.slnx && dotnet test Project.slnx
  3. dotnet run --project src/Host/Project.Api  ->  /${module}/${resource}
`);
