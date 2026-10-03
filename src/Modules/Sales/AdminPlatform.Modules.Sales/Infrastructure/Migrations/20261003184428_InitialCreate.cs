using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPlatform.Modules.Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "delivery_methods",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    base_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    free_shipping_threshold = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    estimated_min_minutes = table.Column<int>(type: "integer", nullable: true),
                    estimated_max_minutes = table.Column<int>(type: "integer", nullable: true),
                    pickup_address = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_methods", x => x.id);
                    table.CheckConstraint("ck_delivery_methods_base_fee_non_negative", "base_fee >= 0");
                    table.CheckConstraint("ck_delivery_methods_threshold_non_negative", "free_shipping_threshold IS NULL OR free_shipping_threshold >= 0");
                });

            migrationBuilder.CreateTable(
                name: "order_code_counters",
                schema: "sales",
                columns: table => new
                {
                    counter_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_code_counters", x => x.counter_key);
                });

            migrationBuilder.CreateTable(
                name: "order_option_groups",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    selection_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_option_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_settings",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_code_prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    order_code_date_format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    order_code_sequence_length = table.Column<int>(type: "integer", nullable: false),
                    payment_session_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_settings", x => x.id);
                    table.CheckConstraint("ck_order_settings_sequence_length", "order_code_sequence_length BETWEEN 3 AND 8");
                    table.CheckConstraint("ck_order_settings_session_minutes", "payment_session_minutes BETWEEN 5 AND 240");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    delivery_address_snapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    payment_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payment_method_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    delivery_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    delivery_method_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_pickup = table.Column<bool>(type: "boolean", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shipping_discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    order_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    wants_utensils = table.Column<bool>(type: "boolean", nullable: false),
                    customer_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_amounts_non_negative", "subtotal >= 0 AND discount >= 0 AND shipping_discount >= 0 AND delivery_fee >= 0 AND total_amount >= 0");
                    table.CheckConstraint("ck_orders_total_matches", "total_amount = subtotal - discount + delivery_fee - shipping_discount");
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    icon_media_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    group = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    gateway = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_holder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    min_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    max_order_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.CheckConstraint("ck_payment_methods_max_non_negative", "max_order_amount IS NULL OR max_order_amount >= 0");
                    table.CheckConstraint("ck_payment_methods_min_non_negative", "min_order_amount IS NULL OR min_order_amount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "payment_sessions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    channel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payment_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payment_method_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    delivery_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    delivery_method_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_pickup = table.Column<bool>(type: "boolean", nullable: false),
                    delivery_address_snapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    wants_utensils = table.Column<bool>(type: "boolean", nullable: false),
                    customer_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shipping_fee = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    promotion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shipping_discount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_holder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    request_json = table.Column<string>(type: "jsonb", nullable: false),
                    lines_json = table.Column<string>(type: "jsonb", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_sessions", x => x.id);
                    table.CheckConstraint("ck_payment_sessions_total_non_negative", "total_amount >= 0");
                });

            migrationBuilder.CreateTable(
                name: "order_option_values",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_option_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    price_adjustment = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_option_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_option_values_order_option_groups_order_option_group_",
                        column: x => x.order_option_group_id,
                        principalSchema: "sales",
                        principalTable: "order_option_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_image_media_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    item_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.CheckConstraint("ck_order_items_amounts_non_negative", "unit_price >= 0 AND line_total >= 0");
                    table.CheckConstraint("ck_order_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "sales",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_option_selections",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    price_adjustment = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_option_selections", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_option_selections_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "sales",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_status_histories",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    to_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_status_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_status_histories_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "sales",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payment_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payment_method_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gateway = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gateway_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    paid_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount_non_negative", "amount >= 0");
                    table.ForeignKey(
                        name: "fk_payments_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "sales",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_item_modifiers",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    price_adjustment = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_modifiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_modifiers_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "sales",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    result = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    gateway = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gateway_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_transactions_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "sales",
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_methods_code",
                schema: "sales",
                table: "delivery_methods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_methods_is_active_display_order",
                schema: "sales",
                table: "delivery_methods",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_modifiers_order_item_id",
                schema: "sales",
                table: "order_item_modifiers",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                schema: "sales",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_product_id",
                schema: "sales",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_option_selections_order_id",
                schema: "sales",
                table: "order_option_selections",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_option_values_order_option_group_id",
                schema: "sales",
                table: "order_option_values",
                column: "order_option_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_status_histories_order_id",
                schema: "sales",
                table: "order_status_histories",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_at_utc",
                schema: "sales",
                table: "orders",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id",
                schema: "sales",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_idempotency_key",
                schema: "sales",
                table: "orders",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_code",
                schema: "sales",
                table: "orders",
                column: "order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_status_created_at_utc",
                schema: "sales",
                table: "orders",
                columns: new[] { "order_status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_phone",
                schema: "sales",
                table: "orders",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_code",
                schema: "sales",
                table: "payment_methods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_is_active_display_order",
                schema: "sales",
                table: "payment_methods",
                columns: new[] { "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_sessions_idempotency_key",
                schema: "sales",
                table: "payment_sessions",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_sessions_reference_code",
                schema: "sales",
                table: "payment_sessions",
                column: "reference_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_sessions_status_created_at_utc",
                schema: "sales",
                table: "payment_sessions",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_payment_id",
                schema: "sales",
                table: "payment_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_order_id",
                schema: "sales",
                table: "payments",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_status_created_at_utc",
                schema: "sales",
                table: "payments",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_methods",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_code_counters",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_item_modifiers",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_option_selections",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_option_values",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_settings",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_status_histories",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "payment_sessions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "payment_transactions",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "order_option_groups",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "sales");
        }
    }
}
