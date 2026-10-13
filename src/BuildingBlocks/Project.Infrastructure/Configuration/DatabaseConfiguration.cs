using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Configuration;
using Project.Infrastructure.Persistence.Providers;

namespace Project.Infrastructure.Configuration;

/// <summary>
/// AES-256-GCM with the master key (<c>Configuration:MasterKey</c>). The master key is the only secret that must live
/// outside the database: everything sensitive stored in the database (configuration secrets and the Data Protection key
/// ring) is encrypted with it, so a leaked backup reveals nothing.
/// </summary>
public static class MasterKeyCipher
{
    public const string ConfigurationKey = "Configuration:MasterKey";
    private const string Prefix = "enc:v1:";

    /// <summary>32 bytes from a base64 key (<c>openssl rand -base64 32</c>); any other text is hashed with SHA-256.</summary>
    public static byte[]? KeyFrom(IConfiguration configuration)
    {
        var value = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length == 32)
            {
                return bytes;
            }
        }
        catch (FormatException)
        {
            // Not base64: derive the key from the text.
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(value));
    }

    public static bool IsEncrypted(string value) => value.StartsWith(Prefix, StringComparison.Ordinal);

    public static string Encrypt(byte[] key, string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var data = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[data.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, data, cipher, tag);
        return Prefix + Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    /// <summary>Null when the value was not encrypted with this key (wrong or rotated master key, tampering).</summary>
    public static string? Decrypt(byte[] key, string value)
    {
        if (!IsEncrypted(value))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(value[Prefix.Length..]);
            var nonce = bytes.AsSpan(0, 12);
            var tag = bytes.AsSpan(12, 16);
            var cipher = bytes.AsSpan(28);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            return null;
        }
    }
}

/// <summary><see cref="ISecretCipher"/> with the master key of the current configuration.</summary>
public sealed class MasterKeySecretCipher(IConfiguration configuration) : ISecretCipher
{
    public bool IsAvailable => MasterKeyCipher.KeyFrom(configuration) is not null;

    public string Encrypt(string plaintext) =>
        MasterKeyCipher.Encrypt(MasterKeyCipher.KeyFrom(configuration) ?? throw new InvalidOperationException(
            "Configure Configuration:MasterKey (environment variable Configuration__MasterKey) to store secrets in the database."), plaintext);
}

/// <summary>Physical names of the configuration table (owned by the Configuration module, read here without EF).</summary>
public static class ConfigurationTable
{
    public const string Schema = "configuration";
    public const string Table = "Entries";
}

/// <summary>Reads the configuration table of the platform database.</summary>
public sealed class DatabaseConfigurationSource(IDatabaseProvider provider, string connectionString, byte[]? masterKey, TimeSpan reloadInterval) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new DatabaseConfigurationProvider(provider, connectionString, masterKey, reloadInterval);
}

/// <summary>
/// Configuration entries stored in the platform database. Loaded at startup and polled every few seconds (a cheap
/// count and last-change query), so a change saved on any instance reaches every instance without a restart. Secrets
/// are decrypted with the master key. A missing database or table (first start, before migrations) is not an error.
/// </summary>
public sealed class DatabaseConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IDatabaseProvider _provider;
    private readonly string _connectionString;
    private readonly byte[]? _masterKey;
    private readonly Timer? _timer;
    private readonly TimeSpan _interval;
    private readonly Lock _gate = new();
    private string? _fingerprint;

    public DatabaseConfigurationProvider(IDatabaseProvider provider, string connectionString, byte[]? masterKey, TimeSpan reloadInterval)
    {
        _provider = provider;
        _connectionString = connectionString;
        _masterKey = masterKey;
        _interval = reloadInterval;
        if (reloadInterval > TimeSpan.Zero)
        {
            // One-shot timer re-armed after each poll: a slow database never piles callbacks up on the thread pool.
            _timer = new Timer(_ => Poll(), null, reloadInterval, Timeout.InfiniteTimeSpan);
        }
    }

    public override void Load() => Refresh(notify: false);

    /// <summary>Re-reads the table now and notifies the options monitors when something changed.</summary>
    public void Refresh(bool notify = true)
    {
        lock (_gate)
        {
            var (data, fingerprint) = Read();
            if (data is null || fingerprint == _fingerprint)
            {
                return;
            }

            Data = data;
            _fingerprint = fingerprint;
        }

        if (notify)
        {
            OnReload();
        }
    }

    public void Dispose() => _timer?.Dispose();

    private void Poll()
    {
        try
        {
            Refresh();
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            // The database is unreachable for a moment: keep the last values.
        }
        finally
        {
            try
            {
                _timer?.Change(_interval, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // The application is stopping.
            }
        }
    }

    private (Dictionary<string, string?>? Data, string? Fingerprint) Read()
    {
        try
        {
            using var connection = _provider.CreateConnection(_connectionString);
            connection.Open();
            var d = _provider.Dialect;
            var table = d.Table(ConfigurationTable.Schema, ConfigurationTable.Table);
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {d.QuoteIdentifier("Key")}, {d.QuoteIdentifier("Value")}, {d.QuoteIdentifier("IsSecret")}, {d.QuoteIdentifier("ConcurrencyStamp")} FROM {table}";
            using var reader = command.ExecuteReader();

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var stamps = new StringBuilder();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var value = reader.IsDBNull(1) ? null : reader.GetString(1);
                var secret = Convert.ToBoolean(reader.GetValue(2), System.Globalization.CultureInfo.InvariantCulture);
                stamps.Append(name).Append('=').Append(reader.GetValue(3)).Append(';');
                data[name] = secret && value is not null
                    ? _masterKey is null ? null : MasterKeyCipher.Decrypt(_masterKey, value)
                    : value;
            }

            return (data, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stamps.ToString()))));
        }
        catch (DbException)
        {
            // No database or no table yet.
            return (null, null);
        }
    }
}

public static class DatabaseConfigurationExtensions
{
    /// <summary>
    /// Adds the platform configuration stored in the database. Precedence: appsettings and user secrets, then the
    /// database, then environment variables and the command line (an environment variable is an emergency override).
    /// Needs only <c>Database:Provider</c> and the platform connection string.
    /// </summary>
    public static IConfigurationManager AddDatabaseConfiguration(this IConfigurationManager configuration)
    {
        if (configuration.Sources.Any(s => s is DatabaseConfigurationSource))
        {
            return configuration;
        }

        var connectionString = configuration["Database:ConnectionStrings:Platform"] is { Length: > 0 } platform
            ? platform
            : configuration["Database:ConnectionStrings:Default"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return configuration;
        }

        var providers = new DatabaseProviderRegistry([new SqlServerProvider(), new PostgreSqlProvider(), new MySqlProvider()]);
        var provider = providers.Get(configuration["Database:Provider"] ?? "PostgreSql");
        if (provider.Name == "SqlServer")
        {
            // SqlClient blocks a connection string for up to a minute after a failed open (the database does not exist
            // before the first migration); the configuration must be readable as soon as it does.
            connectionString = new DbConnectionStringBuilder { ConnectionString = connectionString, ["Pool Blocking Period"] = "NeverBlock" }.ConnectionString;
        }

        var seconds = int.TryParse(configuration["Configuration:ReloadSeconds"], out var value) ? value : 10;
        var source = new DatabaseConfigurationSource(provider, connectionString, MasterKeyCipher.KeyFrom(configuration), TimeSpan.FromSeconds(Math.Max(1, seconds)));

        // Right after the last file (appsettings, user secrets): above the files, below the environment variables of
        // the application and the command line. Host sources (ASPNETCORE_, DOTNET_) come earlier and are not affected.
        var lastFile = configuration.Sources.ToList().FindLastIndex(s => s is FileConfigurationSource);
        if (lastFile >= 0)
        {
            configuration.Sources.Insert(lastFile + 1, source);
        }
        else
        {
            configuration.Sources.Add(source);
        }

        return configuration;
    }
}

/// <summary>Reads the effective configuration and reports where each value comes from.</summary>
public sealed class ConfigurationInspector(IConfiguration configuration) : IConfigurationInspector
{
    private IConfigurationRoot? Root => configuration as IConfigurationRoot;

    public string? Get(string key) => configuration[key];

    public IReadOnlyList<string> Children(string key) => configuration.GetSection(key).GetChildren().Select(c => c.Key).ToList();

    public string? SourceOf(string key)
    {
        foreach (var provider in Root?.Providers.Reverse() ?? [])
        {
            if (provider.TryGet(key, out _) || provider.GetChildKeys([], key).Any())
            {
                return provider switch
                {
                    DatabaseConfigurationProvider => "database",
                    EnvironmentVariablesConfigurationProvider => "environment",
                    JsonConfigurationProvider => "file",
                    _ => "other",
                };
            }
        }

        return null;
    }

    public void Reload()
    {
        foreach (var provider in Root?.Providers.OfType<DatabaseConfigurationProvider>() ?? [])
        {
            provider.Refresh();
        }
    }
}

/// <summary>Encrypts the Data Protection key ring with the master key before it is stored in the database.</summary>
public sealed class MasterKeyXmlEncryptor(byte[] key) : IXmlEncryptor
{
    public EncryptedXmlInfo Encrypt(XElement plaintextElement) =>
        new(new XElement("encryptedKey", MasterKeyCipher.Encrypt(key, plaintextElement.ToString(SaveOptions.DisableFormatting))), typeof(MasterKeyXmlDecryptor));
}

/// <summary>Decrypts key ring elements written by <see cref="MasterKeyXmlEncryptor"/>.</summary>
public sealed class MasterKeyXmlDecryptor(IServiceProvider services) : IXmlDecryptor
{
    public XElement Decrypt(XElement encryptedElement)
    {
        var key = MasterKeyCipher.KeyFrom(services.GetRequiredService<IConfiguration>())
            ?? throw new CryptographicException("The Data Protection key ring is encrypted and Configuration:MasterKey is not configured.");
        var xml = MasterKeyCipher.Decrypt(key, encryptedElement.Value)
            ?? throw new CryptographicException("The Data Protection key ring cannot be decrypted with the current Configuration:MasterKey.");
        return XElement.Parse(xml);
    }
}
