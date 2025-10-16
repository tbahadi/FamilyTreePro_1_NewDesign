using FamilyTreePro.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// إضافة خدمات التسجيل
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
        // حذف قاعدة البيانات القديمة وإنشاء جديدة
        await db.Database.EnsureDeletedAsync();
        db.Database.EnsureCreated();

        // إضافة بيانات أولية
        await SeedData(db);
        logger.LogInformation("✅ تم إنشاء قاعدة البيانات بنجاح!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ خطأ في إنشاء قاعدة البيانات");
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



// دالة لإضافة بيانات أولية (محدثة)
async Task SeedData(AppDbContext context)
{
    // إضافة الدول
    if (!context.Countries.Any())
    {
        var countries = new[]
        {
            new Country { Name = "المملكة العربية السعودية", Code = "SA" },
            new Country { Name = "مصر", Code = "EG" },
            new Country { Name = "العراق", Code = "IQ" },
            new Country { Name = "الجزائر", Code = "DZ" },
            new Country { Name = "المغرب", Code = "MA" },
            new Country { Name = "السودان", Code = "SD" },
            new Country { Name = "اليمن", Code = "YE" },
            new Country { Name = "سوريا", Code = "SY" },
            new Country { Name = "تونس", Code = "TN" },
            new Country { Name = "الإمارات العربية المتحدة", Code = "AE" },
            new Country { Name = "قطر", Code = "QA" },
            new Country { Name = "الكويت", Code = "KW" },
            new Country { Name = "البحرين", Code = "BH" },
            new Country { Name = "عُمان", Code = "OM" },
            new Country { Name = "الأردن", Code = "JO" },
            new Country { Name = "لبنان", Code = "LB" },
            new Country { Name = "فلسطين", Code = "PS" },
            new Country { Name = "ليبيا", Code = "LY" },
            new Country { Name = "موريتانيا", Code = "MR" },
            new Country { Name = "جيبوتي", Code = "DJ" },
            new Country { Name = "جزر القمر", Code = "KM" },
            new Country { Name = "الصومال", Code = "SO" }
        };
        await context.Countries.AddRangeAsync(countries);
    }

    // إضافة المهن مع وصف افتراضي
    if (!context.Occupations.Any())
    {
        var occupations = new[]
        {
            new Occupation { Name = "طبيب", Description = "ممارس طبي" },
            new Occupation { Name = "مهندس", Description = "مختص في الهندسة" },
            new Occupation { Name = "معلم", Description = "مربي ومدرس" },
            new Occupation { Name = "تاجر", Description = "رجل أعمال وتجارة" },
            new Occupation { Name = "موظف حكومي", Description = "يعمل في القطاع الحكومي" },
            new Occupation { Name = "طالب", Description = "يدرس في مدرسة أو جامعة" },
            new Occupation { Name = "متقاعد", Description = "أنهى فترة العمل" },
            new Occupation { Name = "ربة منزل", Description = "تعمل في رعاية المنزل والأسرة" },
            new Occupation { Name = "طيار", Description = "يقود الطائرات" },
            new Occupation { Name = "ضابط", Description = "يعمل في القوات المسلحة" },
            new Occupation { Name = "محامي", Description = "ممارس في مجال القانون" },
            new Occupation { Name = "محاسب", Description = "متخصص في المحاسبة" },
            new Occupation { Name = "مبرمج", Description = "مطور برمجيات" },
            new Occupation { Name = "ممرض", Description = "يعمل في التمريض" },
            new Occupation { Name = "صيدلي", Description = "متخصص في الصيدلة" },
            new Occupation { Name = "فنان", Description = "يعمل في المجال الفني" },
            new Occupation { Name = "كاتب", Description = "يعمل في الكتابة والتأليف" },
            new Occupation { Name = "رياضي", Description = "يمارس الرياضة محترفاً" },
            new Occupation { Name = "إمام", Description = "إمام مسجد" },
            new Occupation { Name = "داعية", Description = "يعمل في الدعوة الإسلامية" }
        };
        await context.Occupations.AddRangeAsync(occupations);
    }

    // إضافة مستخدم افتراضي للتجربة
    if (!context.Users.Any())
    {
        var defaultUser = new User
        {
            Username = "admin",
            Password = "123456",
            Email = "admin@example.com",
            FullName = "مدير النظام"
        };
        context.Users.Add(defaultUser);
    }

    await context.SaveChangesAsync();
}