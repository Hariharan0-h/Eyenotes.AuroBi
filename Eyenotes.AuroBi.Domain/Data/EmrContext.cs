using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Data
{
    public class EmrContext : DbContext
    {
        public EmrContext(DbContextOptions<EmrContext> options) : base(options)
        {
        }
    }
}
