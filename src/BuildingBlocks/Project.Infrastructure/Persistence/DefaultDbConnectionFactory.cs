using System.Data;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence.Providers;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Default ADO.NET connection factory keyed by name. Used by the executor when
/// EF is not in scope (background jobs, migration scripts).
/// </summary>
public sealed class DefaultDbConnectionFactory : IDbConnectionFactory
{
    private readonly IDatabaseProvider _provider;

    public DefaultDbConnectionFactory(IDatabaseProvider provider)
    {
        _provider = provider;
    }

    public IDbConnection Create(string connectionName)
    {
        var conn = _provider.CreateConnection(GetType().Name);
        return conn;
    }
}