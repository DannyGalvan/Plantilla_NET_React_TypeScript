using System.Net;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Modules.Webhooks.Infrastructure;

namespace Project.UnitTests.Modules.Webhooks;

public sealed class WebhookSecurityTests
{
    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("2001:4860:4860::8888", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.20.30.40", false)]
    [InlineData("172.20.0.1", false)]
    [InlineData("192.168.1.10", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    public void Only_public_addresses_are_reachable(string address, bool expected) =>
        SafeNetwork.IsPublic(IPAddress.Parse(address)).ShouldBe(expected);

    [Fact]
    public void Url_policy_requires_https_and_public_literal_hosts()
    {
        var policy = new WebhookUrlPolicy(new TestOptionsMonitor<WebhooksOptions>(new WebhooksOptions()));
        policy.Validate("https://hooks.example.com/in").ShouldBeNull();
        policy.Validate("http://hooks.example.com/in").ShouldNotBeNull();
        policy.Validate("https://localhost/in").ShouldNotBeNull();
        policy.Validate("https://[::1]/in").ShouldNotBeNull();
        policy.Validate("ftp://hooks.example.com").ShouldNotBeNull();

        var development = new WebhookUrlPolicy(new TestOptionsMonitor<WebhooksOptions>(new WebhooksOptions { AllowPrivateNetworks = true }));
        development.Validate("http://localhost:5005/in").ShouldBeNull();
    }

    [Fact]
    public void Deterministic_ids_are_stable_per_message_and_name()
    {
        var message = Guid.NewGuid();
        DeterministicId.From(message, "audit").ShouldBe(DeterministicId.From(message, "audit"));
        DeterministicId.From(message, "audit").ShouldNotBe(DeterministicId.From(message, "webhook"));
        DeterministicId.From(message, "audit").ShouldNotBe(DeterministicId.From(Guid.NewGuid(), "audit"));
        DeterministicId.From(message, "audit").Version.ShouldBe(8);
    }

    [Fact]
    public void Signatures_cover_timestamp_and_body()
    {
        var signature = WebhookSender.Sign("whsec_test", "1700000000", "{\"a\":1}");
        signature.ShouldBe(WebhookSender.Sign("whsec_test", "1700000000", "{\"a\":1}"));
        signature.ShouldNotBe(WebhookSender.Sign("whsec_test", "1700000001", "{\"a\":1}"));
        signature.ShouldNotBe(WebhookSender.Sign("whsec_test", "1700000000", "{\"a\":2}"));
        signature.Length.ShouldBe(64);
    }
}
