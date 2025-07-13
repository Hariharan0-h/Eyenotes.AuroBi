using Dapper;
using Eyenotes.AuroBi.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Application.Services
{
    public interface IMetaDataService
    {
        Task<IEnumerable<string>> GetAllTableNamesAsync();
    }

    public class MetaDataService : IMetaDataService
    {
        private readonly IMetaDataRepository _repository;
        private readonly ILogger<MetaDataService> _logger;

        public MetaDataService(IMetaDataRepository repository, ILogger<MetaDataService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> GetAllTableNamesAsync()
        {
            const string query = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'";

            try
            {
                var connection = _repository.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                var tableNames = await connection.QueryAsync<string>(query);
                _logger.LogInformation("Retrieved {Count} tables.", tableNames.Count());
                return tableNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch table names.");
                throw;
            }
        }
    }
}
