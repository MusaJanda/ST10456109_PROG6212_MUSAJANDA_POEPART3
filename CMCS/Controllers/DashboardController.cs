using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using CMCS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;
        private readonly IPdfReportService _pdfReportService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger,
            IPdfReportService pdfReportService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _pdfReportService = pdfReportService;
        }

        public async Task<IActionResult> Index()
        {
            // PART 3: Session management
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.GetUserAsync(User);
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
            }

            // Route based on role
            if (User.IsInRole("HR"))
            {
                return await HRDashboard();
            }
            else if (User.IsInRole("Lecturer"))
            {
                return await LecturerDashboard();
            }
            else if (User.IsInRole("ProgrammeCoordinator"))
            {
                return await ProgrammeCoordinatorDashboard();
            }
            else if (User.IsInRole("AcademicManager"))
            {
                return await AcademicManagerDashboard();
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return View("Error");
        }

        private async Task<IActionResult> LecturerDashboard()
        {
            // Ensure user has access
            if (!User.IsInRole("Lecturer"))
            {
                return View("Unauthorized");
            }

            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturer
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
            {
                TempData["Error"] = "Lecturer profile not found. Please contact HR.";
                return View("Error");
            }

            // Get recent claims
            var claims = await _context.Claims
                .Where(c => c.LecturerId == lecturer.LecturerId)
                .OrderByDescending(c => c.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewData["User"] = lecturer;
            ViewData["Claims"] = claims;

            return View("LecturerDashboard");
        }

        private async Task<IActionResult> ProgrammeCoordinatorDashboard()
        {
            // PART 3: Ensure users cannot access pages they shouldn't
            if (!User.IsInRole("ProgrammeCoordinator"))
            {
                return View("Unauthorized");
            }

            // Session tracking
            HttpContext.Session.SetString("DashboardView", "Coordinator");
            HttpContext.Session.SetString("LastAccess", DateTime.Now.ToString());

            var pendingClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Documents)
                .Where(c => c.Status == ClaimStatus.Pending ||
                           c.Status == ClaimStatus.ReturnedToCoordinator)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            return View("ProgrammeCoordinatorDashboard", pendingClaims);
        }

        private async Task<IActionResult> AcademicManagerDashboard()
        {
            // PART 3: Ensure users cannot access pages they shouldn't
            if (!User.IsInRole("AcademicManager"))
            {
                return View("Unauthorized");
            }

            // Session tracking
            HttpContext.Session.SetString("DashboardView", "Manager");
            HttpContext.Session.SetString("LastAccess", DateTime.Now.ToString());

            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Documents)
                .Include(c => c.ApprovedByCoordinator)
                .Where(c => c.Status == ClaimStatus.ApprovedByCoordinator)
                .OrderBy(c => c.CoordinatorApprovalDate)
                .ToListAsync();

            return View("AcademicManagerDashboard", approvedClaims);
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRDashboard()
        {
            var users = await GetAllUsersAsync();
            return View("Index", users);
        }

        [Authorize(Roles = "HR")]
        public IActionResult CreateUser()
        {
            var model = new CreateUserViewModel
            {
                Roles = new List<string> { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "HR" }
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            Console.WriteLine("=== CREATE USER PROCESS STARTED ===");
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState errors:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($" - {error.ErrorMessage}");
                }

                model.Roles = new List<string> { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "HR" };
                return View(model);
            }

            try
            {
                Console.WriteLine("Creating ApplicationUser...");

                // Create ApplicationUser first
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                    // Removed CreatedDate to avoid potential database issues
                };

                Console.WriteLine($"Attempting to create user: {model.Email}");
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    Console.WriteLine("User created successfully, adding role...");

                    // Add role to user
                    await _userManager.AddToRoleAsync(user, model.Role);
                    Console.WriteLine($"Role {model.Role} added successfully");

                    // Create specific role profile
                    switch (model.Role)
                    {
                        case "Lecturer":
                            Console.WriteLine("Creating Lecturer profile...");
                            var lecturer = new Lecturer
                            {
                                UserId = user.Id,
                                FirstName = model.FirstName,
                                LastName = model.LastName,
                                Email = model.Email,
                                PhoneNumber = model.PhoneNumber,
                                Department = model.Department,
                                HourlyRate = model.HourlyRate ?? 0,
                                IsActive = true,
                                CreatedDate = DateTime.Now
                            };
                            _context.Lecturer.Add(lecturer);
                            break;

                        case "ProgrammeCoordinator":
                            Console.WriteLine("Creating ProgrammeCoordinator profile...");
                            var coordinator = new ProgrammeCoordinator
                            {
                                UserId = user.Id,
                                FirstName = model.FirstName,
                                LastName = model.LastName,
                                Email = model.Email,
                                PhoneNumber = model.PhoneNumber,
                                Department = model.Department,
                                IsActive = true,
                                CreatedDate = DateTime.Now
                            };
                            _context.ProgrammeCoordinator.Add(coordinator);
                            break;

                        case "AcademicManager":
                            Console.WriteLine("Creating AcademicManager profile...");
                            var manager = new AcademicManager
                            {
                                UserId = user.Id,
                                FirstName = model.FirstName,
                                LastName = model.LastName,
                                Email = model.Email,
                                PhoneNumber = model.PhoneNumber,
                                Department = model.Department,
                                IsActive = true,
                                CreatedDate = DateTime.Now
                            };
                            _context.AcademicManager.Add(manager);
                            break;

                        case "HR":
                            Console.WriteLine("HR user - no additional profile needed");
                            break;
                    }

                    Console.WriteLine("Saving changes to database...");
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Database changes saved successfully!");

                    TempData["Success"] = $"User {model.FirstName} {model.LastName} created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine("User creation failed with errors:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($" - {error.Description}");
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");

                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "An error occurred while creating the user.");
            }

            model.Roles = new List<string> { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "HR" };
            return View(model);
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> EditUser(int? lecturerId, int? coordinatorId, int? managerId)
        {
            if (lecturerId.HasValue)
            {
                var lecturer = await _context.Lecturer.FindAsync(lecturerId.Value);
                if (lecturer != null)
                {
                    var model = new EditUserViewModel
                    {
                        Id = lecturer.LecturerId,
                        UserType = "Lecturer",
                        FirstName = lecturer.FirstName,
                        LastName = lecturer.LastName,
                        Email = lecturer.Email,
                        PhoneNumber = lecturer.PhoneNumber,
                        Department = lecturer.Department,
                        HourlyRate = lecturer.HourlyRate,
                        IsActive = lecturer.IsActive,
                        CreatedDate = lecturer.CreatedDate
                    };
                    return View(model);
                }
            }

            if (coordinatorId.HasValue)
            {
                var coordinator = await _context.ProgrammeCoordinator.FindAsync(coordinatorId.Value);
                if (coordinator != null)
                {
                    var model = new EditUserViewModel
                    {
                        Id = coordinator.CoordinatorId,
                        UserType = "ProgrammeCoordinator",
                        FirstName = coordinator.FirstName,
                        LastName = coordinator.LastName,
                        Email = coordinator.Email,
                        PhoneNumber = coordinator.PhoneNumber,
                        Department = coordinator.Department,
                        IsActive = coordinator.IsActive,
                        CreatedDate = coordinator.CreatedDate
                    };
                    return View(model);
                }
            }

            if (managerId.HasValue)
            {
                var manager = await _context.AcademicManager.FindAsync(managerId.Value);
                if (manager != null)
                {
                    var model = new EditUserViewModel
                    {
                        Id = manager.ManagerId,
                        UserType = "AcademicManager",
                        FirstName = manager.FirstName,
                        LastName = manager.LastName,
                        Email = manager.Email,
                        PhoneNumber = manager.PhoneNumber,
                        Department = manager.Department,
                        IsActive = manager.IsActive,
                        CreatedDate = manager.CreatedDate
                    };
                    return View(model);
                }
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                switch (model.UserType)
                {
                    case "Lecturer":
                        var lecturer = await _context.Lecturer.FindAsync(model.Id);
                        if (lecturer != null)
                        {
                            lecturer.FirstName = model.FirstName;
                            lecturer.LastName = model.LastName;
                            lecturer.Email = model.Email;
                            lecturer.PhoneNumber = model.PhoneNumber;
                            lecturer.Department = model.Department;
                            lecturer.HourlyRate = model.HourlyRate;
                            lecturer.IsActive = model.IsActive;
                        }
                        break;

                    case "ProgrammeCoordinator":
                        var coordinator = await _context.ProgrammeCoordinator.FindAsync(model.Id);
                        if (coordinator != null)
                        {
                            coordinator.FirstName = model.FirstName;
                            coordinator.LastName = model.LastName;
                            coordinator.Email = model.Email;
                            coordinator.PhoneNumber = model.PhoneNumber;
                            coordinator.Department = model.Department;
                            coordinator.IsActive = model.IsActive;
                        }
                        break;

                    case "AcademicManager":
                        var manager = await _context.AcademicManager.FindAsync(model.Id);
                        if (manager != null)
                        {
                            manager.FirstName = model.FirstName;
                            manager.LastName = model.LastName;
                            manager.Email = model.Email; // FIXED: was manager.Email = manager.Email;
                            manager.PhoneNumber = model.PhoneNumber;
                            manager.Department = model.Department;
                            manager.IsActive = model.IsActive;
                        }   
                        break;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ModelState.AddModelError("", "An error occurred while updating the user.");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Reports(string reportType = "summary")
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new ReportViewModel
            {
                ReportType = reportType,
                UserRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };

            // Generate report data based on user role
            if (User.IsInRole("Lecturer"))
            {
                var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer != null)
                {
                    model.Claims = await _context.Claims
                        .Include(c => c.ApprovedByCoordinator)
                        .Include(c => c.ApprovedByManager)
                        .Where(c => c.LecturerId == lecturer.LecturerId)
                        .OrderByDescending(c => c.CreatedDate)
                        .ToListAsync();
                }
            }
            else if (User.IsInRole("ProgrammeCoordinator"))
            {
                model.Claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.Documents)
                    .Where(c => c.Status == ClaimStatus.Pending || c.Status == ClaimStatus.ReturnedToCoordinator)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToListAsync();
            }
            else if (User.IsInRole("AcademicManager"))
            {
                model.Claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ApprovedByCoordinator)
                    .Include(c => c.Documents)
                    .Where(c => c.Status == ClaimStatus.ApprovedByCoordinator)
                    .OrderByDescending(c => c.CoordinatorApprovalDate)
                    .ToListAsync();
            }
            else if (User.IsInRole("HR"))
            {
                model.Users = await GetAllUsersAsync();
                model.Claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ApprovedByCoordinator)
                    .Include(c => c.ApprovedByManager)
                    .Where(c => c.Status == ClaimStatus.FullyApproved)
                    .OrderByDescending(c => c.ManagerApprovalDate)
                    .ToListAsync();
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> DownloadReport(string reportType, string format = "pdf")
        {
            try
            {
                byte[] reportBytes;
                string fileName;
                string contentType;

                var user = await _userManager.GetUserAsync(User);

                if (format.ToLower() == "excel")
                {
                    // Excel generation would go here using ClosedXML
                    reportBytes = GenerateExcelReport(reportType, user);
                    fileName = $"{reportType}_report_{DateTime.Now:yyyyMMdd}.xlsx";
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                }
                else
                {
                    // PDF generation
                    if (User.IsInRole("HR"))
                    {
                        switch (reportType.ToLower())
                        {
                            case "users":
                                var users = await GetAllUsersAsync();
                                reportBytes = _pdfReportService.GenerateUserReport(users);
                                fileName = $"User_Report_{DateTime.Now:yyyyMMdd}.pdf";
                                break;

                            case "payroll":
                                var payrollClaims = await _context.Claims
                                    .Include(c => c.Lecturer)
                                    .Where(c => c.Status == ClaimStatus.FullyApproved)
                                    .ToListAsync();
                                reportBytes = _pdfReportService.GeneratePayrollReport(payrollClaims);
                                fileName = $"Payroll_Report_{DateTime.Now:yyyyMMdd}.pdf";
                                break;

                            case "financial":
                                var financialClaims = await _context.Claims
                                    .Include(c => c.Lecturer)
                                    .ToListAsync();
                                reportBytes = _pdfReportService.GenerateFinancialReport(financialClaims);
                                fileName = $"Financial_Report_{DateTime.Now:yyyyMMdd}.pdf";
                                break;

                            default:
                                TempData["Error"] = "Invalid report type";
                                return RedirectToAction("Reports");
                        }
                    }
                    else if (User.IsInRole("Lecturer"))
                    {
                        var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);
                        if (lecturer != null)
                        {
                            var claims = await _context.Claims
                                .Where(c => c.LecturerId == lecturer.LecturerId)
                                .ToListAsync();
                            reportBytes = _pdfReportService.GenerateClaimsReport(claims);
                            fileName = $"My_Claims_Report_{DateTime.Now:yyyyMMdd}.pdf";
                        }
                        else
                        {
                            TempData["Error"] = "Lecturer profile not found";
                            return RedirectToAction("Reports");
                        }
                    }
                    else
                    {
                        // For coordinators and managers
                        var claims = await _context.Claims
                            .Include(c => c.Lecturer)
                            .ToListAsync();
                        reportBytes = _pdfReportService.GenerateClaimsReport(claims);
                        fileName = $"Claims_Report_{DateTime.Now:yyyyMMdd}.pdf";
                    }
                    contentType = "application/pdf";
                }

                return File(reportBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                TempData["Error"] = "Error generating report";
                return RedirectToAction("Reports");
            }
        }

        private byte[] GenerateExcelReport(string reportType, ApplicationUser user)
        {
            // This is a simplified version - you'd implement full Excel generation with ClosedXML
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");

            worksheet.Cell(1, 1).Value = $"{reportType} Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Value = $"Generated by: {user.Email}";
            worksheet.Cell(3, 1).Value = $"Date: {DateTime.Now:yyyy-MM-dd}";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Helper method to get all users
        private async Task<List<UserListViewModel>> GetAllUsersAsync()
        {
            var lecturers = await _context.Lecturer
                .Select(l => new UserListViewModel
                {
                    FirstName = l.FirstName,
                    LastName = l.LastName,
                    Email = l.Email,
                    PhoneNumber = l.PhoneNumber,
                    Department = l.Department,
                    HourlyRate = l.HourlyRate,
                    Role = "Lecturer",
                    CreatedDate = l.CreatedDate,
                    LecturerId = l.LecturerId,
                    IsActive = l.IsActive
                })
                .ToListAsync();

            var coordinators = await _context.ProgrammeCoordinator
                .Select(c => new UserListViewModel
                {
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    Department = c.Department,
                    HourlyRate = 0,
                    Role = "ProgrammeCoordinator",
                    CreatedDate = c.CreatedDate,
                    CoordinatorId = c.CoordinatorId, // FIXED: was ProgrammeCoordinatorId
                    IsActive = c.IsActive
                })
                .ToListAsync();

            var managers = await _context.AcademicManager
                .Select(m => new UserListViewModel
                {
                    FirstName = m.FirstName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    Department = m.Department,
                    HourlyRate = 0,
                    Role = "AcademicManager",
                    CreatedDate = m.CreatedDate,
                    ManagerId = m.ManagerId, // FIXED: was AcademicManagerId
                    IsActive = m.IsActive
                })
                .ToListAsync();

            return lecturers.Concat(coordinators).Concat(managers).ToList();
        }

        // HR Claims Management
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRClaims()
        {
            var approvedClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.ApprovedByCoordinator)
                .Include(c => c.ApprovedByManager)
                .Where(c => c.Status == ClaimStatus.FullyApproved)
                .OrderByDescending(c => c.ManagerApprovalDate)
                .ToListAsync();

            var paidClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.PaymentStatus == PaymentStatus.Paid)
                .OrderByDescending(c => c.PaymentDate)
                .Take(10)
                .ToListAsync();

            var model = new HRClaimsViewModel
            {
                ApprovedClaims = approvedClaims,
                PaidClaims = paidClaims
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            claim.PaymentStatus = PaymentStatus.Paid;
            claim.PaymentDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Claim #{claim.ClaimId} for {claim.Lecturer.FirstName} {claim.Lecturer.LastName} marked as paid! Amount: R{claim.TotalAmount:F2}";
            return RedirectToAction("HRClaims");
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsProcessing(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.PaymentStatus = PaymentStatus.Processing;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Claim #{claim.ClaimId} marked as processing.";
            return RedirectToAction("HRClaims");
        }
    }
}