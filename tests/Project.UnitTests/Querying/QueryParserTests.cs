using System.Linq.Expressions;
using Project.Application.Querying;

namespace Project.UnitTests.Querying;

public sealed class QueryParserTests
{
    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string? Secret { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public Guid CategoryId { get; set; }
    }

    private sealed class ItemPolicy : QueryPolicy<Item>
    {
        public ItemPolicy()
        {
            Field(i => i.Name).Field(i => i.Price).Filterable(i => i.Stock).Filterable(i => i.DeletedAt).Filterable(i => i.CategoryId);
            DefaultOrder(i => i.Name);
        }
    }

    private static readonly QueryParser Parser = new(FilterOperators.Defaults());
    private static readonly ItemPolicy Policy = new();

    private static readonly Item[] Items =
    [
        new() { Name = "Laptop", Price = 1200, Stock = 3 },
        new() { Name = "Mouse", Price = 20, Stock = 50 },
        new() { Name = "Monitor", Price = 300, Stock = 0, DeletedAt = DateTimeOffset.UnixEpoch },
    ];

    private static string[] Apply(string filter)
    {
        var result = Parser.ParseFilter(filter, Policy);
        result.IsSuccess.ShouldBeTrue(result.Error?.Description);
        return Items.AsQueryable().Where(result.Value!).Select(i => i.Name).ToArray();
    }

    [Theory]
    [InlineData("price:gte:300", new[] { "Laptop", "Monitor" })]
    [InlineData("name:contains:o", new[] { "Laptop", "Mouse", "Monitor" })]
    [InlineData("name:startswith:Mo", new[] { "Mouse", "Monitor" })]
    [InlineData("stock:eq:0", new[] { "Monitor" })]
    [InlineData("price:between:10|500", new[] { "Mouse", "Monitor" })]
    [InlineData("name:in:Mouse|Laptop", new[] { "Laptop", "Mouse" })]
    [InlineData("deletedAt:isnull:true", new[] { "Laptop", "Mouse" })]
    [InlineData("price:lt:1000;stock:gt:10", new[] { "Mouse" })]
    [InlineData("Price:GTE:300", new[] { "Laptop", "Monitor" })]
    public void Filters_are_translated(string filter, string[] expected) => Apply(filter).ShouldBe(expected, ignoreOrder: true);

    [Theory]
    [InlineData("secret:eq:x", "cannot be filtered")]
    [InlineData("price:like:1", "not supported")]
    [InlineData("price", "Malformed")]
    [InlineData("price:gte:abc", "Invalid value")]
    [InlineData("categoryId:eq:not-a-guid", "Invalid value")]
    public void Invalid_filters_are_rejected_with_validation_errors(string filter, string message)
    {
        var result = Parser.ParseFilter(filter, Policy);
        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(Project.SharedKernel.ErrorType.Validation);
        result.Error.Details!["filter"][0].ShouldContain(message);
    }

    [Fact]
    public void Values_never_become_constants_so_they_are_sent_as_sql_parameters()
    {
        var result = Parser.ParseFilter("name:eq:x' OR 1=1 --", Policy);

        var constants = new ConstantCollector();
        constants.Visit(result.Value!);
        constants.Values.ShouldNotContain(v => v is string);
        Items.AsQueryable().Where(result.Value!).ShouldBeEmpty();
    }

    [Fact]
    public void Too_many_conditions_are_rejected()
    {
        var filter = string.Join(';', Enumerable.Repeat("stock:gt:1", QueryParser.MaxConditions + 1));
        Parser.ParseFilter(filter, Policy).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Sort_uses_the_allowlist_and_the_default_order()
    {
        QueryParser.ParseSort<Item>(null, Policy).Value.Count.ShouldBe(1);
        var sort = QueryParser.ParseSort("-price,name", Policy);
        sort.Value.Select(s => s.Descending).ShouldBe([true, false]);
        QueryParser.ParseSort("stock", Policy).IsFailure.ShouldBeTrue();
    }

    private sealed class ConstantCollector : ExpressionVisitor
    {
        public List<object?> Values { get; } = [];

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Values.Add(node.Value);
            return base.VisitConstant(node);
        }
    }
}
