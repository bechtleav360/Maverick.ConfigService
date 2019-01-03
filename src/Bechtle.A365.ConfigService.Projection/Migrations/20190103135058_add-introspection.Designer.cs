﻿// <auto-generated />
using System;
using Bechtle.A365.ConfigService.Projection.DataStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bechtle.A365.ConfigService.Projection.Migrations
{
    [DbContext(typeof(ProjectionStore))]
    [Migration("20190103135058_add-introspection")]
    partial class addintrospection
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ConfigEnvironment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Category");

                    b.Property<bool>("DefaultEnvironment");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("ConfigEnvironments");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ConfigEnvironmentKey", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("ConfigEnvironmentId");

                    b.Property<string>("Key");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("ConfigEnvironmentId");

                    b.ToTable("ConfigEnvironmentKey");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ProjectedConfiguration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("ConfigEnvironmentId");

                    b.Property<string>("ConfigurationJson");

                    b.Property<Guid>("StructureId");

                    b.Property<int>("StructureVersion");

                    b.Property<DateTime?>("ValidFrom");

                    b.Property<DateTime?>("ValidTo");

                    b.HasKey("Id");

                    b.HasIndex("ConfigEnvironmentId");

                    b.HasIndex("StructureId");

                    b.ToTable("ProjectedConfigurations");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ProjectedConfigurationKey", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Key");

                    b.Property<Guid>("ProjectedConfigurationId");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("ProjectedConfigurationId");

                    b.ToTable("ProjectedConfigurationKey");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.Structure", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<int>("Version");

                    b.HasKey("Id");

                    b.ToTable("Structures");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.StructureKey", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Key");

                    b.Property<Guid>("StructureId");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("StructureId");

                    b.ToTable("StructureKey");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.StructureVariable", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Key");

                    b.Property<Guid>("StructureId");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("StructureId");

                    b.ToTable("StructureVariable");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.UsedConfigurationKey", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Key");

                    b.Property<Guid>("ProjectedConfigurationId");

                    b.HasKey("Id");

                    b.HasIndex("ProjectedConfigurationId");

                    b.ToTable("UsedConfigurationKey");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Projection.DataStorage.ProjectionMetadata", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("LastActiveConfigurationId");

                    b.Property<long?>("LatestEvent");

                    b.HasKey("Id");

                    b.ToTable("Metadata");
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ConfigEnvironmentKey", b =>
                {
                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.ConfigEnvironment", "ConfigEnvironment")
                        .WithMany("Keys")
                        .HasForeignKey("ConfigEnvironmentId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ProjectedConfiguration", b =>
                {
                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.ConfigEnvironment", "ConfigEnvironment")
                        .WithMany()
                        .HasForeignKey("ConfigEnvironmentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.Structure", "Structure")
                        .WithMany()
                        .HasForeignKey("StructureId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.ProjectedConfigurationKey", b =>
                {
                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.ProjectedConfiguration", "ProjectedConfiguration")
                        .WithMany("Keys")
                        .HasForeignKey("ProjectedConfigurationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.StructureKey", b =>
                {
                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.Structure", "Structure")
                        .WithMany("Keys")
                        .HasForeignKey("StructureId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.StructureVariable", b =>
                {
                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.Structure", "Structure")
                        .WithMany("Variables")
                        .HasForeignKey("StructureId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Bechtle.A365.ConfigService.Common.DbObjects.UsedConfigurationKey", b =>
                {
                    b.HasOne("Bechtle.A365.ConfigService.Common.DbObjects.ProjectedConfiguration", "ProjectedConfiguration")
                        .WithMany("UsedConfigurationKeys")
                        .HasForeignKey("ProjectedConfigurationId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
