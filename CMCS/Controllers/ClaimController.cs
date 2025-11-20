using CMCS.Data;
using CMCS.Models;
using CMCS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ClaimController> _logger;

        public ClaimController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<ClaimController> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // GET: Claim/Create
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
            {
                TempData["Error"] = "Lecturer profile not found. Please contact HR.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Pre-populate with lecturer data (PART 3 REQUIREMENT)
            var model = new CreateClaimViewModel
            {
                HourlyRate = lecturer.HourlyRate, // Auto-populated from HR data
                Department = lecturer.Department,
                ClaimDate = DateTime.Today
            };

            // Pass lecturer info to view for display
            ViewBag.LecturerName = $"{lecturer.FirstName} {lecturer.LastName}";
            ViewBag.HourlyRate = lecturer.HourlyRate;

            return View(model);
        }

        // POST: Claim/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Create(CreateClaimViewModel model, List<IFormFile> documents)
        {
            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
            {
                TempData["Error"] = "Lecturer profile not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            // PART 3: Validation for hours worked (max 300 hours per month)
            var monthStart = new DateTime(model.ClaimDate.Year, model.ClaimDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var totalHoursThisMonth = await _context.Claims
                .Where(c => c.LecturerId == lecturer.LecturerId &&
                           c.ClaimDate >= monthStart &&
                           c.ClaimDate <= monthEnd &&
                           c.Status != ClaimStatus.Rejected)
                .SumAsync(c => c.HoursWorked);

            if (totalHoursThisMonth + model.HoursWorked > 300)
            {
                ModelState.AddModelError("HoursWorked",
                    $"Total hours for this month ({totalHoursThisMonth + model.HoursWorked}) exceeds the maximum of 300 hours. You have {300 - totalHoursThisMonth} hours remaining.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LecturerName = $"{lecturer.FirstName} {lecturer.LastName}";
                ViewBag.HourlyRate = lecturer.HourlyRate;
                return View(model);
            }

            try
            {
                // Create claim with lecturer's hourly rate (not manual input)
                var claim = new Claim
                {
                    ClaimDate = model.ClaimDate,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = lecturer.HourlyRate, // PART 3: Use HR-defined rate
                    Description = model.Description,
                    Department = model.Department,
                    Status = ClaimStatus.Pending,
                    LecturerId = lecturer.LecturerId,
                    CreatedDate = DateTime.Now
                };

                _context.Claims.Add(claim);
                await _context.SaveChangesAsync();

                // Handle document uploads
                if (documents != null && documents.Any())
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var file in documents)
                    {
                        if (file.Length > 0 && file.Length <= 10485760) // 10MB limit
                        {
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var document = new Document
                            {
                                FileName = file.FileName,
                                FilePath = filePath,
                                ContentType = file.ContentType,
                                FileSize = file.Length,
                                UploadedDate = DateTime.Now,
                                ClaimId = claim.ClaimId
                            };

                            _context.Documents.Add(document);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Claim submitted successfully! Total Amount: R{(claim.HoursWorked * claim.HourlyRate):F2}";
                return RedirectToAction("MyClaims");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                ModelState.AddModelError("", "An error occurred while submitting your claim.");
                ViewBag.LecturerName = $"{lecturer.FirstName} {lecturer.LastName}";
                ViewBag.HourlyRate = lecturer.HourlyRate;
                return View(model);
            }
        }

        // GET: Claim/MyClaims
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> MyClaims()
        {
            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
            {
                return NotFound();
            }

            var claims = await _context.Claims
                .Include(c => c.Documents)
                .Where(c => c.LecturerId == lecturer.LecturerId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(claims);
        }

        // GET: Claim/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Documents)
                .Include(c => c.ApprovedByCoordinator)
                .Include(c => c.ApprovedByManager)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            // Check authorization
            var user = await _userManager.GetUserAsync(User);
            var lecturer = await _context.Lecturer.FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (User.IsInRole("Lecturer") && lecturer?.LecturerId != claim.LecturerId)
            {
                return Forbid();
            }

            return View(claim);
        }

        // GET: Claim/Review/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Review(int id)
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Documents)
                .Include(c => c.ApprovedByCoordinator)
                .Include(c => c.ApprovedByManager)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // POST: Claim/ApproveByCoordinator
        [HttpPost]
        [Authorize(Roles = "ProgrammeCoordinator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveByCoordinator(int id, bool isApproved, string coordinatorNotes)
        {
            // Use session to track coordinator action
            HttpContext.Session.SetString("LastAction", $"Coordinator reviewed claim {id}");
            HttpContext.Session.SetString("LastActionTime", DateTime.Now.ToString());

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var coordinator = await _context.ProgrammeCoordinator
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (coordinator == null)
            {
                TempData["Error"] = "Coordinator profile not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (isApproved)
            {
                claim.Status = ClaimStatus.ApprovedByCoordinator;
                claim.ApprovedByCoordinatorId = coordinator.CoordinatorId;
                claim.CoordinatorApprovalDate = DateTime.Now;
                claim.CoordinatorNotes = coordinatorNotes;

                TempData["Success"] = "Claim approved and forwarded to Academic Manager.";
            }
            else
            {
                claim.Status = ClaimStatus.Rejected;
                claim.CoordinatorNotes = coordinatorNotes ?? "Rejected by coordinator";
                TempData["Success"] = "Claim rejected.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }

        // POST: Claim/ApproveByManager
        [HttpPost]
        [Authorize(Roles = "AcademicManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveByManager(int id, bool isApproved, string managerNotes, bool returnToCoordinator = false)
        {
            // Use session to track manager action
            HttpContext.Session.SetString("LastAction", $"Manager reviewed claim {id}");
            HttpContext.Session.SetString("LastActionTime", DateTime.Now.ToString());

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var manager = await _context.AcademicManager
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manager == null)
            {
                TempData["Error"] = "Manager profile not found.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (isApproved)
            {
                claim.Status = ClaimStatus.FullyApproved;
                claim.ApprovedByManagerId = manager.ManagerId;
                claim.ManagerApprovalDate = DateTime.Now;
                claim.ManagerNotes = managerNotes;

                TempData["Success"] = "Claim fully approved!";
            }
            else if (returnToCoordinator)
            {
                claim.Status = ClaimStatus.ReturnedToCoordinator;
                claim.ManagerNotes = managerNotes;
                TempData["Success"] = "Claim returned to coordinator for revision.";
            }
            else
            {
                claim.Status = ClaimStatus.Rejected;
                claim.ManagerNotes = managerNotes ?? "Rejected by manager";
                TempData["Success"] = "Claim rejected.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Dashboard");
        }

        // Download document
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null || !System.IO.File.Exists(document.FilePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(document.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, document.ContentType, document.FileName);
        }
    }
}