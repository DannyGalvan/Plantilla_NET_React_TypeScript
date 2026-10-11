using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence.Providers;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Unit of Work bound to a single <see cref="ModuleDbContext"/>.
/// </summary>
public sealed class EfUnitOfWork<TContext> : IUnitOfWork
    where TContext : ModuleDbContext
{
    private readonly TContext _context;

    public EfUnitOfWork(TContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);

    public async Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct)
    {
        var tx = await _context.Database.BeginTransactionAsync(isolationLevel, ct);
        return new EfTransaction(tx);
    }

    private sealed class EfTransaction : ITransaction
    {
        private readonly IDbContextTransaction _tx;

        public EfTransaction(IDbContextTransaction tx)
        {
            _tx = tx;
        }

        public Task CommitAsync(CancellationToken ct) => _tx.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct) => _tx.RollbackAsync(ct);
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}