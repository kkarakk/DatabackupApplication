using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DatabackupApplication.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DatabackupApplication.Controllers
{
    public class DataBaseController : Controller
    {

        public static string GenerateRandomDigitCode(int length)
        {
            var random = new Random();
            var str = string.Empty;
            for (var i = 0; i < length; i++)
                str = string.Concat(str, random.Next(10).ToString());
            return str;
        }
        // GET: /<controller>/
        private static string databaseName;
        private string appDirectory;
        public IHostingEnvironment _hostingEnvironment;
        public IConfiguration Configuration;
        public DataBaseController(IHostingEnvironment hostingEnvironment,IConfiguration configuration)
        {
            if (hostingEnvironment == null)
                throw new ArgumentNullException(nameof(hostingEnvironment));
            else
            {
                _hostingEnvironment = hostingEnvironment;
            }

            Configuration = configuration;

        }
        

        public IActionResult Index()
        {
            var table = new System.Data.DataTable();
            using (var dbContext = new BloggingContext())
            using (var dbCommand = dbContext.Database.GetDbConnection().CreateCommand())
            {
                dbCommand.CommandText = "select * from INFORMATION_SCHEMA.TABLES where table_type='BASE TABLE'";

                dbContext.Database.OpenConnection();
                using (var result = dbCommand.ExecuteReader())
                {
                    // do something with result
                    table.Load(result);                    
                }
                //return the database name
                appDirectory = _hostingEnvironment.ContentRootPath;
                databaseName = dbContext.Database.GetDbConnection().Database;
                var fileName = $"{appDirectory}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDigitCode(10)}.{Configuration["DbBackupFileExtension"]}";
                //var commandText = $"BACKUP DATABASE [{databaseName}] TO DISK = '{fileName}' WITH FORMAT";
                // sqlcommand
                
                //dbContext.Database.ExecuteSqlCommand()
            }
            
            //ContextBoundObject.
            return View(table);
        }
    }
}
