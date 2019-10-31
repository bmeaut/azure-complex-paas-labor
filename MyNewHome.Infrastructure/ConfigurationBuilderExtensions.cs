using Microsoft.Extensions.Configuration;
using System;

namespace MyNewHome.Infrastructure
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder builder)
        {
            // TODO use Azure Key Vault

            return builder;
        }
    }
}
