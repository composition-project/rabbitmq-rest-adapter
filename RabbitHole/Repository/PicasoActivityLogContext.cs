using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;

namespace RabbitHole.Repository
{
    public class PicasoActivityLogContext : DbContext
    {
        private string databasename = string.Empty;

        public virtual DbSet<RabbitHoleLog> RabbitHoleLog { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING_ACTIVITYLOG");
            optionsBuilder.UseMySQL(@connectionString);
            databasename = GetDatabaseName(connectionString);
        }

        private string GetDatabaseName(string connectionString)
        {
            string databasename = string.Empty;
            Dictionary<string, string> cs_collection = new Dictionary<string, string>();
            string[] connectionStringSplit = connectionString.Split(';');
            if (connectionStringSplit.Length > 0)
            {
                for (int i = 0; i < connectionStringSplit.Length; i++)
                {
                    string[] strSplit = connectionStringSplit[i].Split('=');
                    if (strSplit.Length == 2)
                        cs_collection.Add(strSplit[0], strSplit[1]);
                }
            }

            cs_collection.TryGetValue("database", out databasename);
            return databasename;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RabbitHoleLog>(entity =>
            {
                entity.HasKey(e => e.id)
                    .HasName("PRIMARY");

                entity.ToTable("RabbitHoleLog", databasename);

                entity.HasIndex(e => e.id)
                    .HasName("PRIMARY")
                    .IsUnique();

                entity.Property(e => e.id)
                    .IsRequired()
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Method)
                    .IsRequired()
                    .HasColumnType("varchar")
                    .HasMaxLength(50);

                entity.Property(e => e.Request)
                   .IsRequired()
                   .HasColumnType("varchar")
                   .HasMaxLength(500);

                entity.Property(e => e.ContentType)
                   .HasColumnType("varchar")
                   .HasMaxLength(50);

                entity.Property(e => e.PicasoHeader)
                   .HasColumnType("varchar")
                   .HasMaxLength(300);

                entity.Property(e => e.ODS)
                   .HasColumnType("varchar")
                   .HasMaxLength(50);

                entity.Property(e => e.Timestamp)
                   .IsRequired()
                   .HasColumnType("datetime");

                entity.Property(e => e.Duration)
                   .HasColumnType("double");

                entity.Property(e => e.StatusCode)
                    .HasColumnType("int(11)");

                entity.Property(e => e.Method)
                   .HasColumnType("varchar")
                   .HasMaxLength(300);

                entity.Property(e => e.Exception)
                   .HasColumnType("varchar");
            });
        }
    }
}
