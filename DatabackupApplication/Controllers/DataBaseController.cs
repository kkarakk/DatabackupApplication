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
using System.Data.SqlClient;
using System.Data.Common;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DatabackupApplication.Controllers
{
    public class DataBaseController : Controller
    {
        //TODO:truncate database before BCP restore
        #region Fields
        private string databaseName;
        //private string appDirectory;
        public IHostingEnvironment _hostingEnvironment;
        public IConfiguration Configuration;

        private DataBase _DBcontext;
        #endregion

        # region ctor
        public DataBaseController(IHostingEnvironment hostingEnvironment, IConfiguration configuration,DataBase DBcontext)
        {
            if (hostingEnvironment == null||configuration == null|| DBcontext == null)
                throw new ArgumentNullException(nameof(hostingEnvironment));
            else
            {
                _hostingEnvironment = hostingEnvironment;
                Configuration = configuration;
             
                _DBcontext = DBcontext;
            }

        }

        #endregion

        #region Methods
        // GET: /<controller>/
        public async Task<IActionResult>  Index()
        {
            //DataTable table = GetTableRows();
            _DBcontext.dataBaseTable = await Task.Run(() => GetTableRows());
          //  DataTable table = await Task.Run(() => GetTableRows());

            //var backupFileNames = _maintenanceService.GetAllBackupFiles().ToList();
            String FullDatabaseBackupFileName = await Task.Run(() => GetPreExistingDatabaseBackupFileName("Full"));
            String DifferentialDatabaseBackupFileName = await Task.Run(() => GetPreExistingDatabaseBackupFileName("Differential"));
            if (IsNullOrEmpty(FullDatabaseBackupFileName))
            {
                ViewData["databaseBackupFileName"] = $"Does not exist";
                ViewData["timeOfBackup"] = "Hasn't been created yet";
                
                //TODO:ask if Delete relative backup if base backup doesn't exist

                String DifferentialDatabaseBackupFilePath = $"{Configuration["BackupDirectoryPath"]}{DifferentialDatabaseBackupFileName}";
                ViewData["HideDifferential"] = true;
                //System.IO.File.SetAttributes(DifferentialDatabaseBackupFilePath, FileAttributes.Normal);
                //System.IO.File.Delete(DifferentialDatabaseBackupFileName);

            }
            else
            {
                ViewData["HideDifferential"] = false;
                ViewData["databaseBackupFileName"] = FullDatabaseBackupFileName;
                ViewData["timeOfBackup"] = System.IO.File.GetLastWriteTime(FullDatabaseBackupFileName); //use static method to get accurate time
                if (IsNullOrEmpty(DifferentialDatabaseBackupFileName))
                {
                    ViewData["databaseDifferentialBackupFileName"] = $"Does not exist";
                    ViewData["timeOfDifferentialBackup"] = "Hasn't been created yet";
                }
                else
                {
                    ViewData["databaseDifferentialBackupFileName"] = DifferentialDatabaseBackupFileName;
                    ViewData["timeOfDifferentialBackup"] = System.IO.File.GetLastWriteTime(DifferentialDatabaseBackupFileName); ;
                }
            }

            //var fileName = new DirectoryInfo(Configuration["BackupDirectoryPath"]).GetFiles().OrderByDescending(p=>p.LastWriteTime).FirstOrDefault();
            //var fileName = dirInfo.GetFiles().OrderByDescending(o => o.LastWriteTime).FirstOrDefault();
            // File.GetLastWriteTime()
            //ContextBoundObject.


            ViewData["databaseBackupFilePath"] = Configuration["BackupDirectoryPath"];

            //return View(_context.dataBaseTable);
            return View(_DBcontext);
        }

        // GET: /<controller>/
        public async Task<IActionResult> BCPBackupRestore()
        {
            //DataTable table = GetTableRows();
            _DBcontext.dataBaseTable = await Task.Run(() => GetTableRows());
            //  DataTable table = await Task.Run(() => GetTableRows());

            ViewData["databaseBackupFilePath"] = Configuration["BackupDirectoryPath"];

            //return View(_context.dataBaseTable);
            return View("BCPBackupRestore",_DBcontext);
        }
        // POST: /<controller>/
        [HttpPost]
        public async Task<IActionResult> TakeDataBaseBackup(string Backup)
        {
            //   Response.Write("[" + testinput + "]");

            await Task.Run(() =>DatabaseBackup(Configuration["FullDbBackupArguments"],"Full"));
            
            return RedirectToAction(nameof(Index));

        }

        // POST: /<controller>/
        [HttpPost]
        public async Task<IActionResult> TakeDataBaseBackupDifferential(string Backup)
        {
            if (IsNullOrEmpty(GetPreExistingDatabaseBackupFileName("Full")))
            {
                //TODO: what should be done if full backup doesn't exist'
                //if (!FileExists(filePath))
                //    return;

                //File.Delete(filePath);
            }
            else
            {
                await DatabaseBackup(Configuration["DifferentialDbBackupArguments"], "Differential");
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RestoreDataBaseFromBackup()
        {
            String FullDatabaseBackupFileName = await Task.Run(() => GetPreExistingDatabaseBackupFileName("Full"));
            
            var conn = new SqlConnectionStringBuilder(Configuration.GetConnectionString("MasterDatabase")).ToString();
            try
            {
                var sqlconn = new SqlConnection(conn);

                //this method (backups) works only with SQL Server database
                using (SqlConnection sqlConnectiononn = new SqlConnection(conn))
                {
                    var commandText = string.Format(
                        "DECLARE @ErrorMessage NVARCHAR(4000)\n" +
                        "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                        "BEGIN TRY\n" +
                            "RESTORE DATABASE [{0}] FROM DISK = '{1}' WITH REPLACE\n" +
                        "END TRY\n" +
                        "BEGIN CATCH\n" +
                            "SET @ErrorMessage = ERROR_MESSAGE()\n" +
                        "END CATCH\n" +
                        "ALTER DATABASE [{0}] SET MULTI_USER WITH ROLLBACK IMMEDIATE\n" +
                        "IF (@ErrorMessage is not NULL)\n" +
                        "BEGIN\n" +
                            "RAISERROR (@ErrorMessage, 16, 1)\n" +
                        "END",
                        _DBcontext.Database.GetDbConnection().Database,
                        FullDatabaseBackupFileName);

                    DbCommand dbCommand = new SqlCommand(commandText, sqlConnectiononn);
                    if (sqlConnectiononn.State != ConnectionState.Open)
                    {
                        sqlConnectiononn.Open();
                    }

                    dbCommand.ExecuteNonQuery();
                }

            }
            catch (Exception e)
            {
                e.ToString();
                throw;
            }
            //clear all pools
            SqlConnection.ClearAllPools();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RestoreDataBaseFromDifferentialBackup()
        {
            String FullDatabaseBackupFileName = await Task.Run(() => GetPreExistingDatabaseBackupFileName("Full"));
            String DifferentialDatabaseBackupFileName = await Task.Run(() => GetPreExistingDatabaseBackupFileName("Differential"));
            if (System.IO.File.GetLastWriteTime(FullDatabaseBackupFileName) < System.IO.File.GetLastWriteTime(DifferentialDatabaseBackupFileName))
            {
                var conn = new SqlConnectionStringBuilder(Configuration.GetConnectionString("MasterDatabase")).ToString();
                try
                {
                    var sqlconn = new SqlConnection(conn);
                    //TODO:fix backup query for differential,doesn't work
                    //this method (backups) works only with SQL Server database
                    //using (SqlConnection sqlConnectiononn = new SqlConnection(conn))
                    //{
                    //    var commandText = string.Format(
                    //        "DECLARE @ErrorMessage NVARCHAR(4000)\n" +
                    //        "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                    //        "BEGIN TRY\n" +
                    //            "RESTORE DATABASE [{0}] FROM DISK = '{1}' WITH REPLACE,NO RECOVERY\n" +
                    //            "RESTORE DATABASE [{0}] FROM DISK = '{2}' WITH RECOVERY\n" +
                    //        "END TRY\n" +
                    //        "BEGIN CATCH\n" +
                    //            "SET @ErrorMessage = ERROR_MESSAGE()\n" +
                    //        "END CATCH\n" +
                    //        "ALTER DATABASE [{0}] SET MULTI_USER WITH ROLLBACK IMMEDIATE\n" +
                    //        "IF (@ErrorMessage is not NULL)\n" +
                    //        "BEGIN\n" +
                    //            "RAISERROR (@ErrorMessage, 16, 1)\n" +
                    //        "END",
                    //        _DBcontext.Database.GetDbConnection().Database,
                    //        FullDatabaseBackupFileName,
                    //        DifferentialDatabaseBackupFileName);

                    //    DbCommand dbCommand = new SqlCommand(commandText, sqlConnectiononn);
                    //    if (sqlConnectiononn.State != ConnectionState.Open)
                    //        sqlConnectiononn.Open();
                    //    await Task.Run(() => dbCommand.ExecuteNonQuery());
                    //}

                }
                catch (Exception e)
                {
                    e.ToString();
                    throw;
                }
                //clear all pools
                SqlConnection.ClearAllPools();
            }
            return RedirectToAction(nameof(Index));
        }


        // POST: /<controller>/
        [HttpPost]
        public async Task<IActionResult> TakeDataBaseBackupBCP(DataBase model)
        {
            //TODO: error log bcp https://forums.asp.net/t/1714452.aspx?How+to+Log+result+of+bcp+command+to+a+txt+file+
            //string TableList = string.Join(", ", model.SelectedTables.ToArray());
            if (model.SelectedTables != null) {
                foreach (string Table in model.SelectedTables)
                {
                    await Task.Run(() => TakeTableBCPAsync(Table, "backup"));
                }
            }
            
            return RedirectToAction(nameof(BCPBackupRestore));
        }

        // POST: /<controller>/
        [HttpPost]
        public async Task<IActionResult> RestoreDataBaseBackupBCP(DataBase model)
        {

            //string TableList = string.Join(", ", model.SelectedTables.ToArray());
            if (model.SelectedTables!=null)
            {

                foreach (string Table in model.SelectedTables)
                {
                    await Task.Run(() => RestoreTableFromBCPAsync(Table));
                } 
            }
            return RedirectToAction(nameof(BCPBackupRestore));
        }

        private string GetPreExistingDatabaseBackupFileName(string TypeOfBackup)
        {
            
            string SearchPattern = $"*.{Configuration["DbBackupFileExtension"]}";
            var list = Directory.GetFiles(Configuration["BackupDirectoryPath"], SearchPattern, SearchOption.TopDirectoryOnly);
            string BackupStringContains = $"{TypeOfBackup}DatabaseBackup";
            var databaseBackupFileName = list.OrderByDescending(path => System.IO.File.GetLastWriteTime(path)).SingleOrDefault(str => str.Contains(BackupStringContains));

            return databaseBackupFileName;
        }

        private static string GenerateRandomDigitCode(int length)
        {
            var random = new Random();
            var str = string.Empty;
            for (var i = 0; i < length; i++)
                str = string.Concat(str, random.Next(10).ToString());
            return str;
        }

        private object getFileWriteTime(string path)
        {
            return System.IO.File.GetLastWriteTime(path);
        }

        private async Task TakeTableBCPAsync(string TableName,string TypeOfBCPOperation)
        {
            using (var DbContext = new DataBase())
            using (var DbCommand = DbContext.Database.GetDbConnection().CreateCommand())
            {

                
                var DBdatabaseName = DbContext.Database.GetDbConnection().Database;
                //string sCommandText = @"exec xp_cmdshell 'bcp.exe ""select * FROM blogging.INFORMATION_SCHEMA.TABLES""  queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c'";
                
                   string SQLTableContext = $"{DBdatabaseName}..{TableName}";
                   string BackupPath = $"{Configuration["BackupDirectoryPath"]}{TableName}.{Configuration["DbBCPBackupFileExtension"]}";
                string BCPErrorFile = $"{Configuration["BackupDirectoryPath"]}{Configuration["BCPErrorFileName"]}";
                string BCPOutputFile = $"{Configuration["BackupDirectoryPath"]}{Configuration["BCPConsoleOutputFileName"]}";
                string SQLCommandText;

             
                  
                         SQLCommandText = $"exec xp_cmdshell 'bcp.exe {SQLTableContext} out {BackupPath} -c -T -S {Configuration["ServerName"]} -U {Configuration["UserId"]} -P {Configuration["Password"]}'";
             
                //string SQLCommandTextTest = @"exec xp_cmdshell 'bcp.exe ""select * FROM blogging.INFORMATION_SCHEMA.TABLES""  queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c'";
                DbCommand.CommandType = System.Data.CommandType.Text;
                   DbCommand.CommandText = SQLCommandText;
               
                if(DbContext.Database.GetDbConnection().State!=ConnectionState.Open)
                {
                   await DbContext.Database.GetDbConnection().OpenAsync();
                }
                //dbContext.Database.ExecuteSqlCommand(commandText, true);
                
                await Task.Run(() => DbCommand.ExecuteNonQuery());
                DbContext.Database.GetDbConnection().Close();

                //string CommandText = $"exec xp_cmdShell 'bcp.exe {0} .. in {1} -c -q -U {2} -P {3} -t '", DBdatabaseName, GetDatabaseBackupFileName(DbContext,"dat"),Configuration["UserId"],Configuration["Password"]); 

            }
        }

        private async Task RestoreTableFromBCPAsync(string TableName)
        {
            string BCPRestoreCommand;
            string DBdatabaseName;
            using (var DbContext = new DataBase())
            using (var DbCommand = DbContext.Database.GetDbConnection().CreateCommand())
            {


                 DBdatabaseName = DbContext.Database.GetDbConnection().Database;
                // string sCommandText = @"exec xp_cmdshell 'bcp.exe ""select * FROM blogging.INFORMATION_SCHEMA.TABLES""  queryout D:\authors.txt -S .\SQLExpress -U sa -P 123456 -c'";

                string SQLTableContext = $"{DBdatabaseName}..{TableName}";
                string BackupPath = $"{Configuration["BackupDirectoryPath"]}{TableName}.{Configuration["DbBCPBackupFileExtension"]}";
                string BCPErrorFile = $"{Configuration["BackupDirectoryPath"]}{Configuration["BCPErrorFileName"]}";
                string BCPOutputFile = $"{Configuration["BackupDirectoryPath"]}{Configuration["BCPConsoleOutputFileName"]}";

                 BCPRestoreCommand = $"exec xp_cmdshell 'bcp.exe {SQLTableContext} in {BackupPath} -c -T -S {Configuration["ServerName"]} -U {Configuration["UserId"]} -P {Configuration["Password"]} -e {BCPErrorFile} -o {BCPOutputFile}'";
            }
                string DropTableConstraints = $"DECLARE @DROP NVARCHAR(MAX),@TABLENAME NVARCHAR(MAX); " +
                        $"SET @DROP = N''; " +
                        $"SET @tableName ='{TableName}';  " +
                        $"SELECT @DROP = @DROP + N'ALTER TABLE ' + QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name) + ' DROP CONSTRAINT ' +QUOTENAME(fk.name) + ';'  " +
                        $"FROM sys.foreign_keys AS fk " +
                        $"INNER JOIN sys.tables AS ct ON fk.parent_object_id = ct.[object_id] INNER JOIN sys.schemas AS cs ON ct.[schema_id] = cs.[schema_id] " +
                        $"where fk.referenced_object_id =(select object_id from sys.tables where name = @tableName) " +
                        $"or fk.parent_object_id = (select object_id from sys.tables where name = @tableName); ";

                string CreateTableConstraints =
                          $"DECLARE @CREATE NVARCHAR(MAX); " +
                          $"DECLARE @tablename2 nvarchar(MAX) = '{TableName}'; " +
                          $"SET @CREATE = N''; " +
                          $"SELECT @CREATE = @CREATE + N'" +
                          $"ALTER TABLE '+ QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name)+ ' ADD CONSTRAINT ' + QUOTENAME(fk.name) + ' FOREIGN KEY (' + STUFF((SELECT ',' + QUOTENAME(c.name) " +
                          $"FROM sys.columns AS c " +
                          $"INNER JOIN sys.foreign_key_columns AS fkc ON fkc.parent_column_id = c.column_id " +
                          $"AND fkc.parent_object_id = c.[object_id] " +
                          $"WHERE fkc.constraint_object_id = fk.[object_id] " +
                          $"ORDER BY fkc.constraint_column_id FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') +') " +
                          $"REFERENCES ' + QUOTENAME(rs.name) + '.' + QUOTENAME(rt.name) + '(' + STUFF((SELECT ',' + QUOTENAME(c.name) " +
                          $"FROM sys.columns AS c " +
                          $"INNER JOIN sys.foreign_key_columns AS fkc ON fkc.referenced_column_id = c.column_id " +
                          $"AND fkc.referenced_object_id = c.[object_id] " +
                          $"WHERE fkc.constraint_object_id = fk.[object_id] " +
                          $"ORDER BY fkc.constraint_column_id FOR XML PATH(N''), TYPE).value(N'.[1]', N'nvarchar(max)'), 1, 1, N'') +');' " +
                          $"FROM sys.foreign_keys AS fk INNER JOIN sys.tables AS rt " +
                          $"ON fk.referenced_object_id = rt.[object_id] " +
                          $"INNER JOIN sys.schemas AS rs  " +
                          $"ON rt.[schema_id] = rs.[schema_id] " +
                          $"INNER JOIN sys.tables AS ct " +
                          $"ON fk.parent_object_id = ct.[object_id] " +
                          $"INNER JOIN sys.schemas AS cs ON ct.[schema_id] = cs.[schema_id] " +
                          $"WHERE rt.is_ms_shipped = 0 AND ct.is_ms_shipped = 0 " +
                          $"AND (fk.referenced_object_id = (select object_id from sys.tables where name = @tablename2) or " +
                          $"fk.parent_object_id = (select object_id from sys.tables  where name = @tablename2)); ";

                

                // 1.Create Drop & Recreate constraint sql queries
                // 2.Drop Constraints
                // 3.Execute BCP Restore
                // 4.Recreate Constraints
                string SQLCommandText = DropTableConstraints + $" " + CreateTableConstraints + $" "
                                      + $"exec (@DROP)" + $" "
                                      + BCPRestoreCommand + $" "
                                      + $"exec (@CREATE)";

                try
                {
                    var conn = new SqlConnectionStringBuilder(Configuration.GetConnectionString("MasterDatabase")).ToString();
                    using (SqlConnection sqlConnectiononn = new SqlConnection(conn))
                    {
                        var commandText = string.Format(
                            "DECLARE @ErrorMessage NVARCHAR(4000)\n" +
                            "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE\n" +
                            "BEGIN TRY\n" +
                                "{1} \n" +
                            "END TRY\n" +
                            "BEGIN CATCH\n" +
                                "SET @ErrorMessage = ERROR_MESSAGE()\n" +
                            "END CATCH\n" +
                            "ALTER DATABASE [{0}] SET MULTI_USER WITH ROLLBACK IMMEDIATE\n" +
                            "IF (@ErrorMessage is not NULL)\n" +
                            "BEGIN\n" +
                                "RAISERROR (@ErrorMessage, 16, 1)\n" +
                            "END",
                            DBdatabaseName,
                            SQLCommandText);

                        DbCommand dbCommand = new SqlCommand(commandText, sqlConnectiononn);
                        if (sqlConnectiononn.State != ConnectionState.Open)
                            sqlConnectiononn.Open();
                        await Task.Run(() => dbCommand.ExecuteNonQuery());
                    }
                }
                catch (Exception e)
                {
                    var seeException = e.ToString();

                }
                SqlConnection.ClearAllPools();
            
        }

        private async Task DatabaseBackup(string Arguments,string TypeOfBackup)
        {
            using (var dbContext = new DataBase())
            {

                string fileName = GetNewDatabaseBackupFileName(dbContext, Configuration["DbBackupFileExtension"],TypeOfBackup);
                ////WITH FORMAT appends full database backup to same file 
                //var commandText = $"BACKUP DATABASE [{databaseName}] TO DISK = '{fileName}' WITH FORMAT";
                string commandText = $"BACKUP DATABASE [{databaseName}] TO DISK = '{fileName}' WITH {Arguments}";
                await Task.Run(() => ExecuteDatabaseBackup(dbContext, commandText));

            }
        }
        
        private System.Data.DataTable GetTableRows()
        {
            var table = new System.Data.DataTable();
            using (var dbContext = new DataBase())
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

        private string GetNewDatabaseBackupFileName(DataBase dbContext,string BackupFileExtension,string TypeOfBackup)
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

        private static void ExecuteDatabaseBackup(DataBase dbContext, string commandText)
        {
             dbContext.Database.ExecuteSqlCommand(commandText, true);
        }

        #endregion
    }
}
