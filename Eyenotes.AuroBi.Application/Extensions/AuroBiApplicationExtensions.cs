using Eyenotes.AuroBi.Application.Services;
using Eyenotes.AuroBi.Domain.Data;
using Eyenotes.AuroBi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Application.Extensions
{
    public static class AuroBiApplicationExtensions
    {
        public static IServiceCollection AddApplicationServices
            (
                this IServiceCollection services, 
                IConfiguration configuration
            )
        {
            #region ConnectionStringsInitialization
            var EmrconnectionString = Environment.GetEnvironmentVariable("Eyenotes20_EmrConnection");
            var AuroBiconnectionString = Environment.GetEnvironmentVariable("Eyenotes20_AuroBiConnection");
            #endregion

            #region DBContextInitialization
            // Register DbContext
            services.AddDbContext<EmrContext>(options =>
                options.UseSqlServer(EmrconnectionString));

            services.AddDbContext<AuroBiContext>(options =>
                options.UseNpgsql(AuroBiconnectionString));
            #endregion

            #region Dynamic DB Context
            services.AddSingleton<IDynamicDbContext, DynamicDbContext>(); 
            #endregion

            #region AddingRepositories
            services.AddScoped<IMetaDataRepository, MetaDataRepository>();
            services.AddScoped<IDataSourceRepository, DataSourceRepository>();
            #endregion

            #region AddingServices
            services.AddScoped<IMetaDataService, MetaDataService>();
            #endregion

            return services;
        }
    }
}
