using Project.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Phase 2: applies Platform migrations first, then Tenant migrations per
// active tenant (in DatabasePerTenant and Hybrid-Dedicated modes). It also
// runs IPermissionSynchronizer and the seeders, then exits.

var app = builder.Build();

app.MapGet("/", () => "Project.MigrationService. Phase 2: skeleton only; the apply-migrations command is wired in the next sub-phase.");

app.Run();

public partial class Program;