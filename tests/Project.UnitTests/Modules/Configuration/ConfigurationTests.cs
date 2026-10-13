using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Project.Application.Configuration;
using Project.Infrastructure.Configuration;
using Project.Modules.Configuration.Application;
using Project.Modules.Files.Infrastructure.Storage;

namespace Project.UnitTests.Modules.Configuration;

public sealed class ConfigurationTests
{
    [Fact]
    public void Secrets_round_trip_only_with_the_same_master_key()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var encrypted = MasterKeyCipher.Encrypt(key, "smtp-password");

        encrypted.ShouldStartWith("enc:v1:");
        encrypted.ShouldNotContain("smtp-password");
        MasterKeyCipher.Decrypt(key, encrypted).ShouldBe("smtp-password");
        MasterKeyCipher.Encrypt(key, "smtp-password").ShouldNotBe(encrypted);
        MasterKeyCipher.Decrypt(RandomNumberGenerator.GetBytes(32), encrypted).ShouldBeNull();
        MasterKeyCipher.Decrypt(key, encrypted[..^6] + "AAAAAA").ShouldBeNull();
        MasterKeyCipher.Decrypt(key, "plain text").ShouldBeNull();
    }

    [Fact]
    public void Master_key_accepts_base64_or_any_text()
    {
        var base64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        MasterKeyCipher.KeyFrom(Config(base64))!.Length.ShouldBe(32);
        MasterKeyCipher.KeyFrom(Config("a long passphrase that is not base64"))!.Length.ShouldBe(32);
        MasterKeyCipher.KeyFrom(Config(null)).ShouldBeNull();
    }

    [Fact]
    public void Sections_are_flattened_to_configuration_keys()
    {
        var catalog = new ConfigurationCatalog([new ConfigurableSection("Files", typeof(FilesOptions), "files", null)]);
        var fields = catalog.Fields(catalog.Sections[0]).ToDictionary(f => f.Key);

        fields["Files:OAuth:Google:ClientId"].Kind.ShouldBe(FieldKind.Text);
        fields["Files:OAuth:Google:ClientSecret"].Secret.ShouldBeTrue();
        fields["Files:OAuth:Google:ClientSecret"].Default.ShouldBeNull();
        fields["Files:Default:Credentials"].Kind.ShouldBe(FieldKind.Map);
        fields["Files:Default:Credentials"].Secret.ShouldBeTrue();
        fields["Files:Default:Settings"].Secret.ShouldBeFalse();
        fields["Files:MaxRequestSizeMb"].RequiresRestart.ShouldBeTrue();
        fields["Files:AllowCustomEndpoints"].Kind.ShouldBe(FieldKind.Boolean);
        fields.Keys.ShouldNotContain("Files:OAuth:Google:IsConfigured");
    }

    private static IConfiguration Config(string? masterKey) =>
        new ConfigurationBuilder().AddInMemoryCollection([KeyValuePair.Create("Configuration:MasterKey", masterKey)]).Build();
}
