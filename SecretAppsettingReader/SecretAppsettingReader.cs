using Microsoft.Extensions.Configuration;

using System.Reflection;

namespace SecretAppsettingReader;

public class SecretAppsettingReader
{
    public T? ReadSection<T>(string sectionName, bool appSettingIsOptional, Assembly assembly)
    {
        var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: appSettingIsOptional)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets(assembly)
            .AddEnvironmentVariables();
        var configurationRoot = builder.Build();

        return configurationRoot.GetSection(sectionName).Get<T>();
    }
}