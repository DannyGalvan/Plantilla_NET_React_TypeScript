using Project.Application.Crud;
using Project.Modules.Catalog.Domain;
using Riok.Mapperly.Abstractions;

namespace Project.Modules.Catalog.Application;

[Mapper]
public partial class ProductMapper : IProjector<Product, ProductDto>, IMapperTo<CreateProductDto, Product>, IMapperTo<UpdateProductDto, Product>
{
    public partial ProductDto Project(Product source);

    public partial IQueryable<ProductDto> Project(IQueryable<Product> source);

    public partial Product Map(CreateProductDto source);

    public partial Product Map(UpdateProductDto source);
}

[Mapper]
public partial class CategoryMapper : IProjector<Category, CategoryDto>, IMapperTo<CreateCategoryDto, Category>, IMapperTo<UpdateCategoryDto, Category>
{
    public partial CategoryDto Project(Category source);

    public partial IQueryable<CategoryDto> Project(IQueryable<Category> source);

    public partial Category Map(CreateCategoryDto source);

    public partial Category Map(UpdateCategoryDto source);
}