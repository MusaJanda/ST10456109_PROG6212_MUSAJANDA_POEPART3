# Contract Monthly Claim System (CMCS) - Part 3

## Project Overview
The Contract Monthly Claim System (CMCS) is a comprehensive web application designed to streamline the submission, review, and approval of monthly claims submitted by contract lecturers. Part 3 introduces advanced automation features, HR management capabilities, and enhanced security measures.

## Part 3 Updates - Changes from Part 2

### 1. HR "Super User" Role Implementation 
**NEW in Part 3:**
- Added dedicated HR role with full user management capabilities
- HR creates all user accounts (no public registration)
- HR sets and manages hourly rates for all lecturers
- HR can update user information at any time
- Default HR account: `hr@cmcs.com` / `Hr@123`

### 2. Enhanced Lecturer Claim Submission 
**IMPROVED from Part 2:**
- **Auto-populated Data:** Lecturer name, surname, and hourly rate are automatically pulled from HR-managed data
- **Auto-calculation:** Total payment automatically calculated (Hours × Hourly Rate)
- **Real-time Display:** Payment breakdown shown instantly as lecturer types
- **Read-only Rate:** Lecturers cannot manually edit hourly rate (HR-controlled)
- **Validation:** Maximum 300 hours per month enforced with clear error messages

### 3. Report Generation System 
**NEW in Part 3:**
- HR can generate comprehensive reports using LINQ queries
- Filter by date range, status, and individual lecturers
- Summary statistics:
  - Total claims by lecturer
  - Total hours worked
  - Total amounts
  - Approval statistics
- Print and export capabilities

### 4. Session Management 
**NEW in Part 3:**
- Sessions implemented for all users
- Session tracking for coordinator and manager actions
- User access control enforced
- 30-minute idle timeout
- Session data persists across page navigation

### 5. Enhanced Access Control 
**IMPROVED from Part 2:**
- Role-based page restrictions enforced
- Unauthorized access properly handled with custom view
- Users cannot access pages outside their role permissions
- Session-based security tracking

### 6. Entity Framework Integration 
**IMPROVED from Part 2:**
- Full Entity Framework Core implementation
- Direct database operations (no migrations required)
- Proper decimal precision for monetary values
- Optimized LINQ queries for performance

## System Architecture

### User Roles
1. **HR (Super User)**
   - Create and manage all user accounts
   - Set hourly rates for lecturers
   - Update user information
   - Generate reports and invoices
   
2. **Lecturer**
   - Submit claims with auto-populated data
   - View claim status and history
   - Track claims through approval process
   - Cannot modify hourly rate

3. **Programme Coordinator**
   - Review and approve/reject pending claims
   - Add notes to claims
   - Forward approved claims to Academic Manager
   - Handle returned claims

4. **Academic Manager**
   - Final approval of claims
   - Return claims to coordinator for revision
   - Final rejection with notes
   - View coordinator approval details

5. **Admin**
   - Manage user roles
   - System administration

## Key Features

### HR Management Dashboard
- **User Creation:**
  - Create lecturers, coordinators, and managers
  - Set initial hourly rates
  - Generate login credentials
  - Email assignment

- **User Management:**
  - Update personal information
  - Modify hourly rates
  - View user statistics

- **Report Generation:**
  ```csharp
  // LINQ-based report aggregation
  var reportData = claims
      .GroupBy(c => c.Lecturer)
      .Select(g => new {
          TotalClaims = g.Count(),
          TotalAmount = g.Sum(c => c.HoursWorked * c.HourlyRate)
      });
  ```

### Lecturer Claim Submission
- **Auto-Calculation:**
  ```javascript
  function calculateTotal() {
      const total = hours * lecturerHourlyRate;
      // Displays R XXXX.XX in real-time
  }
  ```

- **Validation:**
  ```csharp
  // Monthly hour limit check
  if (totalHoursThisMonth + newHours > 300) {
      ModelState.AddModelError("Exceeds 300-hour limit");
  }
  ```

- **Features:**
  - Real-time payment calculation
  - File upload support (PDF, Word, Excel, Images)
  - Claim status tracking
  - Edit pending claims

### Approval Workflow
1. Lecturer submits claim
2. Programme Coordinator reviews
3. Academic Manager final approval
4. Claim marked as fully approved

### Session Implementation
```csharp
// Session configuration in Program.cs
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Usage in controllers
HttpContext.Session.SetString("UserId", user.Id);
HttpContext.Session.SetString("LastAction", $"Reviewed claim {id}");
```

## Technologies Used
- **Framework:** ASP.NET Core 8.0 MVC
- **Database:** SQL Server with Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Bootstrap 5, Bootstrap Icons
- **JavaScript:** Vanilla JS for client-side validation and calculation
- **LINQ:** Report generation and data aggregation
- **Session Management:** ASP.NET Core Session middleware

## Database Schema

### Tables
- **AspNetUsers** - Identity users (extended with ApplicationUser)
- **HR** - HR personnel records
- **Lecturer** - Lecturer profiles with hourly rates
- **Claim** - Claim submissions
- **Document** - Uploaded supporting documents
- **ProgrammeCoordinator** - Coordinator profiles
- **AcademicManager** - Manager profiles

### Key Relationships
```
ApplicationUser (1) ─── (1) Lecturer
ApplicationUser (1) ─── (1) HR
Lecturer (1) ─── (*) Claim
Claim (1) ─── (*) Document
ProgrammeCoordinator (1) ─── (*) Claim (approved)
AcademicManager (1) ─── (*) Claim (approved)
```

## Installation & Setup

### Prerequisites
- Visual Studio 2022
- .NET 8.0 SDK
- SQL Server (LocalDB or Express)

### Steps
1. **Clone the repository:**
   ```bash
   git clone https://github.com/MusaJanda/PROG6212_POEPART3_ST10456109_MusaJanda.git
   cd CMCS
   ```

2. **Update connection string:**
   Edit `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=JayDb;TrustServerCertificate=True;Trusted_Connection=True;"
     }
   }
   ```

3. **Create database:**
   ```bash
   dotnet ef database update
   ```
   
   Or run the application and it will create the database automatically.

4. **Run the application:**
   ```bash
   dotnet run
   ```
   Or press F5 in Visual Studio

5. **Default Accounts:**
   - **HR:** hr@cmcs.com / Hr@123
   - **Programmw Coordinator:** Pc@cmcs.com / Pc@123
   - **Academic Manager:** Am@cmcs.com / Am@123
   - **Lecturer:** Jele@gmail.com / Jele@123

## Usage Guide

### For HR:
1. Log in with HR credentials
2. Create lecturer accounts from HR dashboard
3. Set hourly rates during account creation
4. Update lecturer information as needed
5. Generate reports by filtering claims

### For Lecturers:
1. Log in with credentials provided by HR
2. Create new claim from dashboard
3. Enter hours worked (rate is auto-populated)
4. View auto-calculated total
5. Upload supporting documents
6. Submit and track claim status

### For Coordinators:
1. Log in and view pending claims
2. Review claim details and documents
3. Approve or reject with notes
4. View returned claims from managers

### For Managers:
1. Log in and view coordinator-approved claims
2. Review all claim information
3. Choose to:
   - Approve finally
   - Return to coordinator
   - Reject permanently

## Validation Rules

### Claim Submission:
- Hours worked: 0.5 - 200 per claim
-  Maximum 300 hours per month total
- Hourly rate: R50 - R1000 (HR-managed)
- Claim date cannot be in the future
- Description required (max 500 characters)
- Document size limit: 10MB per file

### User Creation (HR):
- Valid email address required
- Password minimum 6 characters
- Hourly rate between R50-R1000
- Role selection required

## Session Security Features

### Implementation:
```csharp
// Access control in dashboards
public async Task<IActionResult> CoordinatorDashboard()
{
    if (!User.IsInRole("ProgrammeCoordinator"))
    {
        return View("Unauthorized");
    }
    
    HttpContext.Session.SetString("DashboardView", "Coordinator");
    HttpContext.Session.SetString("LastAccess", DateTime.Now.ToString());
    
    // Dashboard logic
}
```

### Session Data Tracked:
- User ID
- User email
- Login timestamp
- Last action performed
- Dashboard view accessed
- Last access time

## Code Highlights

### Auto-Calculation (Client-Side):
```javascript
function calculateTotal() {
    const hourlyRate = parseFloat(lecturerRate);
    const hours = parseFloat(hoursInput.value) || 0;
    const total = hours * hourlyRate;
    
    displayTotal.textContent = total.toFixed(2);
    
    // Validation warning
    if (hours > 300) {
        alert('Warning: Exceeding monthly limit of 300 hours');
    }
}
```

### Monthly Hour Validation (Server-Side):
```csharp
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
        $"Total hours for this month ({totalHoursThisMonth + model.HoursWorked}) " +
        $"exceeds the maximum of 300 hours.");
}
```

### LINQ Report Generation:
```csharp
var reportData = claimsList
    .GroupBy(c => c.Lecturer)
    .Select(g => new
    {
        LecturerName = $"{g.Key.FirstName} {g.Key.LastName}",
        TotalClaims = g.Count(),
        TotalHours = g.Sum(c => c.HoursWorked),
        TotalAmount = g.Sum(c => c.HoursWorked * c.HourlyRate),
        ApprovedClaims = g.Count(c => c.Status == ClaimStatus.FullyApproved),
        PendingClaims = g.Count(c => c.Status == ClaimStatus.Pending || 
                                      c.Status == ClaimStatus.ApprovedByCoordinator)
    })
    .ToList();
```

## Testing Scenarios

### Scenario 1: HR Creates Lecturer
1. HR logs in
2. Clicks "Create New User"
3. Fills in lecturer details (Rate: R250/hour)
4. Submits form
5. Lecturer can immediately log in
6. Hourly rate appears in claim form

### Scenario 2: Lecturer Submits Claim
1. Lecturer logs in
2. Creates claim for 10 hours
3. Rate auto-populated: 250
4. Total auto-calculated: R2,500
5. Submits claim
6. Validation passes (under 300 hours)

### Scenario 3: Monthly Hour Limit
1. Lecturer has 290 hours claimed this month
2. Attempts to claim 20 more hours
3. Validation fails
4. Error message: "Exceeds 300-hour limit"
5. Can only claim 10 hours

### Scenario 4: Approval Workflow
1. Coordinator approves claim
2. Session logged
3. Manager reviews
4. Can see coordinator details
5. Manager approves
6. Status: Fully Approved


## Known Issues & Solutions

### Issue 1: Session Timeout
- **Problem:** Users logged out unexpectedly
- **Solution:** Increased timeout to 30 minutes

### Issue 2: Decimal Precision
- **Problem:** Rounding errors in calculations
- **Solution:** Used `decimal(18, 2)` in database

### Issue 3: File Upload Size
- **Problem:** Large files causing errors
- **Solution:** Enforced 10MB limit with validation

## Future Enhancements  
-  Email notifications
-  SMS alerts
-  API for mobile app
-  Advanced analytics
-  Bulk claim submission
-  Document OCR scanning
-  Multi-currency support

## Contributors
- Musa Janda (Developer)
- Junior Manganyi (Supervisor)


## Contact
- **Email:** ST10456109@imconnect.edu.za
- **GitHub:** Musa Janda
- **Video Demo:** https://youtu.be/pQOLY68tt5Q 



## Acknowledgments
- The Independent Institute of Education
- PROG6212 Course Coordinator
- Bootstrap Team
- Microsoft ASP.NET Core Team

---

**Last Updated:** 21 November 2025
**Version:** 3.0
**Status:** Complete
