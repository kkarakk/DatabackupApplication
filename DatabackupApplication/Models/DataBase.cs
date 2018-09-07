using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;


namespace DatabackupApplication.Models
{
    public class DataBase:DbContext
    {
        public DataBase()
        {

        }

        public DataBase(DbContextOptions<DataBase> options):base(options)
        {
                
        }

        public DataTable dataBaseTable;//for database controller
        public IEnumerable<string> SelectedTables { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=Blogging;User Id=sa;Password=123456;Trusted_Connection=True;");

                //optionsBuilder.UseSqlServer(Configuration.GetConnectionString("BloggingDatabase"));
                //Configuration.GetConnectionString("BloggingDatabase")
            }
        }
    }
}
