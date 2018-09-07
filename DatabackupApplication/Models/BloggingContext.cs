using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace DatabackupApplication.Models
{
    public partial class BloggingContext : DbContext
    {
        public BloggingContext()
        {
            
        }

        public BloggingContext(DbContextOptions<BloggingContext> options)
            : base(options)
        {
        }
        //public BloggingContext(IConfiguration configuration)
        //{
        //    Configuration = configuration;
        //}
        public virtual DbSet<Blog> Blog { get; set; }
        public virtual DbSet<Post> Post { get; set; }
        public  DataTable dataBaseTable;//for database controller
        public IEnumerable<string> SelectedTables { get; set; }
        //public IConfiguration Configuration { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
            if (!optionsBuilder.IsConfigured)
            {
                //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                             optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=Blogging;User Id=sa;Password=123456;Trusted_Connection=True;");

                //optionsBuilder.UseSqlServer(Configuration.GetConnectionString("BloggingDatabase"));
                //Configuration.GetConnectionString("BloggingDatabase")
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog>(entity =>
            {
                entity.Property(e => e.Url).IsRequired();
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasOne(d => d.Blog)
                    .WithMany(p => p.Post)
                    .HasForeignKey(d => d.BlogId);
            });
        }
    }
}
