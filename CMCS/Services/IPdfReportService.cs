using CMCS.ViewModels;
using CMCS.Models;

namespace CMCS.Services
{
    public interface IPdfReportService
    {
        byte[] GenerateUserReport(List<UserListViewModel> users);
        byte[] GeneratePayrollReport(List<Claim> claims);
        byte[] GenerateClaimsReport(List<Claim> claims);
        byte[] GenerateFinancialReport(List<Claim> claims);
        byte[] GenerateInvoice(Claim claim);

    }
}