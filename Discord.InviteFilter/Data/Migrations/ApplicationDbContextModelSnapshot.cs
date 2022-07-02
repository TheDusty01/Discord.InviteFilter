﻿// <auto-generated />
using System;
using Discord.InviteFilter.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Discord.InviteFilter.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Discord.InviteFilter.Models.GuildInviteSettings", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong?>("LogChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<int>("PunishAction")
                        .HasColumnType("int");

                    b.Property<long?>("TimeoutDurationInMinutes")
                        .HasColumnType("bigint");

                    b.HasKey("GuildId");

                    b.ToTable("GuildInviteSettings");
                });
#pragma warning restore 612, 618
        }
    }
}
