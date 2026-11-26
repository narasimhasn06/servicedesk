using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceDesk.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingsPaymentStatusStartsAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentStatus_StartsAt",
                table: "Bookings",
                columns: new[] { "PaymentStatus", "StartsAt" })
                .Annotation("Npgsql:IndexInclude", new[] { "CustomerId", "Amount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentStatus_StartsAt",
                table: "Bookings");
        }
    }
}
