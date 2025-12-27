using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travelette.Relay.Migrations
{
    /// <inheritdoc />
    public partial class AddAmountRefundedToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountRefunded",
                table: "Bookings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountRefunded",
                table: "Bookings");
        }
    }
}
