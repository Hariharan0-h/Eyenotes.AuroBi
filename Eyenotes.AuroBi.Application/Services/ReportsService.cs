using Eyenotes.AuroBi.Domain.Models;
using Eyenotes.AuroBi.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Application.Services
{
    public interface IReportsService
    {
        Task<List<ReportsModel>> GetReportsConfig();
        Task<List<dynamic>> GetReportData(string query);
    }
    public class ReportsService : IReportsService
    {
        private readonly IReportsRepository _repository;
        public ReportsService(IReportsRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<ReportsModel>> GetReportsConfig()
        {
            var reports = await _repository.GetReportsConfig(Queries.GetReportConfigurations);
            return reports;
        }

        public async Task<List<dynamic>> GetReportData(string query)
        {
            var reportData = await _repository.GetReportData(query);
            return reportData;
        }
        
        public record Queries
        {
            public const string GetReportConfigurations = "select id as id, report_name as ReportName, report_query as ReportQuery from public.report_config";
        }
    }
}
