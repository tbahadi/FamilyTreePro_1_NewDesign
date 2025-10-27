using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTreePro.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithModifiedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Occupations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occupations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "FamilyTrees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountryID = table.Column<int>(type: "INTEGER", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentTreeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ConnectionPersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDataIndependent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyTrees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyTrees_Countries_CountryID",
                        column: x => x.CountryID,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyTrees_FamilyTrees_ParentTreeId",
                        column: x => x.ParentTreeId,
                        principalTable: "FamilyTrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyTrees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    FatherName = table.Column<string>(type: "TEXT", nullable: false),
                    GrandFatherName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: false),
                    IsOriginalRecord = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsConnectionPoint = table.Column<bool>(type: "INTEGER", nullable: false),
                    OriginalTreeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeathDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    Photo = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    AdditionReason = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FamilyTreeId = table.Column<int>(type: "INTEGER", nullable: false),
                    OccupationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CountryId = table.Column<int>(type: "INTEGER", nullable: true),
                    FatherId = table.Column<int>(type: "INTEGER", nullable: true),
                    MotherId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Persons_FamilyTrees_FamilyTreeId",
                        column: x => x.FamilyTreeId,
                        principalTable: "FamilyTrees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Persons_Occupations_OccupationId",
                        column: x => x.OccupationId,
                        principalTable: "Occupations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Persons_Persons_FatherId",
                        column: x => x.FatherId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Persons_Persons_MotherId",
                        column: x => x.MotherId,
                        principalTable: "Persons",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrees_ConnectionPersonId",
                table: "FamilyTrees",
                column: "ConnectionPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrees_CountryID",
                table: "FamilyTrees",
                column: "CountryID");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrees_ParentTreeId",
                table: "FamilyTrees",
                column: "ParentTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyTrees_UserId",
                table: "FamilyTrees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_CountryId",
                table: "Persons",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_FamilyTreeId",
                table: "Persons",
                column: "FamilyTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_FatherId",
                table: "Persons",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_MotherId",
                table: "Persons",
                column: "MotherId");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_OccupationId",
                table: "Persons",
                column: "OccupationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CombinedTreeItems_FamilyTrees_FamilyTreeId",
                table: "CombinedTreeItems",
                column: "FamilyTreeId",
                principalTable: "FamilyTrees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CombinedTreeItems_Persons_ConnectionPersonId",
                table: "CombinedTreeItems",
                column: "ConnectionPersonId",
                principalTable: "Persons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyTrees_Persons_ConnectionPersonId",
                table: "FamilyTrees",
                column: "ConnectionPersonId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Persons_FamilyTrees_FamilyTreeId",
                table: "Persons");

            migrationBuilder.DropTable(
                name: "CombinedTreeItems");

            migrationBuilder.DropTable(
                name: "CombinedTrees");

            migrationBuilder.DropTable(
                name: "FamilyTrees");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Occupations");
        }
    }
}
