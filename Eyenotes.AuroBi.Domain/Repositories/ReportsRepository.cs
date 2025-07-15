using Eyenotes.AuroBi.Domain.Data;
using Eyenotes.AuroBi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Repositories
{
    public interface IReportsRepository
    {
        Task<List<ReportsModel>> GetReportsConfig(string query);
        Task<List<dynamic>> GetReportData(string query);
    }
    public class ReportsRepository : IReportsRepository
    {
        private readonly AuroBiContext _context;
        public ReportsRepository(AuroBiContext context)
        {
            _context = context;
        }
        public async Task<List<ReportsModel>> GetReportsConfig(string query)
        {
            using var connection = _context.Database.GetDbConnection();

            var result = await connection.QueryAsync<ReportsModel>(query);

            return result.ToList();
        }
        public async Task<List<dynamic>> GetReportData(string query)
        {
            using var connection = _context.Database.GetDbConnection();
            var result = await connection.QueryAsync(query);
            return result.ToList();
        }
    }
}
