using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Import and export of every generic CRUD resource, without code in the module.</summary>
public sealed class DataExchangeTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Imports_are_validated_per_row_and_saved_all_or_nothing()
    {
        var admin = await api.SignInAsync();
        var csv = "name,description\r\nImported A,first\r\n\"Imported, B\",\"with \"\"quotes\"\"\"\r\n";

        var dryRun = await ImportAsync(admin, "/api/v1/catalog/categories/import?dryRun=true", "categories.csv", csv);
        dryRun.StatusCode.ShouldBe(HttpStatusCode.OK, await dryRun.Content.ReadAsStringAsync(Ct));
        (await dryRun.ReadJsonAsync())["created"]!.GetValue<int>().ShouldBe(0);
        (await (await admin.GetAsync("/api/v1/catalog/categories?filter=name:startswith:Imported", Ct)).ReadJsonAsync())["items"]!.AsArray().ShouldBeEmpty();

        var invalid = await ImportAsync(admin, "/api/v1/catalog/categories/import", "categories.csv", csv + ",missing name\r\n");
        invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var report = await invalid.ReadJsonAsync();
        report["errors"]![0]!["row"]!.GetValue<int>().ShouldBe(4);
        (await (await admin.GetAsync("/api/v1/catalog/categories?filter=name:startswith:Imported", Ct)).ReadJsonAsync())["items"]!.AsArray().ShouldBeEmpty();

        var imported = await ImportAsync(admin, "/api/v1/catalog/categories/import", "categories.csv", csv);
        imported.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await imported.ReadJsonAsync())["created"]!.GetValue<int>().ShouldBe(2);
        var names = (await (await admin.GetAsync("/api/v1/catalog/categories?filter=name:startswith:Imported&sort=name", Ct)).ReadJsonAsync())["items"]!
            .AsArray().Select(c => c!["name"]!.GetValue<string>());
        names.ShouldBe(["Imported A", "Imported, B"]);

        // Spanish Excel CSV: semicolons and decimal comma; values are converted to the property types.
        var category = (await (await admin.GetAsync("/api/v1/catalog/categories?filter=name:eq:Imported A", Ct)).ReadJsonAsync())["items"]![0]!.AsObject().Id();
        var products = $"sku;name;price;stock;categoryId\r\nIMP-1;Imported product;12,50;3;{category}\r\nIMP-2;Bad price;abc;1;{category}\r\n";
        var bad = await ImportAsync(admin, "/api/v1/catalog/products/import", "products.csv", products);
        bad.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await bad.ReadJsonAsync())["errors"]![0]!["errors"]![0]!.GetValue<string>().ShouldContain("price");
        var good = await ImportAsync(admin, "/api/v1/catalog/products/import", "products.csv", products.Replace("abc", "7", StringComparison.Ordinal));
        good.StatusCode.ShouldBe(HttpStatusCode.OK, await good.Content.ReadAsStringAsync(Ct));
        var product = (await (await admin.GetAsync("/api/v1/catalog/products?filter=sku:eq:IMP-1", Ct)).ReadJsonAsync())["items"]![0]!;
        product["price"]!.GetValue<decimal>().ShouldBe(12.50m);

        // Readers cannot import.
        var reader = await CreateReaderAsync(admin);
        (await ImportAsync(reader, "/api/v1/catalog/categories/import", "categories.csv", csv)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Exports_follow_the_list_filter_and_neutralize_formulas()
    {
        var admin = await api.SignInAsync();
        await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Export one", description = "=HYPERLINK(\"http://evil\")" }, Ct);
        await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Export two", description = "plain" }, Ct);

        var csv = await admin.GetAsync("/api/v1/catalog/categories/export?format=csv&filter=name:startswith:Export&sort=name", Ct);
        csv.StatusCode.ShouldBe(HttpStatusCode.OK);
        csv.Content.Headers.ContentDisposition!.FileName!.ShouldEndWith(".csv");
        var lines = (await csv.Content.ReadAsStringAsync(Ct)).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("id,name,description,createdAt");
        lines.Length.ShouldBe(3);
        lines[1].ShouldContain("'=HYPERLINK");

        var xlsx = await admin.GetAsync("/api/v1/catalog/categories/export?format=xlsx&filter=name:startswith:Export&sort=name", Ct);
        xlsx.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var workbook = new XLWorkbook(await xlsx.Content.ReadAsStreamAsync(Ct));
        var sheet = workbook.Worksheets.First();
        sheet.Cell(1, 2).GetString().ShouldBe("name");
        sheet.Cell(2, 2).GetString().ShouldBe("Export one");
        sheet.Cell(2, 3).GetString().ShouldBe("=HYPERLINK(\"http://evil\")");
        sheet.Cell(2, 3).HasFormula.ShouldBeFalse();

        (await admin.GetAsync("/api/v1/catalog/categories/export?format=pdf", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await admin.GetAsync("/api/v1/catalog/categories/export?filter=secret:eq:1", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Excel_files_round_trip()
    {
        var admin = await api.SignInAsync();
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Categories");
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell(1, 2).Value = "Description";
        sheet.Cell(2, 1).Value = "From Excel";
        sheet.Cell(2, 2).Value = 42;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var response = await ImportAsync(admin, "/api/v1/catalog/categories/import", "categories.xlsx", stream.ToArray());
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        var created = (await (await admin.GetAsync("/api/v1/catalog/categories?filter=name:eq:From Excel", Ct)).ReadJsonAsync())["items"]![0]!;
        created["description"]!.GetValue<string>().ShouldBe("42");
    }

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, string url, string fileName, string content) =>
        ImportAsync(client, url, fileName, Encoding.UTF8.GetBytes(content));

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, string url, string fileName, byte[] content)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        return client.PostAsync(url, form, Ct);
    }

    private async Task<HttpClient> CreateReaderAsync(HttpClient admin)
    {
        var roles = await (await admin.GetAsync("/api/v1/identity/roles?filter=name:eq:User", Ct)).ReadJsonAsync();
        (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "import.reader", email = "import.reader@example.com", password = "Secure.Pass-2026", roleIds = new[] { roles["items"]![0]!.AsObject().Id() }, mustChangePassword = false }, Ct))
            .EnsureSuccessStatusCode();
        return await api.SignInAsync("import.reader", "Secure.Pass-2026");
    }
}
