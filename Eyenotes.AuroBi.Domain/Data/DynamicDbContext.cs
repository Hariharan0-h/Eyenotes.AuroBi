using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Data
{
    public interface IDynamicDbContext
    {
        void SetConnection(DbConnection connection);
        DbConnection GetConnection();
        string GetProvider();
    }
    public class DynamicDbContext : IDynamicDbContext
    {
        private DbConnection _connection;

        public void SetConnection(DbConnection connection)
        {
            _connection = connection;
        }

        public DbConnection GetConnection()
        {
            if (_connection == null)
                throw new InvalidOperationException("Database connection not set.");
            return _connection;
        }

        public string GetProvider()
        {
            return _connection?.GetType().Name switch
            {
                "SqlConnection" => "SqlServer",
                "NpgsqlConnection" => "PostgreSQL",
                _ => "Unknown"
            };
        }
    }
}
