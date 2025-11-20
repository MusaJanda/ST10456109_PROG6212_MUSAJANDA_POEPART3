using CMCS.Models;

namespace CMCS.ViewModels
{
    public class HRClaimsViewModel
    {
        public List<Claim> ApprovedClaims { get; set; } = new List<Claim>();
        public List<Claim> PaidClaims { get; set; } = new List<Claim>();
        public decimal TotalPendingAmount => ApprovedClaims.Where(c => c.PaymentStatus == PaymentStatus.Unpaid).Sum(c => c.TotalAmount);
        public decimal TotalPaidAmount => PaidClaims.Sum(c => c.TotalAmount);
    }
}