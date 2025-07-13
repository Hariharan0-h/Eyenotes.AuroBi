using Eyenotes.AuroBi.Domain.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Repositories
{
    public interface IMetaDataRepository
    {
        DbConnection GetDbConnection();
    }

    public class MetaDataRepository : IMetaDataRepository
    {
        private readonly EmrContext _context;

        public MetaDataRepository(EmrContext context)
        {
            _context = context;
        }

        public DbConnection GetDbConnection()
        {
            return _context.Database.GetDbConnection();
        }
    }
}
