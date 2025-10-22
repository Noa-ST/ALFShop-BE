using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDatabase : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. DROP FK CŨ VÀ BẢNG CŨ (Giữ nguyên code tự động)
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categories");

            // 2. RENAME CỘT (CategoryId -> GlobalCategoryId) (Giữ nguyên code tự động)
            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Products",
                newName: "GlobalCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                newName: "IX_Products_GlobalCategoryId");

            // 3. CREATE BẢNG GLOBAL CATEGORIES MỚI (Giữ nguyên code tự động)
            migrationBuilder.CreateTable(
                name: "GlobalCategoris",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalCategoris", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GlobalCategoris_GlobalCategoris_ParentId",
                        column: x => x.ParentId,
                        principalTable: "GlobalCategoris",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopCategories",
                // ... (Cấu hình ShopCategories giữ nguyên)
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShopId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopCategories_ShopCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ShopCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopCategories_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopCategories_Shops_ShopId1",
                        column: x => x.ShopId1,
                        principalTable: "Shops",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlobalCategoris_ParentId",
                table: "GlobalCategoris",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCategories_ParentId",
                table: "ShopCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCategories_ShopId",
                table: "ShopCategories",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopCategories_ShopId1",
                table: "ShopCategories",
                column: "ShopId1");


            // -----------------------------------------------------------------------
            // ✅ [KHẮC PHỤC CUỐI CÙNG]: Sử dụng SQL thô cho cả INSERT và UPDATE
            // -----------------------------------------------------------------------

            var defaultUncategorizedId = Guid.NewGuid();
            var defaultUncategorizedIdString = defaultUncategorizedId.ToString();
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // 1. INSERT Danh mục Mặc định
            migrationBuilder.Sql($@"
                INSERT INTO [GlobalCategoris] ([Id], [Name], [IsDeleted], [CreatedAt]) 
                VALUES ('{defaultUncategorizedIdString}', 'Uncategorized (Migrated)', 0, '{now}')
            ");

            // 2. CẬP NHẬT TẤT CẢ Sản phẩm
            // Cập nhật tất cả các GlobalCategoryId hiện có về ID mới. Điều này giải quyết xung đột.
            migrationBuilder.Sql($@"
                UPDATE [Products] 
                SET [GlobalCategoryId] = '{defaultUncategorizedIdString}' 
                WHERE 1 = 1;
            ");

            // -----------------------------------------------------------------------
            // ✅ KẾT THÚC KHẮC PHỤC
            // -----------------------------------------------------------------------


            // 4. ADD FOREIGN KEY MỚI (Lệnh này sẽ thành công vì tất cả sản phẩm đều trỏ đến ID hợp lệ)
            migrationBuilder.AddForeignKey(
                name: "FK_Products_GlobalCategoris_GlobalCategoryId",
                table: "Products",
                column: "GlobalCategoryId",
                principalTable: "GlobalCategoris",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_GlobalCategoris_GlobalCategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "GlobalCategoris");

            migrationBuilder.DropTable(
                name: "ShopCategories");

            migrationBuilder.RenameColumn(
                name: "GlobalCategoryId",
                table: "Products",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_GlobalCategoryId",
                table: "Products",
                newName: "IX_Products_CategoryId");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ShopId",
                table: "Categories",
                column: "ShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
