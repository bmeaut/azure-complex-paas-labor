using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyNewHome.Bll.Extensions;
using MyNewHome.Functions;
using MyNewHome.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// http://marcelegger.net/azure-functions-v2-keyvault-and-iconfiguration

[assembly: WebJobsStartup(typeof(Startup))]
namespace MyNewHome.Functions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            // Get the current config and merge it into a new ConfigurationBuilder to keep the old settings
            var configurationBuilder = new ConfigurationBuilder();
            var descriptor = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
            if (descriptor?.ImplementationInstance is IConfigurationRoot configuration)
            {
                configurationBuilder.AddConfiguration(configuration);
            }

            // add the key vault to the configuration builder
            var config = configurationBuilder
                .AddAzureKeyVault()
                .Build();

            // replace the existing config with the new one
            builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), config));

            // add the ConfigProvider if you want to use IConfiguration in your function
            // the ConfigProvider is just an implementation of IExtensionConfigProvider to give you access to the current IConfiguration
            builder.AddExtension<ExtensionProvider<IConfiguration, ConfigAttribute>>();
            builder.AddExtension<ExtensionProvider<IServiceProvider, ServiceLocatorAttribute>>();

            builder.Services.AddBllSerivices();
            builder.Services.AddHttpClient();
        }
    }
}
