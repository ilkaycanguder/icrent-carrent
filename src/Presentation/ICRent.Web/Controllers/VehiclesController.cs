using ICRent.Domain.Entities;
using ICRent.Persistence.Repositories.Audits;
using ICRent.Persistence.Repositories.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ICRent.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class VehiclesController : Controller
    {
        private readonly VehicleRepository _vehicles;
        private readonly AuditRepository _audit;
        public VehiclesController(VehicleRepository vehicles, AuditRepository audit)
        {
            _vehicles = vehicles;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _vehicles.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, string plate)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(plate))
            {
                ViewBag.Error = "Tüm alanlar zorunludur.";
                return View();
            }

            // 1) Ön kontrol
            if (await _vehicles.ExistsByNameAsync(name))
            {
                ViewBag.Error = $"\"{name}\" adına sahip bir araç zaten mevcut.";
                return View();
            }
            if (await _vehicles.ExistsByPlateAsync(plate))
            {
                ViewBag.Error = $"\"{plate}\" plakalı bir araç zaten mevcut.";
                return View();
            }

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var id = await _vehicles.CreateAsync(name, plate, userId);

                await _audit.LogAsync(userId, "Ekleme", "Araclar", id,
                  $"{{\"AracAdi\":\"{name}\",\"Plaka\":\"{plate}\"}}");


                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex) // repo'dan gelen anlamlı hata
            {
                ViewBag.Error = ex.Message; // "Araç adı veya plaka zaten kayıtlı."
                return View();
            }
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var values = await _vehicles.GetByIdAsync(id);
            if (values == null)
            {
                return NotFound();
            }
            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string plate)
        {
            var old = await _vehicles.GetByIdAsync(id);
            if (old is null) return NotFound();

            if (string.Equals(old.Name, name, StringComparison.Ordinal) &&
                string.Equals(old.Plate, plate, StringComparison.Ordinal))
            {
                TempData["Info"] = "Herhangi bir değişiklik yapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            // 1) Ön kontroller (alan bazlı hata)
            if (await _vehicles.ExistsByNameAsync(name, excludeId: id))
                ModelState.AddModelError("Name", $"\"{name}\" adına sahip başka bir araç zaten mevcut.");

            if (await _vehicles.ExistsByPlateAsync(plate, excludeId: id))
                ModelState.AddModelError("Plate", $"\"{plate}\" plakalı başka bir araç zaten mevcut.");

            if (!ModelState.IsValid)
                return View(new Vehicle { Id = id, Name = name, Plate = plate });

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                await _vehicles.UpdateAsync(id, name, plate, userId);

                // 🔹 Audit: Güncelleme kaydı
                var payload = JsonSerializer.Serialize(new
                {
                    AracId = id,
                    EskiAd = old.Name,
                    EskiPlaka = old.Plate,
                    YeniAd = name,
                    YeniPlaka = plate
                });
                await _audit.LogAsync(userId, "Güncelleme", "Araclar", id, payload);

                TempData["Success"] = "Araç başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(new Vehicle { Id = id, Name = name, Plate = plate });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var v = await _vehicles.GetByIdAsync(id);

            await _vehicles.DeleteAsync(id);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _audit.LogAsync(userId, "Silme", "Araclar", id,
                v is null ? $"{{\"VehicleId\":{id}}}"
                          : $"{{\"AracId\":{id},\"AracAdi\":\"{v.Name}\",\"Plaka\":\"{v.Plate}\"}}");

            TempData["Success"] = "Araç başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
