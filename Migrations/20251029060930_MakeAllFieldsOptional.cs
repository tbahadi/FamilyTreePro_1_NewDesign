using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyTreePro.Migrations
{
    /// <inheritdoc />
    public partial class MakeAllFieldsOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. إضافة الحقول الجديدة أولاً
            migrationBuilder.AddColumn<bool>(
                name: "ClanStart",
                table: "Persons",
                type: "INTEGER",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFounder",
                table: "Persons",
                type: "INTEGER",
                nullable: true,
                defaultValue: false);

            // 2. إنشاء جدول مؤقت بالهيكل الجديد
            migrationBuilder.Sql(@"
                CREATE TABLE Persons_temp (
                    Id INTEGER NOT NULL CONSTRAINT PK_Persons PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    FatherName TEXT,
                    GrandFatherName TEXT,
                    LastName TEXT,
                    Nickname TEXT,
                    IsFounder INTEGER DEFAULT 0,
                    ClanStart INTEGER DEFAULT 0,
                    IsOriginalRecord INTEGER DEFAULT 1,
                    IsConnectionPoint INTEGER DEFAULT 0,
                    OriginalTreeId INTEGER,
                    Gender TEXT NOT NULL,
                    BirthDate TEXT,
                    DeathDate TEXT,
                    City TEXT,
                    Photo TEXT,
                    Notes TEXT,
                    AdditionReason TEXT,
                    CreatedDate TEXT NOT NULL,
                    LastUpdated TEXT,
                    FamilyTreeId INTEGER NOT NULL,
                    OccupationId INTEGER,
                    CountryId INTEGER,
                    FatherId INTEGER,
                    MotherId INTEGER,
                    CONSTRAINT FK_Persons_Countries_CountryId FOREIGN KEY (CountryId) REFERENCES Countries (Id),
                    CONSTRAINT FK_Persons_FamilyTrees_FamilyTreeId FOREIGN KEY (FamilyTreeId) REFERENCES FamilyTrees (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Persons_Occupations_OccupationId FOREIGN KEY (OccupationId) REFERENCES Occupations (Id),
                    CONSTRAINT FK_Persons_Persons_FatherId FOREIGN KEY (FatherId) REFERENCES Persons_temp (Id),
                    CONSTRAINT FK_Persons_Persons_MotherId FOREIGN KEY (MotherId) REFERENCES Persons_temp (Id)
                );
            ");

            // 3. نسخ البيانات من الجدول القديم إلى الجديد
            migrationBuilder.Sql(@"
                INSERT INTO Persons_temp (
                    Id, FirstName, FatherName, GrandFatherName, LastName,
                    Nickname, IsFounder, ClanStart, IsOriginalRecord, IsConnectionPoint,
                    OriginalTreeId, Gender, BirthDate, DeathDate, City,
                    Photo, Notes, AdditionReason, CreatedDate, LastUpdated,
                    FamilyTreeId, OccupationId, CountryId, FatherId, MotherId
                )
                SELECT 
                    Id, FirstName, FatherName, GrandFatherName, LastName,
                    Nickname, 0, 0,
                    COALESCE(IsOriginalRecord, 1),
                    COALESCE(IsConnectionPoint, 0),
                    OriginalTreeId, Gender, BirthDate, DeathDate, City,
                    Photo, Notes, AdditionReason, CreatedDate, LastUpdated,
                    FamilyTreeId, OccupationId, CountryId, FatherId, MotherId
                FROM Persons;
            ");

            // 4. حذف الجدول القديم
            migrationBuilder.DropTable(
                name: "Persons");

            // 5. إعادة تسمية الجدول الجديد
            migrationBuilder.Sql("ALTER TABLE Persons_temp RENAME TO Persons;");

            // 6. إعادة إنشاء الفهارس
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. إنشاء جدول مؤقت بالهيكل القديم (للاسترجاع)
            migrationBuilder.Sql(@"
                CREATE TABLE Persons_old (
                    Id INTEGER NOT NULL CONSTRAINT PK_Persons PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL,
                    FatherName TEXT NOT NULL,
                    GrandFatherName TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    Nickname TEXT NOT NULL,
                    IsOriginalRecord INTEGER NOT NULL,
                    IsConnectionPoint INTEGER NOT NULL,
                    OriginalTreeId INTEGER,
                    Gender TEXT NOT NULL,
                    BirthDate TEXT,
                    DeathDate TEXT,
                    City TEXT NOT NULL,
                    Photo TEXT NOT NULL,
                    Notes TEXT NOT NULL,
                    AdditionReason TEXT NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    LastUpdated TEXT,
                    FamilyTreeId INTEGER NOT NULL,
                    OccupationId INTEGER,
                    CountryId INTEGER,
                    FatherId INTEGER,
                    MotherId INTEGER,
                    CONSTRAINT FK_Persons_Countries_CountryId FOREIGN KEY (CountryId) REFERENCES Countries (Id),
                    CONSTRAINT FK_Persons_FamilyTrees_FamilyTreeId FOREIGN KEY (FamilyTreeId) REFERENCES FamilyTrees (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Persons_Occupations_OccupationId FOREIGN KEY (OccupationId) REFERENCES Occupations (Id),
                    CONSTRAINT FK_Persons_Persons_FatherId FOREIGN KEY (FatherId) REFERENCES Persons_old (Id),
                    CONSTRAINT FK_Persons_Persons_MotherId FOREIGN KEY (MotherId) REFERENCES Persons_old (Id)
                );
            ");

            // 2. نسخ البيانات من الجدول الجديد إلى القديم (مع تعيين قيم افتراضية للحقول المطلوبة)
            migrationBuilder.Sql(@"
                INSERT INTO Persons_old (
                    Id, FirstName, FatherName, GrandFatherName, LastName,
                    Nickname, IsOriginalRecord, IsConnectionPoint,
                    OriginalTreeId, Gender, BirthDate, DeathDate, City,
                    Photo, Notes, AdditionReason, CreatedDate, LastUpdated,
                    FamilyTreeId, OccupationId, CountryId, FatherId, MotherId
                )
                SELECT 
                    Id, FirstName, 
                    COALESCE(FatherName, 'غير معروف'),
                    COALESCE(GrandFatherName, 'غير معروف'),
                    COALESCE(LastName, 'غير معروف'),
                    COALESCE(Nickname, 'لا يوجد'),
                    COALESCE(IsOriginalRecord, 1),
                    COALESCE(IsConnectionPoint, 0),
                    OriginalTreeId, Gender, BirthDate, DeathDate,
                    COALESCE(City, 'غير محدد'),
                    COALESCE(Photo, ''),
                    COALESCE(Notes, 'لا يوجد'),
                    COALESCE(AdditionReason, ''),
                    CreatedDate, LastUpdated,
                    FamilyTreeId, OccupationId, CountryId, FatherId, MotherId
                FROM Persons;
            ");

            // 3. حذف الجدول الجديد
            migrationBuilder.DropTable(
                name: "Persons");

            // 4. إعادة تسمية الجدول القديم
            migrationBuilder.Sql("ALTER TABLE Persons_old RENAME TO Persons;");

            // 5. إعادة إنشاء الفهارس
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

            // 6. إزالة الحقول الجديدة (لن يتم تنفيذ هذا لأننا أعدنا الجدول القديم)
            // migrationBuilder.DropColumn(name: "IsFounder", table: "Persons");
            // migrationBuilder.DropColumn(name: "ClanStart", table: "Persons");
        }
    }
}