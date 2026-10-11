namespace Project.Modules.Catalog.Application;

public sealed record ProductDto(Guid Id, string Sku, string Name, string? Description, decimal Price, int Stock, Guid CategoryId);
public sealed record CategoryDto(Guid Id, string Name, string? Description);

public sealed record CreateProductDto(string Sku, string Name, string? Description, decimal Price, int Stock, Guid CategoryId);
public sealed record UpdateProductDto(string? Name, string? Description, decimal? Price, int? Stock, Guid? CategoryId, Guid? ConcurrencyStamp);

public sealed record CreateCategoryDto(string Name, string? Description);
public sealed record UpdateCategoryDto(string? Name, string? Description, Guid? ConcurrencyStamp);