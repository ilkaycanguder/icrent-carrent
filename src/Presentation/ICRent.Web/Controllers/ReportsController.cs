using ICRent.Domain.Entities;
using ICRent.Persistence.Repositories.Abstractions;
using ICRent.Persistence.Repositories.Audits;
using ICRent.Persistence.Repositories.Users;
using ICRent.Persistence.Repositories.Vehicles;
using ICRent.Web.Models.Common;
using ICRent.Web.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ICRent.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ReportsController : Controller
    {
        private readonly IWorkLogQueryRepository _workLogs;
        private readonly VehicleRepository _vehicles;
        private readonly AuditRepository _audit;
        private readonly UserRepository _users;

        public ReportsController(VehicleRepository vehicles, IWorkLogQueryRepository workLogs, AuditRepository audit, UserRepository user)
        {
            _workLogs = workLogs;
            _vehicles = vehicles;
            _audit = audit;
            _users = user;
        }

        [HttpGet]
        public IActionResult WeeklyReport()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // fetch ile token göndermiyorsan bunu EKLE
        public async Task<IActionResult> WeeklyReportData([FromForm] int[]? vehicleIds)
        {
            // Haftanın Pazartesi başlangıcı (start dahil, end hariç)
            var today = DateOnly.FromDateTime(DateTime.Now);
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekEnd = weekStart.AddDays(7);

            // Araçlar ve filtre
            var allVehicles = await _vehicles.GetAllAsync();
            var ids = (vehicleIds != null && vehicleIds.Length > 0)
                        ? vehicleIds
                        : allVehicles.Select(v => v.Id).ToArray();

            // Ham kayıtlar
            var rows = await _workLogs.GetRangeWithUserAsync(ids, weekStart, weekEnd);

            // 168 saat bazlı özet
            const decimal BASE_WEEK = 168m;

            var result = allVehicles
                .Where(v => ids.Contains(v.Id))
                .Select(v =>
                {
                    var g = rows.Where(r => r.VehicleId == v.Id);
                    var active = g.Sum(x => x.Active);
                    var maint = g.Sum(x => x.Maint);
                    var idle = BASE_WEEK - (active + maint);
                    if (idle < 0) idle = 0;

                    return new
                    {
                        // VIEW'in beklediği İSİMLER
                        name = v.Name,
                        plate = v.Plate,
                        active = active,
                        maintenance = maint,
                        idle = idle,
                        activePct = active * 100m / BASE_WEEK,
                        idlePct = idle * 100m / BASE_WEEK
                    };
                })
                .OrderBy(x => x.name)
                .ToList();

            return Json(result);
        }

        // Audit ve diğer aksiyonların aynen kalabilir:
        [HttpGet]
        public async Task<IActionResult> Audit(int page = 1, int pageSize = 20)
        {
            var list = await _audit.GetLatestAsync(100);
            var vehicles = await _vehicles.GetAllAsync();


            var model = new List<AuditViewModel>();

            foreach (var item in list)
            {
                string entityLabel = item.EntityId.ToString();
                string pretty = item.Details ?? "-";

                int? vehicleId = null;
                DateTime? day = null;
                decimal? aktif = null, bakim = null;

                if (!string.IsNullOrWhiteSpace(item.Details))
                {
                    using var doc = JsonDocument.Parse(item.Details);
                    var r = doc.RootElement;

                    if (r.TryGetProperty("AracId", out var pa)) vehicleId = pa.GetInt32();
                    else if (r.TryGetProperty("VehicleId", out var pv)) vehicleId = pv.GetInt32();

                    if (r.TryGetProperty("Tarih", out var pt) && DateTime.TryParse(pt.GetString(), out var d1))
                        day = d1;
                    else if (r.TryGetProperty("WorkDate", out var pw) && DateTime.TryParse(pw.GetString(), out var d2))
                        day = d2;

                    if (r.TryGetProperty("AktifSaat", out var pa2)) aktif = pa2.GetDecimal();
                    else if (r.TryGetProperty("ActiveHours", out var pa3)) aktif = pa3.GetDecimal();

                    if (r.TryGetProperty("BakimSaat", out var pb1)) bakim = pb1.GetDecimal();
                    else if (r.TryGetProperty("MaintenanceHours", out var pb2)) bakim = pb2.GetDecimal();
                }

                var vehicleName = vehicleId.HasValue
                    ? vehicles.FirstOrDefault(v => v.Id == vehicleId.Value)?.Name ?? $"Araç #{vehicleId}"
                    : "-";
                var vehiclePlate = vehicleId.HasValue
                    ? vehicles.FirstOrDefault(v => v.Id == vehicleId.Value)?.Plate ?? ""
                    : "-";

                var vehicleText = $"{vehicleName} ({vehiclePlate})";

                entityLabel = day != null ? $"{vehicleText} - {day:dd.MM.yyyy}" : vehicleText;

                var byUser = item.User;

                pretty = $"Araç: {vehicleText}";
                if (day != null) pretty += $", Tarih: {day:dd.MM.yyyy}";
                if (aktif != null) pretty += $", Aktif: {aktif:0.#} saat";
                if (bakim != null) pretty += $", Bakım: {bakim:0.#} saat";

                pretty += $" • Girişi yapan: {byUser}";

                if (item.Entity == "Araclar")
                {
                    // Varsayılanlar
                    string ad = null, plaka = null, yeniAd = null, yeniPlaka = null;

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.Details))
                        {
                            using var doc = JsonDocument.Parse(item.Details);
                            var r = doc.RootElement;

                            // Ekleme / Silme şeması
                            if (r.TryGetProperty("AracAdi", out var pAd)) ad = pAd.GetString();
                            if (r.TryGetProperty("Plaka", out var pPlk)) plaka = pPlk.GetString();

                            // Güncelleme şeması
                            if (r.TryGetProperty("EskiAd", out var pOldN)) ad = pOldN.GetString();
                            if (r.TryGetProperty("EskiPlaka", out var pOldP)) plaka = pOldP.GetString();
                            if (r.TryGetProperty("YeniAd", out var pNewN)) yeniAd = pNewN.GetString();
                            if (r.TryGetProperty("YeniPlaka", out var pNewP)) yeniPlaka = pNewP.GetString();
                        }
                    }
                    catch { /* bozuk JSON'u ham bırakırız */ }

                    // Etiket (Id kolonu yerine)
                    if (!string.IsNullOrWhiteSpace(yeniAd) || !string.IsNullOrWhiteSpace(yeniPlaka))
                        entityLabel = $"{(yeniAd ?? ad) ?? "-"} ({(yeniPlaka ?? plaka) ?? "-"})";
                    else
                        entityLabel = $"{(ad ?? "-")} ({(plaka ?? "-")})";

                    // Okunaklı detay – işleme göre
                    if (item.Action == "Ekleme")
                    {
                        pretty = $"Araç eklendi: {entityLabel}";
                    }
                    else if (item.Action == "Silme")
                    {
                        pretty = $"Araç silindi: {entityLabel}";
                    }
                    else if (item.Action == "Güncelleme")
                    {
                        var fromText = $"{ad ?? "-"} ({plaka ?? "-"})";
                        var toText = $"{(yeniAd ?? ad) ?? "-"} ({(yeniPlaka ?? plaka) ?? "-"})";
                        pretty = $"Araç güncellendi: {fromText} → {toText}";
                    }
                }

                model.Add(new AuditViewModel
                {
                    Islem = item.Action,
                    Varlik = item.Entity,
                    EntityLabel = entityLabel,
                    Detay = pretty,
                    Tarih = item.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                });
            }
            var total = model.Count;
            var pageItems = model
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var paged = new PagedResult<AuditViewModel>
            {
                Items = pageItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };


            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> Gantt()
        {
            // Varsayılan: bu haftanın pazartesi – pazar aralığı
            var today = DateOnly.FromDateTime(DateTime.Now);
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekEndExcl = weekStart.AddDays(7);         // [start, end)
            var weekEndIncl = weekEndExcl.AddDays(-1);      // ekranda göstermek için dahil gün

            ViewBag.Vehicles = await _vehicles.GetAllAsync();
            ViewBag.WeekStart = weekStart.ToString("yyyy-MM-dd");
            ViewBag.WeekEnd = weekEndIncl.ToString("yyyy-MM-dd");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GanttData(int[] vehicleIds, DateOnly start, DateOnly end)
        {
            // formdaki "end" dahil geldiği için sorguya exclusive verelim:
            var endExclusive = end.AddDays(1);

            var rows = await _workLogs.GetRangeWithUserAsync(vehicleIds, start, endExclusive);

            // Aktif barları (istersen bakım için de ikinci bir set üretebilirsin)
            var data = rows.Where(x => x.Active > 0).Select(x => new
            {
                Vehicle = $"{x.Vehicle} ({x.Plate})",
                User = x.CreatedBy,
                Start = x.Date,
                End = x.Date.AddHours((double)x.Active)
            });

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> WorkLogHistory(int? vehicleId, DateOnly? start, DateOnly? end)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var s = start ?? today.AddDays(-7);
            var e = (end ?? today).AddDays(1); // bitiş dahil

            ViewBag.Vehicles = await _vehicles.GetAllAsync();
            ViewBag.VehicleId = vehicleId;
            ViewBag.Start = s; ViewBag.End = e.AddDays(-1);

            var list = await _audit.GetWorkLogHistoryAsync(vehicleId, s, e, top: 200);
            return View(list);
        }



    }
}
