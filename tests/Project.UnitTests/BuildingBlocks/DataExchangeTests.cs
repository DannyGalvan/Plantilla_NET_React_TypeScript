using System.Text;
using Project.Web.Crud;

namespace Project.UnitTests.BuildingBlocks;

public sealed class DataExchangeTests
{
    private sealed record Input(string Name, decimal Price, int Stock, bool IsActive, Guid? CategoryId);

    [Fact]
    public void Csv_reader_handles_quotes_new_lines_and_semicolons()
    {
        var csv = "Name;Notes\r\n\"Uno; dos\";\"line 1\nline 2\"\r\nTres;\"he said \"\"hi\"\"\"\r\n\r\n";
        var (headers, rows) = TabularReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), TabularFormat.Csv, 100);
        headers.ShouldBe(["Name", "Notes"]);
        rows.Count.ShouldBe(2);
        rows[0].ShouldBe(["Uno; dos", "line 1\nline 2"]);
        rows[1][1].ShouldBe("he said \"hi\"");
    }

    [Fact]
    public void Binder_converts_cells_and_reports_bad_values_per_row()
    {
        var id = Guid.NewGuid();
        var rows = ImportBinder.Bind<Input>(
            ["name", "PRICE", "Stock", "Is Active", "categoryId", "unknown"],
            [["Laptop", "1.234,5", "3", "sí", id.ToString(), "ignored"], ["Mouse", "12,5", "1.5", "maybe", "", null]]);

        rows[0].Errors.ShouldBeEmpty();
        rows[0].Value.ShouldBe(new Input("Laptop", 1234.5m, 3, true, id));
        rows[0].Number.ShouldBe(2);
        rows[1].Errors.Count.ShouldBe(2);
        rows[1].Value.ShouldBeNull();
    }

    [Theory]
    [InlineData("1234.5", 1234.5)]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("1234,5", 1234.5)]
    [InlineData("1.234,5", 1234.5)]
    [InlineData("-0,75", -0.75)]
    public void Numbers_accept_english_and_spanish_separators(string cell, double expected)
    {
        var row = ImportBinder.Bind<Input>(["name", "price"], [["x", cell]])[0];
        row.Errors.ShouldBeEmpty();
        row.Value!.Price.ShouldBe((decimal)expected);
    }
}
