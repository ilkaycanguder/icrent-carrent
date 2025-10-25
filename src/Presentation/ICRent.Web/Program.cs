using ICRent.Persistence.Context;
using ICRent.Persistence.Repositories.Abstractions;
using ICRent.Persistence.Repositories.Audits;
using ICRent.Persistence.Repositories.Users;
using ICRent.Persistence.Repositories.Vehicles;
using ICRent.Persistence.Repositories.WorkLogs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace ICRent.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

      
            // Persistence
            builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
            builder.Services.AddScoped<VehicleRepository>();
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<AuditRepository>();
            builder.Services.AddScoped<IWorkLogCommandRepository, WorkLogCommandRepository>();
            builder.Services.AddScoped<IWorkLogQueryRepository, WorkLogQueryRepository>();


            // Auth / Authorization
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
              .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
              {
                  o.LoginPath = "/Account/Login";
                  o.AccessDeniedPath = "/Account/Denied";
              });

            builder.Services.AddAuthorization(o =>
            {
                o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                o.AddPolicy("UserOnly", p => p.RequireRole("User"));
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Landing}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
