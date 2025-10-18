using FamilyTreePro.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

                // التحقق من وجود الشجرات
                var sourceTree = await _context.FamilyTrees.FindAsync(sourceTreeId);
                var targetTree = await _context.FamilyTrees.FindAsync(targetTreeId);

                if (sourceTree == null || targetTree == null)
                {
                    _logger.LogError($"الشجرة المصدر {sourceTreeId} أو الشجرة الهدف {targetTreeId} غير موجودة");
                    return false;
                }

                // جلب جميع الأشخاص من الشجرة المصدر
                var sourcePersons = await _context.Persons
                    .Where(p => p.FamilyTreeId == sourceTreeId)
                    .ToListAsync();

                _logger.LogInformation($"تم العثور على {sourcePersons.Count} فرد في الشجرة المصدر");

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
                        AdditionReason = sourcePerson.AdditionReason,
                        OccupationId = sourcePerson.OccupationId,
                        CountryId = sourcePerson.CountryId,
                        FamilyTreeId = targetTreeId, // الشجرة الهدف
                        OriginalTreeId = sourceTreeId, // الشجرة الأصلية
                        IsOriginalRecord = false, // سجل منسوخ
                        IsConnectionPoint = sourcePerson.IsConnectionPoint,
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };

                    _context.Persons.Add(copiedPerson);
                }

                await _context.SaveChangesAsync();

                // تحديث الشجرة المصدر لتكون غير مستقلة
                sourceTree.IsDataIndependent = false;
                sourceTree.ParentTreeId = targetTreeId;
                sourceTree.ConnectionPersonId = connectionPersonId;
                _context.FamilyTrees.Update(sourceTree);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم نسخ {sourcePersons.Count} فرد بنجاح من الشجرة {sourceTree.Name} إلى الشجرة {targetTree.Name}");
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

                _logger.LogInformation($"تم العثور على {sourcePersons.Count} فرد في الشجرة المصدر و {copiedPersons.Count} فرد منسوخ");

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
                        existingCopy.AdditionReason = sourcePerson.AdditionReason;
                        existingCopy.OccupationId = sourcePerson.OccupationId;
                        existingCopy.CountryId = sourcePerson.CountryId;
                        existingCopy.IsConnectionPoint = sourcePerson.IsConnectionPoint;
                        existingCopy.LastUpdated = DateTime.Now;

                        _context.Persons.Update(existingCopy);
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
                            AdditionReason = sourcePerson.AdditionReason,
                            OccupationId = sourcePerson.OccupationId,
                            CountryId = sourcePerson.CountryId,
                            FamilyTreeId = targetTreeId,
                            OriginalTreeId = sourceTreeId,
                            IsOriginalRecord = false,
                            IsConnectionPoint = sourcePerson.IsConnectionPoint,
                            CreatedDate = DateTime.Now,
                            LastUpdated = DateTime.Now
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

        // فك ارتباط الشجرة
        public async Task<bool> UnlinkTree(int treeId)
        {
            try
            {
                _logger.LogInformation($"فك ارتباط الشجرة {treeId}");

                var tree = await _context.FamilyTrees.FindAsync(treeId);
                if (tree == null)
                {
                    _logger.LogError($"الشجرة {treeId} غير موجودة");
                    return false;
                }

                // حذف الأشخاص المنسوخين من الشجرة الأم
                var copiedPersons = await _context.Persons
                    .Where(p => p.OriginalTreeId == treeId && !p.IsOriginalRecord)
                    .ToListAsync();

                if (copiedPersons.Any())
                {
                    _context.Persons.RemoveRange(copiedPersons);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"تم حذف {copiedPersons.Count} فرد منسوخ");
                }

                // إعادة تعيين الشجرة لتكون مستقلة
                tree.IsDataIndependent = true;
                tree.ParentTreeId = null;
                tree.ConnectionPersonId = null;
                _context.FamilyTrees.Update(tree);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم فك ارتباط الشجرة {treeId} بنجاح");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في فك ارتباط الشجرة {treeId}");
                return false;
            }
        }
    }
}