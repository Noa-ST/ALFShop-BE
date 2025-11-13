using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Adfeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AverageResponseTimeSeconds",
                table: "Shops",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "FeaturedWeight",
                table: "Shops",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "FulfilledRate",
                table: "Shops",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnlineStatus",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedUntil",
                table: "Shops",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RankingScore",
                table: "Shops",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<float>(
                name: "ReturnRate",
                table: "Shops",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "AddsToCart30d",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AddsToCart7d",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "DiscountPercent",
                table: "Products",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<double>(
                name: "FeaturedWeight",
                table: "Products",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedUntil",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RankingScore",
                table: "Products",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Sold30d",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Sold7d",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Views30d",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Views7d",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "FeaturedWeight",
                table: "GlobalCategoris",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "GlobalCategoris",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedUntil",
                table: "GlobalCategoris",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RankingScore",
                table: "GlobalCategoris",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "FeaturedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Device = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Region = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    City = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeaturedRankings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    PinBoost = table.Column<double>(type: "double precision", nullable: false),
                    Metric1 = table.Column<double>(type: "double precision", nullable: false),
                    Metric2 = table.Column<double>(type: "double precision", nullable: false),
                    Metric3 = table.Column<double>(type: "double precision", nullable: false),
                    Penalty = table.Column<double>(type: "double precision", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedRankings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedEvents_EntityType_EntityId_EventType_CreatedAt",
                table: "FeaturedEvents",
                columns: new[] { "EntityType", "EntityId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedRankings_EntityType_EntityId_ComputedAt",
                table: "FeaturedRankings",
                columns: new[] { "EntityType", "EntityId", "ComputedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeaturedEvents");

            migrationBuilder.DropTable(
                name: "FeaturedRankings");

            migrationBuilder.DropColumn(
                name: "AverageResponseTimeSeconds",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "FeaturedWeight",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "FulfilledRate",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "OnlineStatus",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "PinnedUntil",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "RankingScore",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ReturnRate",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "AddsToCart30d",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AddsToCart7d",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FeaturedWeight",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PinnedUntil",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RankingScore",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sold30d",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sold7d",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Views30d",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Views7d",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FeaturedWeight",
                table: "GlobalCategoris");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "GlobalCategoris");

            migrationBuilder.DropColumn(
                name: "PinnedUntil",
                table: "GlobalCategoris");

            migrationBuilder.DropColumn(
                name: "RankingScore",
                table: "GlobalCategoris");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
