using ICRent.Application.Security;
using ICRent.Domain.Entities;
using ICRent.Persistence.Context;
using ICRent.Persistence.Repositories.Users;
using ICRent.Web.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace ICRent.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _users;

        public AccountController(UserRepository users)
        {
            _users = users;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password)
        {
            var user = await _users.GetByUserNameAsync(userName);
            if (user == null || !PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Landing");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel, [FromServices] IDbConnectionFactory factory)
        {
            if (!ModelState.IsValid)
                return View(registerViewModel);

            var existing = await _users.GetByUserNameAsync(registerViewModel.UserName);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(registerViewModel.UserName), "Bu kullanıcı adı zaten alınmış.");
                return View();
            }

            var (hash, salt) = PasswordHasher.Hash(registerViewModel.Password);

            // manuel kayıt ekleme
            const string sql = """
                INSERT INTO dbo.Users(UserName, PasswordHash, PasswordSalt, Role)
                VALUES(@u, @h, @s, 'User');
            """;

            using var con = factory.Create();
            await con.OpenAsync();
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", registerViewModel.UserName);
            cmd.Parameters.AddWithValue("@h", hash);
            cmd.Parameters.AddWithValue("@s", salt);
            await cmd.ExecuteNonQueryAsync();

            ViewBag.RegisterSuccess = true;
            ViewBag.SuccessText = "Kayıt başarılı! Giriş yapabilirsiniz.";
            return View(new RegisterViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string? next = null)
        {
            await HttpContext.SignOutAsync();
            if (string.Equals(next, "login", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Login", "Account");

            return RedirectToAction("Index", "Landing");
        }

        public IActionResult Denied() => View();

        //#if DEBUG
        //        [HttpGet]
        //        public async Task<IActionResult> SetAdminPassword(string password = "Admin*123")
        //        {
        //            var admin = await _users.GetByUserNameAsync("admin");
        //            if (admin == null)
        //                return Content("Admin kullanıcısı bulunamadı.");

        //            var (hash, salt) = PasswordHasher.Hash(password);
        //            await _users.UpdatePasswordAsyn(admin.Id, hash, salt);
        //            return Content($"Admin şifresi '{password}' olarak ayarlandı.");
        //        }
        //#endif

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var currentName = User.Identity!.Name!;
            var user = await _users.GetByUserNameAsync(currentName);
            if (user == null) return NotFound();

            return View(new EditCredentialsViewModel { UserName = user.UserName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditCredentialsViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var currentName = User.Identity!.Name!;
            var user = await _users.GetByUserNameAsync(currentName);
            if (user == null) return NotFound();

            // mevcut şifre doğrula
            if (!PasswordHasher.Verify(vm.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Mevcut şifre hatalı.");
                return View(vm);
            }

            // kullanıcı adı benzersiz mi?
            if (!string.Equals(vm.UserName, user.UserName, StringComparison.OrdinalIgnoreCase) &&
                await _users.IsUserNameTakenAsync(vm.UserName, user.Id))
            {
                ModelState.AddModelError(nameof(vm.UserName), "Bu kullanıcı adı zaten kullanılıyor.");
                return View(vm);
            }

            // yeni şifre yalnızca girilmişse hashle
            byte[]? newHash = null, newSalt = null;
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var t = PasswordHasher.Hash(vm.NewPassword);
                newHash = t.hash;
                newSalt = t.salt;
            }

            // güncelle (repo tarafında hash/salt null ise eski şifreyi koru)
            await _users.UpdateCredentialsAsync(user.Id, vm.UserName, newHash, newSalt);

            // View'a başarı bayrağı gönder (redirect YOK)
            ViewBag.ProfileUpdated = true;
            ViewBag.SuccessText = "Bilgileriniz güncellendi. 2 saniye içinde giriş ekranına yönlendirileceksiniz.";

            // güvenlik/görsellik: şifre alanlarını temizle
            vm.CurrentPassword = vm.NewPassword = vm.ConfirmNewPassword = string.Empty;

            return View(vm);
        }

    }
}
