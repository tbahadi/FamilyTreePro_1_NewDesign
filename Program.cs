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

// إنشاء قاعدة البيانات تلقائياً
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        db.Database.EnsureCreated();

        if (!db.Users.Any())
        {
            // إضافة مستخدمين افتراضيين
            var adminUser = new User
            {
                Username = "admin",
                Password = "admin123",
                FullName = "مدير النظام",
                Email = "admin@example.com",
                CreatedDate = DateTime.Now
            };
            db.Users.Add(adminUser);

            var normalUser = new User
            {
                Username = "user",
                Password = "123456",
                FullName = "مستخدم عادي",
                Email = "user@example.com",
                CreatedDate = DateTime.Now
            };
            db.Users.Add(normalUser);

            await db.SaveChangesAsync();
            logger.LogInformation("تم إضافة المستخدمين الافتراضيين");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "خطأ في إنشاء قاعدة البيانات");
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


// تحديث قاعدة البيانات تلقائياً عند تشغيل التطبيق
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("🔧 جاري تحديث قاعدة البيانات...");

        // الحصول على الاتصال مباشرة
        var connection = db.Database.GetDbConnection();
        connection.Open();

        // محاولة إضافة الحقول الجديدة
        try
        {
            using var command1 = connection.CreateCommand();
            command1.CommandText = "ALTER TABLE Users ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1";
            command1.ExecuteNonQuery();
            logger.LogInformation("✅ تم إضافة حقل IsActive");
        }
        catch (Exception ex)
        {
            logger.LogInformation($"ℹ️ حقل IsActive موجود بالفعل: {ex.Message}");
        }

        try
        {
            using var command2 = connection.CreateCommand();
            command2.CommandText = "ALTER TABLE Users ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0";
            command2.ExecuteNonQuery();
            logger.LogInformation("✅ تم إضافة حقل IsAdmin");
        }
        catch (Exception ex)
        {
            logger.LogInformation($"ℹ️ حقل IsAdmin موجود بالفعل: {ex.Message}");
        }

        // تحديث مستخدم admin
        var adminUser = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (adminUser != null)
        {
            // استخدام Reflection لتحديث الحقول الجديدة بشكل آمن
            var isActiveProp = adminUser.GetType().GetProperty("IsActive");
            var isAdminProp = adminUser.GetType().GetProperty("IsAdmin");

            if (isActiveProp != null) isActiveProp.SetValue(adminUser, true);
            if (isAdminProp != null) isAdminProp.SetValue(adminUser, true);

            db.SaveChanges();
            logger.LogInformation("✅ تم ترقية admin إلى مدير نظام");
        }

        connection.Close();
        logger.LogInformation("🎉 تم تحديث قاعدة البيانات بنجاح");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في تحديث قاعدة البيانات");
    }
}

app.Run();