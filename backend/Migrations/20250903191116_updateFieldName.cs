using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTest.Migrations
{
    /// <inheritdoc />
    public partial class updateFieldName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduledArrivalUtc",
                table: "Flights",
                newName: "ScheduledArrivalTimeUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduledArrivalTimeUtc",
                table: "Flights",
                newName: "ScheduledArrivalUtc");
        }
    }
}
