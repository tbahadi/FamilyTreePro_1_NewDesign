using FamilyTreePro.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// إعداد SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=familytree.db"));

var app = builder.Build();

// إنشاء وتحديث قاعدة البيانات تلقائياً
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("🔧 جاري إنشاء/تحديث قاعدة البيانات...");

        // إنشاء الجداول إذا لم تكن موجودة
        await db.Database.EnsureCreatedAsync();

        // التحقق من إضافة الحقول الجديدة في جميع الجداول
        await EnsureAllColumnsExist(db, logger);

        // إضافة البيانات الافتراضية (المهن والدول)
        await EnsureDefaultDataExists(db, logger);

        // إضافة المستخدمين الافتراضيين إذا لم يكونوا موجودين
        if (!db.Users.Any())
        {
            await AddDefaultUsers(db, logger);
        }
        else
        {
            // تحديث مستخدم admin إذا كان موجوداً
            await UpdateAdminUser(db, logger);
        }

        // التحقق من البيانات المضافة
        var occupationsCount = await db.Occupations.CountAsync();
        var countriesCount = await db.Countries.CountAsync();
        var usersCount = await db.Users.CountAsync();

        logger.LogInformation($"✅ تم إعداد قاعدة البيانات بنجاح");
        logger.LogInformation($"   - عدد المستخدمين: {usersCount}");
        logger.LogInformation($"   - عدد المهن: {occupationsCount}");
        logger.LogInformation($"   - عدد الدول: {countriesCount}");

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في إعداد قاعدة البيانات");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// الدوال المساعدة لإنشاء قاعدة البيانات
static async Task EnsureAllColumnsExist(AppDbContext db, ILogger logger)
{
    try
    {
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        // التحقق من الحقول في جدول Users
        var userColumnsToAdd = new[]
        {
            new { ColumnName = "IsActive", SqlType = "INTEGER NOT NULL DEFAULT 1" },
            new { ColumnName = "IsAdmin", SqlType = "INTEGER NOT NULL DEFAULT 0" }
        };

        foreach (var column in userColumnsToAdd)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"ALTER TABLE Users ADD COLUMN {column.ColumnName} {column.SqlType}";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation($"✅ تم إضافة حقل {column.ColumnName} إلى جدول Users");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
            {
                logger.LogInformation($"ℹ️ حقل {column.ColumnName} موجود بالفعل في جدول Users");
            }
        }

        // التحقق من الحقول الجديدة في جدول Persons
        var personColumnsToAdd = new[]
        {
            new { ColumnName = "IsOriginalRecord", SqlType = "INTEGER NOT NULL DEFAULT 1" },
            new { ColumnName = "IsConnectionPoint", SqlType = "INTEGER NOT NULL DEFAULT 0" },
            new { ColumnName = "OriginalTreeId", SqlType = "INTEGER" },
            new { ColumnName = "AdditionReason", SqlType = "TEXT" },
            new { ColumnName = "Nickname", SqlType = "TEXT" },
            new { ColumnName = "GrandFatherName", SqlType = "TEXT" },
            new { ColumnName = "LastName", SqlType = "TEXT" },
            new { ColumnName = "Gender", SqlType = "TEXT" },
            new { ColumnName = "BirthDate", SqlType = "TEXT" },
            new { ColumnName = "DeathDate", SqlType = "TEXT" },
            new { ColumnName = "City", SqlType = "TEXT" },
            new { ColumnName = "Photo", SqlType = "TEXT" },
            new { ColumnName = "Notes", SqlType = "TEXT" },
            new { ColumnName = "CreatedDate", SqlType = "TEXT" },
            new { ColumnName = "LastUpdated", SqlType = "TEXT" },
            new { ColumnName = "OccupationId", SqlType = "INTEGER" },
            new { ColumnName = "CountryId", SqlType = "INTEGER" },
            new { ColumnName = "FatherId", SqlType = "INTEGER" },
            new { ColumnName = "MotherId", SqlType = "INTEGER" }
        };

        foreach (var column in personColumnsToAdd)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"ALTER TABLE Persons ADD COLUMN {column.ColumnName} {column.SqlType}";
                await command.ExecuteNonQueryAsync();
                logger.LogInformation($"✅ تم إضافة حقل {column.ColumnName} إلى جدول Persons");
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
            {
                logger.LogInformation($"ℹ️ حقل {column.ColumnName} موجود بالفعل في جدول Persons");
            }
        }

        // التحقق من وجود جداول المهن والدول
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Occupations'";
            var result = await command.ExecuteScalarAsync();
            if (result == null)
            {
                logger.LogInformation("ℹ️ جدول Occupations غير موجود، سيتم إنشاؤه تلقائياً");
            }
            else
            {
                logger.LogInformation("✅ جدول Occupations موجود");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ خطأ في التحقق من جدول Occupations");
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Countries'";
            var result = await command.ExecuteScalarAsync();
            if (result == null)
            {
                logger.LogInformation("ℹ️ جدول Countries غير موجود، سيتم إنشاؤه تلقائياً");
            }
            else
            {
                logger.LogInformation("✅ جدول Countries موجود");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ خطأ في التحقق من جدول Countries");
        }

        await connection.CloseAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في التحقق من الحقول");
    }
}

// الدوال المساعدة لإضافة البيانات الافتراضية
static async Task EnsureDefaultDataExists(AppDbContext db, ILogger logger)
{
    try
    {
        await EnsureDefaultOccupations(db, logger);
        await EnsureDefaultCountries(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في إضافة البيانات الافتراضية");
    }
}

static async Task EnsureDefaultOccupations(AppDbContext db, ILogger logger)
{
    try
    {
        if (!db.Occupations.Any())
        {
            var occupations = new[]
            {
                new Occupation { Name = "طبيب" },
                new Occupation { Name = "مهندس" },
                new Occupation { Name = "معلم" },
                new Occupation { Name = "موظف حكومي" },
                new Occupation { Name = "تاجر" },
                new Occupation { Name = "طالب" },
                new Occupation { Name = "محاسب" },
                new Occupation { Name = "محامي" },
                new Occupation { Name = "ممرض" },
                new Occupation { Name = "ضابط" },
                new Occupation { Name = "جندي" },
                new Occupation { Name = "ربة منزل" },
                new Occupation { Name = "فني" },
                new Occupation { Name = "مبرمج" },
                new Occupation { Name = "مدير" },
                new Occupation { Name = "سائق" },
                new Occupation { Name = "عامل" },
                new Occupation { Name = "فلاح" },
                new Occupation { Name = "راعي" },
                new Occupation { Name = "صياد" },
                new Occupation { Name = "غير محدد" }
            };

            db.Occupations.AddRange(occupations);
            await db.SaveChangesAsync();
            logger.LogInformation($"✅ تم إضافة {occupations.Length} مهنة افتراضية");
        }
        else
        {
            var count = await db.Occupations.CountAsync();
            logger.LogInformation($"ℹ️ يوجد {count} مهنة في قاعدة البيانات");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في إضافة المهن الافتراضية");
    }
}

static async Task EnsureDefaultCountries(AppDbContext db, ILogger logger)
{
    try
    {
        if (!db.Countries.Any())
        {
            var countries = new[]
            {
                new Country { Name = "السعودية" },
                new Country { Name = "مصر" },
                new Country { Name = "الأردن" },
                new Country { Name = "فلسطين" },
                new Country { Name = "سوريا" },
                new Country { Name = "لبنان" },
                new Country { Name = "العراق" },
                new Country { Name = "اليمن" },
                new Country { Name = "الإمارات" },
                new Country { Name = "قطر" },
                new Country { Name = "الكويت" },
                new Country { Name = "البحرين" },
                new Country { Name = "عمان" },
                new Country { Name = "السودان" },
                new Country { Name = "ليبيا" },
                new Country { Name = "الجزائر" },
                new Country { Name = "المغرب" },
                new Country { Name = "تونس" },
                new Country { Name = "غير محدد" }
            };

            db.Countries.AddRange(countries);
            await db.SaveChangesAsync();
            logger.LogInformation($"✅ تم إضافة {countries.Length} دولة افتراضية");
        }
        else
        {
            var count = await db.Countries.CountAsync();
            logger.LogInformation($"ℹ️ يوجد {count} دولة في قاعدة البيانات");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في إضافة الدول الافتراضية");
    }
}

static async Task AddDefaultUsers(AppDbContext db, ILogger logger)
{
    try
    {
        var adminUser = new User
        {
            Username = "admin",
            Password = "admin123", // في الإصدار النهائي استخدم التشفير
            FullName = "مدير النظام",
            Email = "admin@example.com",
            CreatedDate = DateTime.Now,
            IsActive = true,
            IsAdmin = true
        };

        var normalUser = new User
        {
            Username = "user",
            Password = "123456", // في الإصدار النهائي استخدم التشفير
            FullName = "مستخدم عادي",
            Email = "user@example.com",
            CreatedDate = DateTime.Now,
            IsActive = true,
            IsAdmin = false
        };

        db.Users.Add(adminUser);
        db.Users.Add(normalUser);
        await db.SaveChangesAsync();

        logger.LogInformation("✅ تم إضافة المستخدمين الافتراضيين");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في إضافة المستخدمين الافتراضيين");
    }
}

static async Task UpdateAdminUser(AppDbContext db, ILogger logger)
{
    try
    {
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser != null)
        {
            adminUser.IsActive = true;
            adminUser.IsAdmin = true;
            await db.SaveChangesAsync();
            logger.LogInformation("✅ تم تحديث صلاحيات مدير النظام");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في تحديث مدير النظام");
    }
}

static async Task<bool> CanConnectToDatabase(AppDbContext db)
{
    try
    {
        return await db.Database.CanConnectAsync();
    }
    catch
    {
        return false;
    }
}