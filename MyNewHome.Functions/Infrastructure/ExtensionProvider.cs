using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyNewHome.Functions
{
    internal class ExtensionProvider<TDependency, TAttribute> : IExtensionConfigProvider
        where TAttribute : Attribute
    {
        private readonly TDependency _dependency;

        public ExtensionProvider(TDependency dependency)
        {
            _dependency = dependency;
        }

        public void Initialize(ExtensionConfigContext context) => context.AddBindingRule<TAttribute>().BindToInput(_ => _dependency);
    }
}
