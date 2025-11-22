using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryCRUDImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemCategories_CategoryId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MaximumStock",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MinimumStock",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OverrideSplitPercentage",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "SKU",
                table: "Items",
                newName: "Sku");

            migrationBuilder.RenameColumn(
                name: "SearchKeywords",
                table: "Items",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "Model",
                table: "Items",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "ExtendedProperties",
                table: "Items",
                newName: "Measurements");

            migrationBuilder.RenameColumn(
                name: "CostBasis",
                table: "Items",
                newName: "OriginalPrice");

            migrationBuilder.RenameIndex(
                name: "IX_Items_OrganizationId_SKU",
                table: "Items",
                newName: "IX_Items_OrganizationId_Sku");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId1",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Condition",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpirationDate",
                table: "Items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ItemCategoryId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ListedDate",
                table: "Items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Materials",
                table: "Items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPrice",
                table: "Items",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageUrl",
                table: "Items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReceivedDate",
                table: "Items",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "SoldDate",
                table: "Items",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusChangedReason",
                table: "Items",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestCheckouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SessionToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestCheckouts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemImages_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemImages_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Shoppers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ShippingAddress1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippingAddress2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shoppers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shoppers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shoppers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Slug", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 14, 27, 15, 230, DateTimeKind.Utc).AddTicks(2705), null, new DateTime(2025, 11, 22, 14, 27, 15, 230, DateTimeKind.Utc).AddTicks(2708) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9765), new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9765) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9505), "$2a$11$fKxFvMJPrG0AEpVF0xEqZemSqFAIRnaKXl/.4R7ONBN8jtZ8YIFzS", new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9506) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9535), "$2a$11$fKxFvMJPrG0AEpVF0xEqZemSqFAIRnaKXl/.4R7ONBN8jtZ8YIFzS", new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9535) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9551), "$2a$11$fKxFvMJPrG0AEpVF0xEqZemSqFAIRnaKXl/.4R7ONBN8jtZ8YIFzS", new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9551) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9566), "$2a$11$fKxFvMJPrG0AEpVF0xEqZemSqFAIRnaKXl/.4R7ONBN8jtZ8YIFzS", new DateTime(2025, 11, 22, 14, 27, 15, 452, DateTimeKind.Utc).AddTicks(9567) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ItemId1",
                table: "Transactions",
                column: "ItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedBy",
                table: "Items",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemCategoryId",
                table: "Items",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_OrganizationId_Category",
                table: "Items",
                columns: new[] { "OrganizationId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_OrganizationId_Status",
                table: "Items",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_UpdatedBy",
                table: "Items",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OrganizationId_Name",
                table: "Categories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckouts_ExpiresAt",
                table: "GuestCheckouts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckouts_OrganizationId",
                table: "GuestCheckouts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestCheckouts_SessionToken",
                table: "GuestCheckouts",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemImages_CreatedBy",
                table: "ItemImages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ItemImages_ItemId",
                table: "ItemImages",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Shoppers_OrganizationId",
                table: "Shoppers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Shoppers_OrganizationId_Email",
                table: "Shoppers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shoppers_UserId",
                table: "Shoppers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemCategories_ItemCategoryId",
                table: "Items",
                column: "ItemCategoryId",
                principalTable: "ItemCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Users_CreatedBy",
                table: "Items",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Users_UpdatedBy",
                table: "Items",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Items_ItemId1",
                table: "Transactions",
                column: "ItemId1",
                principalTable: "Items",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Categories_CategoryId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemCategories_ItemCategoryId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Users_CreatedBy",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Users_UpdatedBy",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Items_ItemId1",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "GuestCheckouts");

            migrationBuilder.DropTable(
                name: "ItemImages");

            migrationBuilder.DropTable(
                name: "Shoppers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ItemId1",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Items_CreatedBy",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemCategoryId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_OrganizationId_Category",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_OrganizationId_Status",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_UpdatedBy",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemId1",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemCategoryId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ListedDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Materials",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MinimumPrice",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PrimaryImageUrl",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReceivedDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SoldDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "StatusChangedReason",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "Items",
                newName: "SKU");

            migrationBuilder.RenameColumn(
                name: "OriginalPrice",
                table: "Items",
                newName: "CostBasis");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Items",
                newName: "SearchKeywords");

            migrationBuilder.RenameColumn(
                name: "Measurements",
                table: "Items",
                newName: "ExtendedProperties");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Items",
                newName: "Model");

            migrationBuilder.RenameIndex(
                name: "IX_Items_OrganizationId_Sku",
                table: "Items",
                newName: "IX_Items_OrganizationId_SKU");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Condition",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "Items",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "Items",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximumStock",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStock",
                table: "Items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OverrideSplitPercentage",
                table: "Items",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Items",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "Items",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 46, 788, DateTimeKind.Utc).AddTicks(3081), new DateTime(2025, 11, 22, 13, 19, 46, 788, DateTimeKind.Utc).AddTicks(3082) });

            migrationBuilder.UpdateData(
                table: "Providers",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8963), new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8963) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8657), "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8666) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8693), "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8693) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8710), "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8711) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8726), "$2a$11$CGLc9CD5/XAJqp1eCd67yelz9daNNKGQM9maAEEHIJVek/bBfTqGi", new DateTime(2025, 11, 22, 13, 19, 47, 3, DateTimeKind.Utc).AddTicks(8727) });

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemCategories_CategoryId",
                table: "Items",
                column: "CategoryId",
                principalTable: "ItemCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
