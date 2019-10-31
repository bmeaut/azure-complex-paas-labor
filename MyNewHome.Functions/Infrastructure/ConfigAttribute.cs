using Microsoft.Azure.WebJobs.Description;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyNewHome.Functions
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    [Binding]
    public sealed class ConfigAttribute : Attribute
    {
    }
}
