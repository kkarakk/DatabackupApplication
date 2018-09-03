using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DatabackupApplication.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DatabackupApplication.Controllers
{
    public class DataBaseController : Controller
    {
        

        // GET: /<controller>/
        public IActionResult Index()
        {
            var table = new System.Data.DataTable();
            using (var context = new BloggingContext())
            using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "select * from INFORMATION_SCHEMA.TABLES where table_type='BASE TABLE'";
                
                context.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    // do something with result
                    table.Load(result);                    
                }
               
            }
            //ContextBoundObject.
            return View(table);
        }
    }
}
