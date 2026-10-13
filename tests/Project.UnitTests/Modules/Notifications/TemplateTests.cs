using Project.Modules.Notifications.Application;

namespace Project.UnitTests.Modules.Notifications;

public sealed class TemplateTests
{
    [Fact]
    public void Placeholders_are_replaced_and_unknown_ones_removed()
    {
        var data = new Dictionary<string, string> { ["name"] = "Ana", ["order.number"] = "FAC-0007" };
        TemplateRenderer.Render("Hola {{ name }}, tu pedido {{order.number}} {{missing}}listo.", data)
            .ShouldBe("Hola Ana, tu pedido FAC-0007 listo.");
    }

    [Fact]
    public void Email_layout_encodes_every_value()
    {
        var html = EmailLayout.Render("<Brand>", "Title & more", "Line 1\n<img src=x onerror=alert(1)>");
        html.ShouldContain("&lt;Brand&gt;");
        html.ShouldContain("Title &amp; more");
        html.ShouldContain("&lt;img src=x onerror=alert(1)&gt;");
        html.ShouldNotContain("<img");
    }
}
