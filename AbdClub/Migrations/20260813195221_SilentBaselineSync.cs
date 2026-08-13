using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AbdClub.Migrations
{
    /// <inheritdoc />
    public partial class SilentBaselineSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🌟 LEAVE THIS COMPLETELY EMPTY! 
            // This prevents EF from trying to recreate your "Locations" or "Members" tables.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

    }

}
