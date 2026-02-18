using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalService.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositPaid",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DepositPaidAt",
                table: "Rentals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmountPaid",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FullyPaidAt",
                table: "Rentals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDepositPaid",
                table: "Rentals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFullyPaid",
                table: "Rentals",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositPaid",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DepositPaidAt",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "FinalAmountPaid",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "FullyPaidAt",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "IsDepositPaid",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "IsFullyPaid",
                table: "Rentals");
        }
    }
}
