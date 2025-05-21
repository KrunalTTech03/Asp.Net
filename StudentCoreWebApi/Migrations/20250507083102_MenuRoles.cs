using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentCoreWebApi.Migrations
{
    /// <inheritdoc />
    public partial class MenuRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_MenuPermissions_Permissions_PermissionId",
            //    table: "MenuPermissions");

            migrationBuilder.AddForeignKey(
         name: "FK_MenuPermissions_CreateMenuPermissions_PermissionId",
         table: "MenuPermissions",
         column: "PermissionId",
         principalTable: "CreateMenuPermissions",
         principalColumn: "Id",
         onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuPermissions_CreateMenuPermissions_PermissionId",
                table: "MenuPermissions");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuPermissions_Permissions_PermissionId",
                table: "MenuPermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
