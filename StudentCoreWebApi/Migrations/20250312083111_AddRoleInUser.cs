using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentCoreWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleInUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "role_name");

            migrationBuilder.AddColumn<Guid>(
                name: "role_Id",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role_Id",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "role_name",
                table: "Users",
                newName: "Password");
        }
    }
}
