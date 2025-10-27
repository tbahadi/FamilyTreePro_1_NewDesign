using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTreePro.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryIDAndVisibilityToFamilyTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdditionReason",
                table: "Persons",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOriginalRecord",
                table: "Persons",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Persons",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalTreeId",
                table: "Persons",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryID",
                table: "FamilyTrees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDataIndependent",
                table: "FamilyTrees",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "FamilyTrees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CombinedTrees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedTrees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombinedTrees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CombinedTreeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CombinedTreeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FamilyTreeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConnectionPersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedTreeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombinedTreeItems_CombinedTrees_CombinedTreeId",
                        column: x => x.CombinedTreeId,
                        principalTable: "CombinedTrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombinedTreeItems_FamilyTrees_FamilyTreeId",
                        column: x => x.FamilyTreeId,
                        principalTable: "FamilyTrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CombinedTreeItems_Persons_ConnectionPersonId",
                        column: x => x.ConnectionPersonId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrees_CountryID",
                table: "FamilyTrees",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedTreeItems_CombinedTreeId",
                table: "CombinedTreeItems",
                column: "CombinedTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedTreeItems_ConnectionPersonId",
                table: "CombinedTreeItems",
                column: "ConnectionPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedTreeItems_FamilyTreeId",
                table: "CombinedTreeItems",
                column: "FamilyTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_CombinedTrees_UserId",
                table: "CombinedTrees",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyTrees_Countries_CountryID",
                table: "FamilyTrees",
                column: "CountryID",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyTrees_Countries_CountryID",
                table: "FamilyTrees");

            migrationBuilder.DropTable(
                name: "CombinedTreeItems");

            migrationBuilder.DropTable(
                name: "CombinedTrees");

            migrationBuilder.DropIndex(
                name: "IX_FamilyTrees_CountryID",
                table: "FamilyTrees");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdditionReason",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "IsOriginalRecord",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "OriginalTreeId",
                table: "Persons");

            migrationBuilder.DropColumn(
                name: "CountryID",
                table: "FamilyTrees");

            migrationBuilder.DropColumn(
                name: "IsDataIndependent",
                table: "FamilyTrees");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "FamilyTrees");
        }
    }
}
