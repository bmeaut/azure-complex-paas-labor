using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyNewHome.Bll.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBllSerivices(this IServiceCollection services)
        {
            services.AddScoped<PetService>();

            return services;
        }
    }
}
