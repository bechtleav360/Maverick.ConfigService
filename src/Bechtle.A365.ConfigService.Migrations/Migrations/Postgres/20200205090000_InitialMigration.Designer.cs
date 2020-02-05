﻿// <auto-generated />
using System;
using Bechtle.A365.ConfigService.Common.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Bechtle.A365.ConfigService.Migrations.Migrations.Postgres
{
    [DbContext(typeof(PostgresSnapshotContext))]
    [Migration("20200205090000_InitialMigration")]
    partial class InitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("ConfigService")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbContexts.SqlSnapshot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("DataType")
                        .HasColumnType("text");

                    b.Property<string>("Identifier")
                        .HasColumnType("text");

                    b.Property<string>("JsonData")
                        .HasColumnType("text");

                    b.Property<long>("MetaVersion")
                        .HasColumnType("bigint");

                    b.Property<long>("Version")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Snapshots");
                });
#pragma warning restore 612, 618
        }
    }
}
