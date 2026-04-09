using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fiap_hackaton.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReportFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Reports",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Reports");
        }
    }
}
