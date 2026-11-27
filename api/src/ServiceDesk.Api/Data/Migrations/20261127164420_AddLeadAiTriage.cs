using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceDesk.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadAiTriage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiDraftReply",
                table: "Leads",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiPriority",
                table: "Leads",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiService",
                table: "Leads",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiStatus",
                table: "Leads",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "Leads",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiTriagedAt",
                table: "Leads",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiDraftReply",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiPriority",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiService",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiStatus",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AiTriagedAt",
                table: "Leads");
        }
    }
}
