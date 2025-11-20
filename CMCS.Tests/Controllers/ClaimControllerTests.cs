using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Controllers;
using CMCS.Data;
using CMCS.Models;
using CMCS.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace CMCS.Tests
{
    public class ClaimControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<ILogger<ClaimController>> _mockLogger;
        private readonly ApplicationDbContext _context;
        private readonly ClaimController _controller;

        public ClaimControllerTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Test_Database_" + System.Guid.NewGuid())
                .Options;
            _context = new ApplicationDbContext(options);

            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            // Mock other dependencies
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<ClaimController>>();

            _controller = new ClaimController(_context, _mockUserManager.Object, _mockEnvironment.Object, _mockLogger.Object);

            SetupAuthenticatedUser();
        }

        [Fact]
        public async Task Create_Get_ReturnsViewResult()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "test-user-id",
                FirstName = "Test",
                LastName = "Lecturer",
                HourlyRate = 250.00m,
                Department = "Computer Science"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.Lecturer.Add(lecturer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateClaimViewModel>(viewResult.Model);
            Assert.Equal(250.00m, model.HourlyRate);
            Assert.Equal("Computer Science", model.Department);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_RedirectsToMyClaims()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "test-user-id",
                FirstName = "Test",
                LastName = "Lecturer",
                HourlyRate = 250.00m,
                Department = "Computer Science"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.Lecturer.Add(lecturer);
            await _context.SaveChangesAsync();

            var model = new CreateClaimViewModel
            {
                ClaimDate = System.DateTime.Today,
                HoursWorked = 10,
                Description = "Test claim",
                Department = "Computer Science"
            };

            _mockEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");

            // Act
            var result = await _controller.Create(model, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyClaims", redirectResult.ActionName);
            Assert.True(_context.Claims.Any()); // Verify claim was created
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "test-user-id",
                FirstName = "Test",
                LastName = "Lecturer",
                HourlyRate = 250.00m,
                Department = "Computer Science"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.Lecturer.Add(lecturer);
            await _context.SaveChangesAsync();

            var model = new CreateClaimViewModel
            {
                HoursWorked = -1, // Invalid hours
                Description = "Test claim",
                Department = "Computer Science"
            };
            _controller.ModelState.AddModelError("HoursWorked", "Hours must be positive");

            // Act
            var result = await _controller.Create(model, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<CreateClaimViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Create_Post_WithoutLecturerRecord_RedirectsToDashboard()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            // Don't add lecturer to database - simulating missing lecturer record

            var model = new CreateClaimViewModel
            {
                ClaimDate = System.DateTime.Today,
                HoursWorked = 10,
                Description = "Test claim",
                Department = "Computer Science"
            };

            // Act
            var result = await _controller.Create(model, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Dashboard", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Create_Post_ExceedsMonthlyHours_ReturnsViewWithError()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "test-user-id",
                FirstName = "Test",
                LastName = "Lecturer",
                HourlyRate = 250.00m,
                Department = "Computer Science"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.Lecturer.Add(lecturer);

            // Add existing claims for the current month totaling 175 hours
            var existingClaim = new Claim
            {
                ClaimId = 1,
                LecturerId = 1,
                ClaimDate = System.DateTime.Today,
                HoursWorked = 175,
                HourlyRate = 250.00m,
                Description = "Existing claim",
                Department = "Computer Science",
                Status = ClaimStatus.Pending,
                CreatedDate = System.DateTime.Now
            };
            _context.Claims.Add(existingClaim);
            await _context.SaveChangesAsync();

            var model = new CreateClaimViewModel
            {
                ClaimDate = System.DateTime.Today,
                HoursWorked = 10, // This would make total 185 hours, exceeding 180 limit
                Description = "Test claim",
                Department = "Computer Science"
            };

            // Act
            var result = await _controller.Create(model, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values.SelectMany(v => v.Errors),
                e => e.ErrorMessage.Contains("exceeds the maximum of 180 hours"));
        }

        [Fact]
        public async Task MyClaims_ReturnsViewWithClaims()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "test-user-id",
                FirstName = "Test",
                LastName = "Lecturer"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.Lecturer.Add(lecturer);

            var claim = new Claim
            {
                ClaimId = 1,
                LecturerId = 1,
                ClaimDate = System.DateTime.Now,
                HoursWorked = 10,
                HourlyRate = 250.00m,
                Description = "Test claim",
                Department = "Computer Science",
                Status = ClaimStatus.Pending,
                CreatedDate = System.DateTime.Now
            };
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.MyClaims();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Claim>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "test-user-id" };
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "test-user-id",
                FirstName = "Test",
                LastName = "Lecturer"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.Lecturer.Add(lecturer);

            var claim = new Claim
            {
                ClaimId = 1,
                ClaimDate = System.DateTime.Now,
                HoursWorked = 10,
                HourlyRate = 250.00m,
                Description = "Test claim",
                Department = "Computer Science",
                LecturerId = 1,
                CreatedDate = System.DateTime.Now
            };
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Claim>(viewResult.Model);
            Assert.Equal(1, model.ClaimId);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Review_WithCoordinatorRole_ReturnsView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "coordinator-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var claim = new Claim
            {
                ClaimId = 1,
                ClaimDate = System.DateTime.Now,
                HoursWorked = 10,
                HourlyRate = 250.00m,
                Description = "Test claim",
                Department = "Computer Science",
                LecturerId = 1,
                Status = ClaimStatus.Pending,
                CreatedDate = System.DateTime.Now
            };
            _context.Claims.Add(claim);

            var lecturer = new Lecturer
            {
                LecturerId = 1,
                FirstName = "Test",
                LastName = "Lecturer"
            };
            _context.Lecturer.Add(lecturer);
            await _context.SaveChangesAsync();

            // Setup user with ProgrammeCoordinator role
            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, "coordinator-user-id"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "ProgrammeCoordinator")
                }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Review(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Claim>(viewResult.Model);
            Assert.Equal(1, model.ClaimId);
        }

        [Fact]
        public async Task ApproveByCoordinator_WithValidData_RedirectsToDashboard()
        {
            // Arrange
            var user = new ApplicationUser { Id = "coordinator-user-id" };
            var coordinator = new ProgrammeCoordinator
            {
                CoordinatorId = 1,
                UserId = "coordinator-user-id",
                FirstName = "Test",
                LastName = "Coordinator"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _context.ProgrammeCoordinator.Add(coordinator);

            var claim = new Claim
            {
                ClaimId = 1,
                Status = ClaimStatus.Pending,
                CreatedDate = System.DateTime.Now
            };
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new Mock<ISession>().Object;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await _controller.ApproveByCoordinator(1, true, "Approved");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Dashboard", redirectResult.ControllerName);
        }

        private void SetupAuthenticatedUser()
        {
            var user = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Name, "test@test.com"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "Lecturer")
                }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }
    }
}