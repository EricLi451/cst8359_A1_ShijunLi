using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A1_ShijunLi.Migrations
{
    /// <inheritdoc />
    public partial class Assignment3_AddUserIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Attendees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Attendees");
        }
    }
}
