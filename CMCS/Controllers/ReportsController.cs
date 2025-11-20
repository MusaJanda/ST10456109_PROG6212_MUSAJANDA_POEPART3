using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPdfReportService pdfService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
            _logger = logger;
        }

        // GET: Reports/Index - Show available reports based on role
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UserRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            return View();
        }

        // GET: Reports/MyClaimsReport - Lecturer's personal report
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> MyClaimsReport()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var lecturer = await _context.Lecturer
                    .FirstOrDefaultAsync(l => l.UserId == user.Id);

                if (lecturer == null)
                {
                    TempData["Error"] = "Lecturer profile not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.LecturerId == lecturer.LecturerId)
                    .ToListAsync();

                if (!claims.Any())
                {
                    TempData["Error"] = "You have no claims to generate a report for.";
                    return RedirectToAction("MyClaims", "Claim");
                }

                var pdfBytes = _pdfService.GenerateClaimsReport(claims);
                return File(pdfBytes, "application/pdf", $"My_Claims_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lecturer report");
                TempData["Error"] = "An error occurred while generating your report.";
                return RedirectToAction("MyClaims", "Claim");
            }
        }

        // GET: Reports/CoordinatorReport - Claims reviewed by coordinator
        [Authorize(Roles = "ProgrammeCoordinator")]
        public async Task<IActionResult> CoordinatorReport()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var coordinator = await _context.ProgrammeCoordinator
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (coordinator == null)
                {
                    TempData["Error"] = "Coordinator profile not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                // Get all claims reviewed by this coordinator
                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Where(c => c.ApprovedByCoordinatorId == coordinator.CoordinatorId)
                    .ToListAsync();

                if (!claims.Any())
                {
                    TempData["Error"] = "No claims have been reviewed yet.";
                    return RedirectToAction("Index", "Dashboard");
                }

                var pdfBytes = _pdfService.GenerateClaimsReport(claims);
                return File(pdfBytes, "application/pdf", $"Coordinator_Report_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating coordinator report");
                TempData["Error"] = "An error occurred while generating the report.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Reports/ManagerReport - Claims approved by manager
        [Authorize(Roles = "AcademicManager")]
        public async Task<IActionResult> ManagerReport()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var manager = await _context.AcademicManager
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (manager == null)
                {
                    TempData["Error"] = "Manager profile not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                // Get all claims reviewed by this manager
                var claims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.ApprovedByCoordinator)
                    .Where(c => c.ApprovedByManagerId == manager.ManagerId)
                    .ToListAsync();

                if (!claims.Any())
                {
                    TempData["Error"] = "No claims have been reviewed yet.";
                    return RedirectToAction("Index", "Dashboard");
                }

                var pdfBytes = _pdfService.GenerateClaimsReport(claims);
                return File(pdfBytes, "application/pdf", $"Manager_Report_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating manager report");
                TempData["Error"] = "An error occurred while generating the report.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Reports/DownloadInvoice/5 - Download invoice for a specific claim
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.ClaimId == id);

                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("Index", "Dashboard");
                }

                // Check authorization - users can only download invoices for their claims
                // unless they are HR, Coordinator, or Manager
                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Lecturer"))
                {
                    var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);
                    if (lecturer == null || lecturer.LecturerId != claim.LecturerId)
                    {
                        TempData["Error"] = "You can only download invoices for your own claims.";
                        return RedirectToAction("MyClaims", "Claim");
                    }
                }

                var pdfBytes = _pdfService.GenerateInvoice(claim);
                return File(pdfBytes, "application/pdf", $"Invoice_{claim.ClaimId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading invoice");
                TempData["Error"] = "An error occurred while downloading the invoice.";
                return RedirectToAction("Index", "Dashboard");
            }
        }
    }
}