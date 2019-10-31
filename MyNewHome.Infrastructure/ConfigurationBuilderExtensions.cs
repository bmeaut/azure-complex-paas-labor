using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;

namespace MyNewHome.Infrastructure
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder builder)
        {
            var config = builder.Build();
            var keyVaultBaseUrl = config.GetValue<string>("KeyVault");

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(
                    azureServiceTokenProvider.KeyVaultTokenCallback));
            builder.AddAzureKeyVault(keyVaultBaseUrl, keyVaultClient, new DefaultKeyVaultSecretManager());

            return builder;
        }
    }
}
