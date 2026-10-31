using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabTest.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FlightNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ScheduledArrivalUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OriginAirport = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DestinationAirport = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FlightId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RawCommand = table.Column<string>(type: "TEXT", nullable: false),
                    CheckInMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    BaggageMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    CleaningMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    PbbAngle = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.CheckConstraint("CK_WorkOrder_PbbAngle", "[PbbAngle] IN (0,90,180,270)");
                    table.ForeignKey(
                        name: "FK_WorkOrders_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_FlightId",
                table: "WorkOrders",
                column: "FlightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "Flights");
        }
    }
}
