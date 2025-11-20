using CMCS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;
using CMCS.Services;

namespace CMCS
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddControllersWithViews();

            // PART 3: Add Session services (REQUIRED)
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddScoped<IPdfReportService, PdfReportService>();

            // Add logging
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Enable Session must be before UseAuthorization
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            // Seed data with proper error handling
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    logger.LogInformation("Starting database seeding...");
                    await SeedData(services);
                    logger.LogInformation("Database seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Dashboard}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        private static async Task SeedData(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // PART 3: Ensure all necessary roles exist (INCLUDING HR)
            string[] roleNames = { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "Admin", "HR" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"✓ Role '{roleName}' created successfully.");
                    }
                    else
                    {
                        logger.LogError($"✗ Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    logger.LogInformation($"✓ Role '{roleName}' already exists.");
                }
            }

            // Create default admin user if none exists
            string adminEmail = "admin@cmcs.com";
            string adminPassword = "Admin@123";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "System",
                    Surname = "Administrator",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(newAdmin, adminPassword);
                if (createResult.Succeeded)
                {
                    logger.LogInformation($"✓ Admin user '{adminEmail}' created successfully.");
                    var roleResult = await userManager.AddToRoleAsync(newAdmin, "Admin");
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation($"✓ Admin role assigned to '{adminEmail}'.");
                    }
                }
            }

            // PART 3: Create default HR user
            string hrEmail = "hr@cmcs.com";
            string hrPassword = "Hr@123";
            var hrUser = await userManager.FindByEmailAsync(hrEmail);

            if (hrUser == null)
            {
                var newHR = new ApplicationUser
                {
                    UserName = hrEmail,
                    Email = hrEmail,
                    Name = "HR",
                    Surname = "Department",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(newHR, hrPassword);
                if (createResult.Succeeded)
                {
                    logger.LogInformation($"✓ HR user '{hrEmail}' created successfully.");
                    var roleResult = await userManager.AddToRoleAsync(newHR, "HR");
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation($"✓ HR role assigned to '{hrEmail}'.");
                    }
                }
                else
                {
                    logger.LogError($"✗ HR creation failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"✓ HR user '{hrEmail}' already exists.");
            }
        }
    }
}