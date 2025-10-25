using ICRent.Domain.Entities;
using ICRent.Persistence.Repositories.Abstractions;
using ICRent.Persistence.Repositories.Audits;
using ICRent.Persistence.Repositories.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace ICRent.Web.Controllers
{
    [Authorize(Policy = "UserOnly")]
    public class WorkLogsController : Controller
    {
        private readonly IWorkLogQueryRepository _workLogQuery;
        private readonly IWorkLogCommandRepository _workLogCommand;
        private readonly VehicleRepository _vehicles;
        private readonly AuditRepository _audit;

        public WorkLogsController(IWorkLogQueryRepository workLogQuery, VehicleRepository vehicles, AuditRepository audit, IWorkLogCommandRepository workLogCommand)
        {
            _vehicles = vehicles;
            _audit = audit;
            _workLogQuery = workLogQuery;
            _workLogCommand = workLogCommand;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? vehicleId, DateOnly? start, DateOnly? end)
        {
            ViewBag.Vehicles = await _vehicles.GetAllAsync();
            ViewBag.VehicleId = vehicleId ?? 0;

            // İlk geliş: seçim yoksa tabloyu göstermeyelim
            if (vehicleId is null || vehicleId <= 0 || start is null || end is null)
            {
                ViewBag.StartString = "";
                ViewBag.EndString = "";
                return View(new List<WorkLog>());
            }

            ViewBag.StartString = start.Value.ToString("yyyy-MM-dd");
            ViewBag.EndString = end.Value.ToString("yyyy-MM-dd");

            var list = await _workLogQuery.GetByVehicleRangeAsync(vehicleId.Value, start.Value, end.Value);
            return View(list);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Vehicles = await _vehicles.GetAllAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int vehicleId, DateOnly workDate, decimal activeHours, decimal maintenanceHours)
        {
            // Basit alan doğrulamaları
            if (activeHours < 0 || maintenanceHours < 0)
                ModelState.AddModelError(string.Empty, "Saatler negatif olamaz.");
            if (activeHours + maintenanceHours > 24)
                ModelState.AddModelError(string.Empty, "Bir gün için toplam (Aktif + Bakım) 24 saati geçemez.");

            if (!ModelState.IsValid)
            {
                ViewBag.Vehicles = await _vehicles.GetAllAsync();
                return View();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                await _workLogCommand.UpsertAsync(vehicleId, workDate, activeHours, maintenanceHours, userId);

                await _audit.LogAsync(userId, "Ekleme", "Is Kayitlari", vehicleId,
                $"{{\"VehicleId\":{vehicleId},\"WorkDate\":\"{workDate:yyyy-MM-dd}\"," +
                $"\"ActiveHours\":{activeHours},\"MaintenanceHours\":{maintenanceHours}}}");

                TempData["Success"] = "Kayıt eklendi.";
                return RedirectToAction(nameof(Create));
            }
            catch (InvalidOperationException)
            {
                // Kullanıcı dostu mesaj
                ModelState.AddModelError(string.Empty,
                    "Bu araç için seçtiğiniz günde toplam 24 saat dolmuş görünüyor. "
                  + "Mevcut kayıtları düzenleyerek güncelleme yapabilirsiniz.");
                ViewBag.Vehicles = await _vehicles.GetAllAsync();
                return View(); // View'da zaten asp-validation-summary var, mesaj burada çıkar.
            }
        }


        [HttpGet]
        public async Task<IActionResult> List(int vehicleId)
        {
            var list = await _workLogQuery.GetByVehicleAsync(vehicleId);
            ViewBag.VehicleId = vehicleId;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _workLogQuery.GetByIdAsync(id);
            if (m is null) return NotFound();
            ViewBag.Vehicle = (await _vehicles.GetAllAsync()).First(v => v.Id == m.VehicleId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal activeHours, decimal maintenanceHours)
        {
            var m = await _workLogQuery.GetByIdAsync(id);
            if (m is null) return NotFound();

            // Sunucu tarafı doğrulama
            if (activeHours < 0 || maintenanceHours < 0)
                ModelState.AddModelError(string.Empty, "Saatler negatif olamaz.");
            if (activeHours + maintenanceHours > 24)
                ModelState.AddModelError(string.Empty, "Bir gün için toplam (Aktif + Bakım) 24 saati geçemez.");

            if (!ModelState.IsValid)
            {
                // View için tekrar select doldur
                ViewBag.Vehicles = await _vehicles.GetAllAsync();

                // Kullanıcının girdiği (ama doğrulamadan kalan) değerleri ViewBag ile taşı
                ViewBag.PostActive = activeHours;
                ViewBag.PostMaint = maintenanceHours;

                return View(m); // m'yi dokunmadan geri yolla
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // DELTA hesapla: yeni - eski
            var deltaA = activeHours - m.ActiveHours;
            var deltaM = maintenanceHours - m.MaintenanceHours;

            try
            {
                // Upsert delta değil "ekleme" gibi çalışıyorsa, repo 24h kontrolünü yapıyor.
                await _workLogCommand.UpsertAsync(m.VehicleId, m.WorkDate, activeHours, maintenanceHours, userId);

                await _audit.LogAsync(userId, "Güncelleme", "Is Kayitlari", id,
                 $"{{\"VehicleId\":{m.VehicleId},\"WorkDate\":\"{m.WorkDate:yyyy-MM-dd}\"," +
                 $"\"ActiveHours\":{activeHours},\"MaintenanceHours\":{maintenanceHours}}}");

                TempData["Success"] = "Kayıt güncellendi.";
                return RedirectToAction(nameof(Index), new { vehicleId = m.VehicleId });
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, "Bugün için bu araca ayrılan 24 saat doldu. Lütfen saatleri azaltın.");
                ViewBag.PostActive = activeHours;
                ViewBag.PostMaint = maintenanceHours;
                ViewBag.Vehicle = (await _vehicles.GetAllAsync()).First(v => v.Id == m.VehicleId);
                return View(m);
            }
        }


        // DELETE
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _workLogQuery.GetByIdAsync(id);
            if (m is null) return NotFound();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _workLogCommand.DeleteAsync(id, userId);
            await _audit.LogAsync(userId, "Silme", "Is Kayitlari", id,
                $"{{\"AracId\":{m.VehicleId},\"Tarih\":\"{m.WorkDate:yyyy-MM-dd}\"}}");
            TempData["Success"] = "Kayıt silindi.";
            return RedirectToAction(nameof(Index), new { vehicleId = m.VehicleId });
        }

    }
}
