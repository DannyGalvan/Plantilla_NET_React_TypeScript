using Project.Application.Querying;

namespace Project.Application.UnitTests.Querying;

public sealed class FilterParserTests
{
    private readonly FilterParser<TestEntity> _parser = new(new IFilterOperator[]
    {
        new EqualOperator(),
        new NotEqualOperator(),
        new GreaterThanOperator(),
        new GreaterThanOrEqualOperator(),
        new LessThanOperator(),
        new LessThanOrEqualOperator(),
        new ContainsOperator(),
        new StartsWithOperator(),
        new IsNullOperator(),
    });

    [Fact]
    public void Empty_filter_returns_null()
    {
        Assert.Null(_parser.Parse(null));
        Assert.Null(_parser.Parse(string.Empty));
    }

    [Fact]
    public void Single_condition_parses()
    {
        var lambda = _parser.Parse("Name:eq:foo");
        Assert.NotNull(lambda);
        Assert.Equal("x", lambda!.Parameters[0].Name);
    }

    [Fact]
    public void Multiple_conditions_combine_with_and()
    {
        var lambda = _parser.Parse("Name:eq:foo;Price:gt:10");
        Assert.NotNull(lambda);
        Assert.Contains("AndAlso", lambda!.Body.ToString());
    }

    [Fact]
    public void Unknown_field_throws()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse("NotAField:eq:x"));
    }

    [Fact]
    public void Unknown_operator_throws()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse("Name:nope:foo"));
    }

    [Fact]
    public void Malformed_segment_throws()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse("just-one-segment"));
        Assert.Throws<ArgumentException>(() => _parser.Parse("name:eq"));
    }

    [Fact]
    public void Guid_value_is_parsed()
    {
        var lambda = _parser.Parse("Id:eq:5d5e6f7a-8b9c-0d1e-2f3a-4b5c6d7e8f9a");
        Assert.NotNull(lambda);
        // The expression body must reference the parsed Guid constant.
        Assert.Contains("5d5e6f7a-8b9c-0d1e-2f3a-4b5c6d7e8f9a", lambda!.Body.ToString());
    }

    [Fact]
    public void Bool_value_is_parsed_for_isnull()
    {
        var lambda = _parser.Parse("Description:isnull:true");
        Assert.NotNull(lambda);
    }

    [Fact]
    public void Nested_property_is_resolved()
    {
        var lambda = _parser.Parse("Category.Name:eq:electronics");
        Assert.NotNull(lambda);
    }

    private sealed class TestEntity
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public TestEntity? Category { get; set; }
    }
}