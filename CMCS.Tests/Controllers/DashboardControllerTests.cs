using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Controllers;
using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CMCS.Tests
{
    public class DashboardControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<DashboardController>> _mockLogger;
        private readonly Mock<IPdfReportService> _mockPdfService;
        private readonly ApplicationDbContext _context;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
           .UseInMemoryDatabase(databaseName: "Test_Dashboard_DB_" + System.Guid.NewGuid())
           .Options;
            _context = new ApplicationDbContext(options);

            // Mock UserManager
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            // Mock Logger
            _mockLogger = new Mock<ILogger<DashboardController>>();

            // Mock PdfReportService
            _mockPdfService = new Mock<IPdfReportService>();

            _controller = new DashboardController(
                _context,
                _mockUserManager.Object,
                _mockLogger.Object,
                _mockPdfService.Object); // Add the mock service
        }

        [Fact]
        public async Task Index_WithLecturerRole_ReturnsLecturerDashboard()
        {
            // Arrange
            var user = new ApplicationUser { Id = "lecturer-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var lecturer = new Lecturer
            {
                LecturerId = 1,
                UserId = "lecturer-user-id",
                FirstName = "Test",
                LastName = "Lecturer"
            };
            _context.Lecturer.Add(lecturer);
            await _context.SaveChangesAsync();

            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, "lecturer-user-id"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "Lecturer")
                }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("LecturerDashboard", viewResult.ViewName);
        }

        [Fact]
        public async Task Index_WithProgrammeCoordinatorRole_ReturnsCoordinatorDashboard()
        {
            // Arrange
            var user = new ApplicationUser { Id = "coordinator-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

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
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ProgrammeCoordinatorDashboard", viewResult.ViewName);
        }

        [Fact]
        public async Task Index_WithAcademicManagerRole_ReturnsManagerDashboard()
        {
            // Arrange
            var user = new ApplicationUser { Id = "manager-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, "manager-user-id"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "AcademicManager")
                }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("AcademicManagerDashboard", viewResult.ViewName);
        }

        [Fact]
        public async Task Index_WithNoRole_ReturnsUnauthorizedView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "basic-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, "basic-user-id")
                    // No roles assigned
                }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Unauthorized", viewResult.ViewName);
        }

        [Fact]
        public async Task Index_WithLecturerRoleButNoLecturerRecord_ReturnsUnauthorizedView()
        {
            // Arrange
            var user = new ApplicationUser { Id = "lecturer-user-id" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Don't add lecturer record to simulate missing profile

            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]
                {
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.NameIdentifier, "lecturer-user-id"),
                    new System.Security.Claims.Claim(
                        System.Security.Claims.ClaimTypes.Role, "Lecturer")
                }, "TestAuthentication"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Unauthorized", viewResult.ViewName);
        }
    }
}