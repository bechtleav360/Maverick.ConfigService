﻿// <auto-generated />
using System;
using Bechtle.A365.ConfigService.Common.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bechtle.A365.ConfigService.Migrations.Migrations.Sqlite
{
    [DbContext(typeof(SqliteSnapshotContext))]
    [Migration("20200205090000_InitialMigration")]
    partial class InitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("Mav_Config")
                .HasAnnotation("ProductVersion", "3.1.1");

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbContexts.SqlSnapshot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("DataType")
                        .HasColumnType("TEXT");

                    b.Property<string>("Identifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("JsonData")
                        .HasColumnType("TEXT");

                    b.Property<long>("MetaVersion")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Version")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Snapshots");
                });
#pragma warning restore 612, 618
        }
    }
}
