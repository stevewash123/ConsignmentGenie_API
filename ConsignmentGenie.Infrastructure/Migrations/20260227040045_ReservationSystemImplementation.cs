using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsignmentGenie.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReservationSystemImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Orders_OrderId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "CustomerWishlist");

            migrationBuilder.DropTable(
                name: "GuestCheckouts");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Shoppers");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VerificationCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    VerificationCodeSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusChangedReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PickedUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    TotalValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PickupInstructions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reservations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReservationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ItemTitle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ItemSku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ItemImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationItems_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(740), new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(741) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 125, DateTimeKind.Utc).AddTicks(4707), new DateTime(2026, 2, 27, 4, 0, 44, 125, DateTimeKind.Utc).AddTicks(4708) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(470), "$2a$11$ErLitSseups.NSrGXGWHpeOjXlgid7chvEuC90vywQ2oIqiUUn.Kq", new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(472) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(501), "$2a$11$ErLitSseups.NSrGXGWHpeOjXlgid7chvEuC90vywQ2oIqiUUn.Kq", new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(536) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(555), "$2a$11$ErLitSseups.NSrGXGWHpeOjXlgid7chvEuC90vywQ2oIqiUUn.Kq", new DateTime(2026, 2, 27, 4, 0, 44, 692, DateTimeKind.Utc).AddTicks(555) });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationItems_ItemId",
                table: "ReservationItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationItems_ReservationId",
                table: "ReservationItems",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationItems_ReservationId_ItemId",
                table: "ReservationItems",
                columns: new[] { "ReservationId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedAt",
                table: "Reservations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreatedBy",
                table: "Reservations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ExpiresAt",
                table: "Reservations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_OrganizationId",
                table: "Reservations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_OrganizationId_CustomerPhone",
                table: "Reservations",
                columns: new[] { "OrganizationId", "CustomerPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_OrganizationId_Status",
                table: "Reservations",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UpdatedBy",
                table: "Reservations",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationItems");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PasswordResetToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StripeCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Organizations_OrganizationId",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SessionToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
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
                name: "Shoppers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PreferredName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingAddress1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippingAddress2 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "CustomerWishlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerWishlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerWishlist_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerWishlist_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FulfillmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShippingAddress1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShippingAddress2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShippingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ShippingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingCountry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShippingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ShippingZip = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsignorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SplitPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Consignors_ConsignorId",
                        column: x => x.ConsignorId,
                        principalTable: "Consignors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_ShoppingCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "ShoppingCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Consignors",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1513), new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1514) });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 360, DateTimeKind.Utc).AddTicks(8331), new DateTime(2026, 2, 23, 16, 50, 26, 360, DateTimeKind.Utc).AddTicks(8332) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1234), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1274) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1304), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1305) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1320), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1321) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ApprovalStatus", "ApprovedAt", "ApprovedBy", "ClerkPin", "CreatedAt", "Email", "HiredDate", "IsActive", "LastLoginAt", "Name", "OrganizationId", "PasswordHash", "Phone", "PreferredName", "RejectedReason", "Role", "UpdatedAt" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), 1, null, null, null, new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1336), "customer1@microsaasbuilders.com", null, true, null, null, new Guid("11111111-1111-1111-1111-111111111111"), "$2a$11$.v1oX1.fnbSMHYYBA0m0oucB2FAoC.jUmjR1cY1Rc1kcTIVZWLyP2", null, null, null, 3, new DateTime(2026, 2, 23, 16, 50, 26, 913, DateTimeKind.Utc).AddTicks(1336) });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderId",
                table: "Transactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ItemId",
                table: "CartItems",
                columns: new[] { "CartId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ItemId",
                table: "CartItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OrganizationId",
                table: "Customers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OrganizationId_Email",
                table: "Customers",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWishlist_CustomerId",
                table: "CustomerWishlist",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWishlist_ItemId",
                table: "CustomerWishlist",
                column: "ItemId");

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
                name: "IX_OrderItems_ConsignorId",
                table: "OrderItems",
                column: "ConsignorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ItemId",
                table: "OrderItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_ItemId",
                table: "OrderItems",
                columns: new[] { "OrderId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId",
                table: "Orders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrganizationId_OrderNumber",
                table: "Orders",
                columns: new[] { "OrganizationId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

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

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_CustomerId",
                table: "ShoppingCarts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ExpiresAt",
                table: "ShoppingCarts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_OrganizationId",
                table: "ShoppingCarts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_OrganizationId_CustomerId",
                table: "ShoppingCarts",
                columns: new[] { "OrganizationId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_OrganizationId_SessionId",
                table: "ShoppingCarts",
                columns: new[] { "OrganizationId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_SessionId",
                table: "ShoppingCarts",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Orders_OrderId",
                table: "Transactions",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
