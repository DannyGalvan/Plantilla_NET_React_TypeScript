using System.Linq.Expressions;
using Project.Application.Crud;
using Project.Modules.Catalog.Domain;

namespace Project.Modules.Catalog.Application;

public sealed class ProductQueryPolicy : QueryPolicy<Product>
{
    protected override IReadOnlyCollection<string> DefineFilterables() =>
        new[] { "Sku", "Name", "Price", "Stock", "CategoryId" };

    protected override IReadOnlyCollection<string> DefineSortables() =>
        new[] { "Name", "Price", "Stock", "CreatedAt" };

    protected override IReadOnlyCollection<string> DefineIncludables() =>
        new[] { "Category" };

    public ProductQueryPolicy()
    {
        RegisterSort("Name", (Expression<Func<Product, object>>)(p => p.Name));
        RegisterSort("Price", (Expression<Func<Product, object>>)(p => p.Price));
        RegisterSort("Stock", (Expression<Func<Product, object>>)(p => p.Stock));
        RegisterSort("CreatedAt", (Expression<Func<Product, object>>)(p => p.Id));
    }
}

public sealed class CategoryQueryPolicy : QueryPolicy<Category>
{
    protected override IReadOnlyCollection<string> DefineFilterables() =>
        new[] { "Name" };

    protected override IReadOnlyCollection<string> DefineSortables() =>
        new[] { "Name" };

    protected override IReadOnlyCollection<string> DefineIncludables() => [];

    public CategoryQueryPolicy()
    {
        RegisterSort("Name", (Expression<Func<Category, object>>)(c => c.Name));
    }
}