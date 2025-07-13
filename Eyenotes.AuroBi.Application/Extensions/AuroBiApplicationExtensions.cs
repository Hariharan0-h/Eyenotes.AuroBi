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
            var connectionString = Environment.GetEnvironmentVariable("Eyenotes20_EmrConnection");
            #endregion

            #region DBContextInitialization
            // Register DbContext
            services.AddDbContext<EmrContext>(options =>
                options.UseSqlServer(connectionString));
            #endregion

            #region AddingRepositories
            services.AddScoped<IMetaDataRepository, MetaDataRepository>();
            #endregion

            #region AddingServices
            services.AddScoped<IMetaDataService, MetaDataService>();
            #endregion

            return services;
        }
    }
}
