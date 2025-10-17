using FamilyTreePro.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTreePro.Services
{
    public class DataSyncService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSyncService> _logger;

        public DataSyncService(AppDbContext context, ILogger<DataSyncService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // نسخ بيانات شجرة إلى شجرة أخرى
        public async Task<bool> CopyTreeData(int sourceTreeId, int targetTreeId, int? connectionPersonId)
        {
            try
            {
                _logger.LogInformation($"بدء نسخ بيانات الشجرة {sourceTreeId} إلى الشجرة {targetTreeId}");

                // جلب جميع الأشخاص من الشجرة المصدر
                var sourcePersons = await _context.Persons
                    .Where(p => p.FamilyTreeId == sourceTreeId)
                    .ToListAsync();

                // نسخ الأشخاص إلى الشجرة الهدف
                foreach (var sourcePerson in sourcePersons)
                {
                    var copiedPerson = new Person
                    {
                        FirstName = sourcePerson.FirstName,
                        FatherName = sourcePerson.FatherName,
                        GrandFatherName = sourcePerson.GrandFatherName,
                        LastName = sourcePerson.LastName,
                        Nickname = sourcePerson.Nickname,
                        Gender = sourcePerson.Gender,
                        BirthDate = sourcePerson.BirthDate,
                        DeathDate = sourcePerson.DeathDate,
                        City = sourcePerson.City,
                        Photo = sourcePerson.Photo,
                        Notes = sourcePerson.Notes,
                        OccupationId = sourcePerson.OccupationId,
                        CountryId = sourcePerson.CountryId,
                        FamilyTreeId = targetTreeId, // الشجرة الهدف
                        OriginalTreeId = sourceTreeId, // الشجرة الأصلية
                        IsOriginalRecord = false, // سجل منسوخ
                        CreatedDate = DateTime.Now
                    };

                    _context.Persons.Add(copiedPerson);
                }

                await _context.SaveChangesAsync();

                // تحديث الشجرة المصدر لتكون غير مستقلة
                var sourceTree = await _context.FamilyTrees.FindAsync(sourceTreeId);
                if (sourceTree != null)
                {
                    sourceTree.IsDataIndependent = false;
                    sourceTree.ParentTreeId = targetTreeId;
                    sourceTree.ConnectionPersonId = connectionPersonId;
                    _context.FamilyTrees.Update(sourceTree);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم نسخ {sourcePersons.Count} فرد بنجاح");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في نسخ بيانات الشجرة {sourceTreeId}");
                return false;
            }
        }

        // مزامنة التحديثات من الشجرة الأصلية إلى الشجرة الأم
        public async Task<bool> SyncUpdates(int sourceTreeId, int targetTreeId)
        {
            try
            {
                _logger.LogInformation($"مزامنة التحديثات من الشجرة {sourceTreeId} إلى الشجرة {targetTreeId}");

                // جلب الأشخاص المنسوخين في الشجرة الهدف
                var copiedPersons = await _context.Persons
                    .Where(p => p.FamilyTreeId == targetTreeId &&
                               p.OriginalTreeId == sourceTreeId &&
                               !p.IsOriginalRecord)
                    .ToListAsync();

                // جلب الأشخاص الجدد/المحدثة من الشجرة المصدر
                var sourcePersons = await _context.Persons
                    .Where(p => p.FamilyTreeId == sourceTreeId)
                    .ToListAsync();

                // تحديث أو إضافة الأشخاص
                foreach (var sourcePerson in sourcePersons)
                {
                    var existingCopy = copiedPersons.FirstOrDefault(p =>
                        p.FirstName == sourcePerson.FirstName &&
                        p.FatherName == sourcePerson.FatherName &&
                        p.GrandFatherName == sourcePerson.GrandFatherName);

                    if (existingCopy != null)
                    {
                        // تحديث البيانات
                        existingCopy.BirthDate = sourcePerson.BirthDate;
                        existingCopy.DeathDate = sourcePerson.DeathDate;
                        existingCopy.City = sourcePerson.City;
                        existingCopy.Notes = sourcePerson.Notes;
                        existingCopy.OccupationId = sourcePerson.OccupationId;
                        existingCopy.CountryId = sourcePerson.CountryId;
                    }
                    else
                    {
                        // إضافة جديد
                        var newPerson = new Person
                        {
                            FirstName = sourcePerson.FirstName,
                            FatherName = sourcePerson.FatherName,
                            GrandFatherName = sourcePerson.GrandFatherName,
                            LastName = sourcePerson.LastName,
                            Nickname = sourcePerson.Nickname,
                            Gender = sourcePerson.Gender,
                            BirthDate = sourcePerson.BirthDate,
                            DeathDate = sourcePerson.DeathDate,
                            City = sourcePerson.City,
                            Photo = sourcePerson.Photo,
                            Notes = sourcePerson.Notes,
                            OccupationId = sourcePerson.OccupationId,
                            CountryId = sourcePerson.CountryId,
                            FamilyTreeId = targetTreeId,
                            OriginalTreeId = sourceTreeId,
                            IsOriginalRecord = false,
                            CreatedDate = DateTime.Now
                        };
                        _context.Persons.Add(newPerson);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("تمت المزامنة بنجاح");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في المزامنة");
                return false;
            }
        }
    }
}