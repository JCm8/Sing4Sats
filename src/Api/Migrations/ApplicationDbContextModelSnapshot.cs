﻿// <auto-generated />
using System;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

            modelBuilder.Entity("Api.Models.LightningInvoice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<long>("Amount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Comment")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("LightningAddress")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalInvoice")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("OurAmount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OurId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("OurInvoice")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("PaidAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("LightningInvoices");
                });

            modelBuilder.Entity("Api.Models.SongRequestModel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Invoice")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsAlternativeVideo")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("PaidAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("PaymentHash")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("PlayedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("YoutubeLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("SongRequests");
                });

            modelBuilder.Entity("Api.Models.UserModel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Api.Models.SongRequestModel", b =>
                {
                    b.HasOne("Api.Models.UserModel", "User")
                        .WithMany("YoutubeRequests")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Api.Models.UserModel", b =>
                {
                    b.Navigation("YoutubeRequests");
                });
#pragma warning restore 612, 618
        }
    }
}
