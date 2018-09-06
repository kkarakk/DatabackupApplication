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
using System.IO;
using static System.String;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DatabackupApplication.Controllers
{
    public class DataBaseController : Controller
    {
        
        public DataBaseController(IHostingEnvironment hostingEnvironment, IConfiguration configuration,BloggingContext context)
        {
            if (hostingEnvironment == null)
                throw new ArgumentNullException(nameof(hostingEnvironment));
            else
            {
                _hostingEnvironment = hostingEnvironment;
            }

            Configuration = configuration;
            _context = context;

        }

        public static string GenerateRandomDigitCode(int length)
        {
            var random = new Random();
            var str = string.Empty;
            for (var i = 0; i < length; i++)
                str = string.Concat(str, random.Next(10).ToString());
            return str;
        }
        
        private  string databaseName;
        //private string appDirectory;
        public IHostingEnvironment _hostingEnvironment;
        public IConfiguration Configuration;
        private BloggingContext _context;
        

        // GET: /<controller>/
        public async Task<IActionResult>  Index()
        {
            //DataTable table = GetTableRows();
            DataTable table = await Task.Run(() => GetTableRows());

            //var backupFileNames = _maintenanceService.GetAllBackupFiles().ToList();
            String databaseBackupFileName = await Task.Run(() => GetPreExistingDatabaseBackupFileName());
            if (IsNullOrEmpty(databaseBackupFileName))
            {
                ViewData["databaseBackupFileName"] = $"Does not exist";
                ViewData["timeOfBackup"] = "Hasn't been created yet";
                //TODO: Delete relative backup if base backup doesn't exist
            }
            else
            {
                ViewData["databaseBackupFileName"] = databaseBackupFileName;
                ViewData["timeOfBackup"] = System.IO.File.GetLastWriteTime(databaseBackupFileName); //use static method to get accurate time
            }

            //var fileName = new DirectoryInfo(Configuration["BackupDirectoryPath"]).GetFiles().OrderByDescending(p=>p.LastWriteTime).FirstOrDefault();
            //var fileName = dirInfo.GetFiles().OrderByDescending(o => o.LastWriteTime).FirstOrDefault();
            // File.GetLastWriteTime()
            //ContextBoundObject.
    
            
            ViewData["databaseBackupFilePath"] = Configuration["BackupDirectoryPath"];
            
            return View(table);//TODO: change this to use VIEW model instead
        }

        private String GetPreExistingDatabaseBackupFileName()
        {
            var list = Directory.GetFiles(Configuration["BackupDirectoryPath"], $"*.{Configuration["DbBackupFileExtension"]}", SearchOption.TopDirectoryOnly);
            var databaseBackupFileName = list.OrderByDescending(path => System.IO.File.GetLastWriteTime(path)).FirstOrDefault();
            return databaseBackupFileName;
        }

        // POST: /<controller>/
        [HttpPost]
        public async Task<IActionResult> TakeDataBaseBackup()
        {
            //   Response.Write("[" + testinput + "]");

            await DatabaseBackup(Configuration["FullDbBackupArguments"],"Full");

            using (var DbContext = new BloggingContext())
            using (var DbCommand = DbContext.Database.GetDbConnection().CreateCommand())
            {

                DbContext.Database.GetDbConnection().Open();
                var DBdatabaseName = DbContext.Database.GetDbConnection().Database;
                //string sCommandText = "exec xp_cmdShell 'bcp.exe'" + databaseName + ".." + "" + " in " +
                //                              GetDatabaseBackupFileName(DbContext,"dat") + " - c -q -U " +  Configuration["UserID"] + " -P " + Configuration["Password"] + "-t  ";
                //string CommandText = ($"exec xp_cmdShell 'bcp.exe' {0} .. in {1} -c -q -U {2} -P {3} -t ", databaseName,GetDataBackupFileName(DbContext,"dat"),Configuration["UserId"],Configuration["Password"]); 
                // $"{Configuration["BackupDirectoryPath"]}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDig*/itCode(10)}.{BackupFileExtension}";
                //string sCommandText = "exec xp_cmdshell " + "'bcp.exe' " + "'select * FROM INFORMATION_SCHEMA.TABLES' " + "queryout " + @"D:\authors.txt " + @" -S .\SQLExpress -U sa -P 123456 -c";
                string sCommandText = @"exec xp_cmdshell 'bcp.exe ""select * FROM blogging.INFORMATION_SCHEMA.TABLES""  queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c'";
                //string CommandText = $"exec xp_cmdShell 'bcp.exe {0} .. in {1} -c -q -U {2} -P {3} -t '", DBdatabaseName, GetDatabaseBackupFileName(DbContext,"dat"),Configuration["UserId"],Configuration["Password"]); 
                DbCommand.CommandType = System.Data.CommandType.Text;
                DbCommand.CommandText = sCommandText;


                await Task.Run(() => DbCommand.ExecuteNonQuery());
            }


            return RedirectToAction(nameof(Index));

        }

        private async Task DatabaseBackup(string Arguments,string TypeOfBackup)
        {
            using (var dbContext = new BloggingContext())
            {

                string fileName = GetNewDatabaseBackupFileName(dbContext, Configuration["DbBackupFileExtension"],TypeOfBackup);
                ////WITH FORMAT appends full database backup to same file 
                //var commandText = $"BACKUP DATABASE [{databaseName}] TO DISK = '{fileName}' WITH FORMAT";
                string commandText = $"BACKUP DATABASE [{databaseName}] TO DISK = '{fileName}' WITH {Arguments}";
                await Task.Run(() => ExecuteDatabaseBackup(dbContext, commandText));

            }
        }

        private object getFileWriteTime(string path)
        {
            return System.IO.File.GetLastWriteTime(path);
        }

        

     

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

        private string GetNewDatabaseBackupFileName(BloggingContext dbContext,string BackupFileExtension,string TypeOfBackup)
        {
            ////get the appdirectory location to search for backup files
            //appDirectory = _hostingEnvironment.ContentRootPath;
            //return the database name
            databaseName = dbContext.Database.GetDbConnection().Database;
            //var fileName = $"{appDirectory}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDigitCode(10)}.{Configuration["DbBackupFileExtension"]}";
            ////backup to configured directory w/ random name(unique backups)
            //var fileName = $"{Configuration["BackupDirectoryPath"]}database_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{GenerateRandomDigitCode(10)}.{BackupFileExtension}";
            var fileName = $"{Configuration["BackupDirectoryPath"]}{databaseName}{TypeOfBackup}DatabaseBackup.{BackupFileExtension}";

            return fileName;
        }

        private static void ExecuteDatabaseBackup(BloggingContext dbContext, string commandText)
        {
             dbContext.Database.ExecuteSqlCommand(commandText, true);
        }
    }
}
