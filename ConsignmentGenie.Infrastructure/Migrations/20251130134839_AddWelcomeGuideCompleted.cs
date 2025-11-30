using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWelcomeGuideCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WelcomeGuideCompleted",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt", "WelcomeGuideCompleted" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 37, 852, DateTimeKind.Utc).AddTicks(1702), new DateTime(2025, 11, 30, 13, 48, 37, 852, DateTimeKind.Utc).AddTicks(1704), false });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6287), new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6288) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(5996), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6039) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6068), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6069) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6086), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6087) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6104), "$2a$11$WLKp9sKXgZaWBhKTjCtVJetdu44MVnJ/pedeCoEO/8xtMznP4j9z.", new DateTime(2025, 11, 30, 13, 48, 38, 415, DateTimeKind.Utc).AddTicks(6104) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WelcomeGuideCompleted",
                table: "Organizations");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 27, 505, DateTimeKind.Utc).AddTicks(7769), new DateTime(2025, 11, 29, 22, 25, 27, 505, DateTimeKind.Utc).AddTicks(7770) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9655), new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9656) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9379), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9416) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9447), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9448) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9463), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9464) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9480), "$2a$11$ao7rj/C16LmCZ9s74B1mcubANh82xO4bZd6aO7hEFb1iImI1QU8ZO", new DateTime(2025, 11, 29, 22, 25, 28, 42, DateTimeKind.Utc).AddTicks(9481) });
        }
    }
}
