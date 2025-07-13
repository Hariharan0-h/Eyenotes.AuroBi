using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyenotes.AuroBi.Domain.Models
{
    public class SqlConnectionCredentials
    {
        public string Host { get; set; }
        public int Port { get; set; } = 1433;
        public string Username { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
    }

    public class PostgresConnectionCredentials
    {
        public string Host { get; set; }
        public int Port { get; set; } = 5432;
        public string Username { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
    }

    public class ExcelUploadRequest
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; }
    }
}
