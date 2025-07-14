using Eyenotes.AuroBi.Application.Services;
using Eyenotes.AuroBi.Domain.Data;
using Eyenotes.AuroBi.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eyenotes.AuroBi.Application.Extensions
{
    public static class AuroBiApplicationExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            #region ConnectionStringsInitialization
            var EmrconnectionString = Environment.GetEnvironmentVariable("Eyenotes20_EmrConnection");
            var AuroBiconnectionString = Environment.GetEnvironmentVariable("Eyenotes20_AuroBiConnection");
            #endregion

            #region DBContextInitialization
            // Register DbContext with connection resiliency
            services.AddDbContext<EmrContext>(options =>
            {
                options.UseSqlServer(EmrconnectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                });
            });

            services.AddDbContext<AuroBiContext>(options =>
            {
                options.UseNpgsql(AuroBiconnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(60);
                });
            });
            #endregion

            #region Connection Management
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            #endregion

            #region AddingRepositories
            services.AddScoped<IMetaDataRepository, MetaDataRepository>();
            services.AddScoped<IDataSourceRepository, DataSourceRepository>();
            #endregion

            #region AddingServices
            services.AddScoped<IMetaDataService, MetaDataService>();
            services.AddScoped<IDataSourceService, DataSourceService>();
            #endregion

            return services;
        }
    }
}