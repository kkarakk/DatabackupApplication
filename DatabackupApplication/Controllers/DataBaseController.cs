using System;
using System.Data;
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

        // GET: /<controller>/
        public IActionResult  Index()
        {
            System.Data.DataTable table = GetTableRows();

            //ContextBoundObject.
            return View(table);
        }

        [HttpPost]
        public void DataBaseBackup()
        {
            //   Response.Write("[" + testinput + "]");


            // sqlcommand
            using (var dbContext = new BloggingContext())
            {
                string fileName = GetDatabaseBackupFileName(dbContext,Configuration["DbBackupFileExtension"]);

                var commandText = $"BACKUP DATABASE [{databaseName}] TO DISK = '{fileName}' WITH FORMAT";
                ExecuteDatabaseBackup(dbContext, commandText);
            }

            using (var DbContext = new BloggingContext())
            using (var DbCommand = DbContext.Database.GetDbConnection().CreateCommand())
            {

                databaseName = DbContext.Database.GetDbConnection().Database;
                //string sCommandText = "exec xp_cmdShell 'bcp.exe'" + databaseName + ".." + "" + " in " +
                //                              GetDatabaseBackupFileName(DbContext,"dat") + " - c -q -U " +  Configuration["UserID"] + " -P " + Configuration["Password"] + "-t  ";
                //string CommandText = ($"exec xp_cmdShell 'bcp.exe' {0} .. in {1} -c -q -U {2} -P {3} -t ", databaseName,GetDataBackupFileName(DbContext,"dat"),Configuration["UserId"],Configuration["Password"]); 
                // $"{Configuration["BackupDirectoryPath"]}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDig*/itCode(10)}.{BackupFileExtension}";
                //DbCommand.CommandType = System.Data.CommandType.Text;
                //DbCommand.CommandText = sCommandText;

              //  DbCommand.ExecuteNonQuery();
            }


            //BCPdatabaseBackup();

        }

        //private static void BCPdatabaseBackup()
        //{
        //    using (var DbContext = new BloggingContext())
        //    using (var DbCommand = DbContext.Database.GetDbConnection().CreateCommand())
        //    {

        //        databaseName = DbContext.Database.GetDbConnection().Database;
        //        //string sCommandText = "exec xp_cmdShell 'bcp.exe'" + databaseName + ".." + "" + " in " +
        //        //                              GetDatabaseBackupFileName(DbContext,"dat") + " - c -q -U " +  Configuration["UserID"] + " -P " + Configuration["Password"] + "-t  ";
        //        //string CommandText = ($"exec xp_cmdShell 'bcp.exe' {0} .. in {1} -c -q -U {2} -P {3} -t ", databaseName,GetDataBackupFileName(DbContext,"dat"),Configuration["UserId"],Configuration["Password"]); 
        //      // $"{Configuration["BackupDirectoryPath"]}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDig*/itCode(10)}.{BackupFileExtension}";
        //        DbCommand.CommandType = System.Data.CommandType.Text;
        //        DbCommand.CommandText = sCommandText;

        //        DbCommand.ExecuteNonQuery();
        //    }

        //}

        private System.Data.DataTable GetTableRows()
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
                
            }

            return table;
        }

        private string GetDatabaseBackupFileName(BloggingContext dbContext,string BackupFileExtension)
        {
            //return the database name
            appDirectory = _hostingEnvironment.ContentRootPath;
            databaseName = dbContext.Database.GetDbConnection().Database;
            //backup to wherever app is installed
            //var fileName = $"{appDirectory}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDigitCode(10)}.{Configuration["DbBackupFileExtension"]}";
            //backup to particular directory
            var fileName = $"{Configuration["BackupDirectoryPath"]}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDigitCode(10)}.{BackupFileExtension}";
            return fileName;
        }

        private static void ExecuteDatabaseBackup(BloggingContext dbContext, string commandText)
        {
            dbContext.Database.ExecuteSqlCommand(commandText, true);
        }
    }
}
