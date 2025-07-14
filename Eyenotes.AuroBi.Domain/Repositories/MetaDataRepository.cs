using Eyenotes.AuroBi.Domain.Data;
using System.Data.Common;

namespace Eyenotes.AuroBi.Domain.Repositories
{
    public interface IMetaDataRepository
    {
        Task<DbConnection> GetDbConnectionAsync();
        string GetProvider();
        Task<bool> TestConnectionAsync();
    }

    public class MetaDataRepository : IMetaDataRepository
    {
        private readonly IConnectionManager _connectionManager;

        public MetaDataRepository(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task<DbConnection> GetDbConnectionAsync()
        {
            return await _connectionManager.GetConnectionAsync();
        }

        public string GetProvider()
        {
            return _connectionManager.GetProvider();
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await _connectionManager.TestConnectionAsync();
        }
    }
}