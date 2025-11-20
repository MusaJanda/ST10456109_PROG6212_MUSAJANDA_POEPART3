using CMCS.Models;

namespace CMCS.ViewModels
{
    public class ReportViewModel
    {
        public string ReportType { get; set; } = "summary";
        public string UserRole { get; set; }
        public List<UserListViewModel> Users { get; set; } = new List<UserListViewModel>();
        public List<Claim> Claims { get; set; } = new List<Claim>();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Statistics
        public int TotalUsers => Users.Count;
        public int ActiveUsers => Users.Count(u => u.IsActive);
        public int TotalClaims => Claims.Count;
        public decimal TotalClaimAmount => Claims.Sum(c => c.TotalAmount);
        public int PendingClaims => Claims.Count(c => c.Status == ClaimStatus.Pending);
        public int ApprovedClaims => Claims.Count(c => c.Status == ClaimStatus.FullyApproved);
        public int PaidClaims => Claims.Count(c => c.PaymentStatus == PaymentStatus.Paid);
    }
}