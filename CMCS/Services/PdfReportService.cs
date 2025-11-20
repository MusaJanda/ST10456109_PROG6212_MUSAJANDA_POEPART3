using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CMCS.ViewModels;
using CMCS.Models;

namespace CMCS.Services
{
    public class PdfReportService : IPdfReportService
    {
        public PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateUserReport(List<UserListViewModel> users)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("User Management Report")
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);

                            column.Item().Text($"Generated on: {DateTime.Now:dd MMMM yyyy}")
                                .FontSize(10)
                                .Italic();
                        });
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Name
                            columns.RelativeColumn(2); // Email
                            columns.RelativeColumn(1.5f); // Role
                            columns.RelativeColumn(1); // Status
                            columns.RelativeColumn(1); // Created
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Name").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Email").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Role").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Created").Bold();
                        });

                        // Rows
                        foreach (var user in users)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{user.FirstName} {user.LastName}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.Email);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.Role);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.IsActive ? "Active" : "Inactive");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.CreatedDate.ToString("dd/MM/yyyy"));
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GeneratePayrollReport(List<Claim> claims)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Text("Payroll Report").Bold().FontSize(20);
                    page.Header().Text($"Generated on: {DateTime.Now:dd MMMM yyyy}").FontSize(10);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); // Claim ID
                            columns.RelativeColumn(2); // Lecturer
                            columns.RelativeColumn(); // Date
                            columns.RelativeColumn(); // Hours
                            columns.RelativeColumn(); // Rate
                            columns.RelativeColumn(); // Amount
                            columns.RelativeColumn(); // Status
                            columns.RelativeColumn(); // Payment
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("ID").Bold();
                            header.Cell().Text("Lecturer").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Hours").Bold();
                            header.Cell().Text("Rate").Bold();
                            header.Cell().Text("Amount").Bold();
                            header.Cell().Text("Status").Bold();
                            header.Cell().Text("Payment").Bold();
                        });

                        foreach (var claim in claims)
                        {
                            table.Cell().Text(claim.ClaimId.ToString());
                            table.Cell().Text($"{claim.Lecturer.FirstName} {claim.Lecturer.LastName}");
                            table.Cell().Text(claim.ClaimDate.ToString("dd/MM/yyyy"));
                            table.Cell().Text(claim.HoursWorked.ToString("F1"));
                            table.Cell().Text($"R {claim.HourlyRate:F2}");
                            table.Cell().Text($"R {claim.TotalAmount:F2}");
                            table.Cell().Text(claim.Status.ToString());
                            table.Cell().Text(claim.PaymentStatus.ToString());
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateClaimsReport(List<Claim> claims)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Text("Claims Report").Bold().FontSize(20);
                    page.Header().Text($"Generated on: {DateTime.Now:dd MMMM yyyy}").FontSize(10);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); // ID
                            columns.RelativeColumn(2); // Lecturer
                            columns.RelativeColumn(); // Date
                            columns.RelativeColumn(); // Hours
                            columns.RelativeColumn(); // Rate
                            columns.RelativeColumn(); // Amount
                            columns.RelativeColumn(); // Status
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("ID").Bold();
                            header.Cell().Text("Lecturer").Bold();
                            header.Cell().Text("Date").Bold();
                            header.Cell().Text("Hours").Bold();
                            header.Cell().Text("Rate").Bold();
                            header.Cell().Text("Amount").Bold();
                            header.Cell().Text("Status").Bold();
                        });

                        foreach (var claim in claims)
                        {
                            table.Cell().Text(claim.ClaimId.ToString());
                            table.Cell().Text($"{claim.Lecturer.FirstName} {claim.Lecturer.LastName}");
                            table.Cell().Text(claim.ClaimDate.ToString("dd/MM/yyyy"));
                            table.Cell().Text(claim.HoursWorked.ToString("F1"));
                            table.Cell().Text($"R {claim.HourlyRate:F2}");
                            table.Cell().Text($"R {claim.TotalAmount:F2}");
                            table.Cell().Text(claim.Status.ToString());
                        }
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateFinancialReport(List<Claim> claims)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Text("Financial Summary Report").Bold().FontSize(20);

                    // Summary statistics
                    page.Content().Grid(grid =>
                    {
                        grid.Columns(4);

                        grid.Item().Background(Colors.Blue.Lighten3).Padding(10).Text("Total Claims").Bold();
                        grid.Item().Background(Colors.Green.Lighten3).Padding(10).Text("Pending Payment").Bold();
                        grid.Item().Background(Colors.Orange.Lighten3).Padding(10).Text("Total Paid").Bold();
                        grid.Item().Background(Colors.Red.Lighten3).Padding(10).Text("Rejected Claims").Bold();

                        grid.Item().Padding(10).Text(claims.Count.ToString());
                        grid.Item().Padding(10).Text(claims.Count(c => c.PaymentStatus == PaymentStatus.Unpaid && c.Status == ClaimStatus.FullyApproved).ToString());
                        grid.Item().Padding(10).Text(claims.Count(c => c.PaymentStatus == PaymentStatus.Paid).ToString());
                        grid.Item().Padding(10).Text(claims.Count(c => c.Status == ClaimStatus.Rejected).ToString());
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Add the missing GenerateInvoice method
        public byte[] GenerateInvoice(Claim claim)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Text("INVOICE").Bold().FontSize(24);
                    page.Header().Text($"Claim #{claim.ClaimId}").FontSize(14);

                    page.Content().Grid(grid =>
                    {
                        grid.Columns(2);

                        // Left column - Lecturer info
                        grid.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
                        {
                            column.Item().Text("BILL TO").Bold();
                            column.Item().Text($"{claim.Lecturer.FirstName} {claim.Lecturer.LastName}");
                            column.Item().Text(claim.Lecturer.Email);
                            if (!string.IsNullOrEmpty(claim.Lecturer.PhoneNumber))
                                column.Item().Text(claim.Lecturer.PhoneNumber);
                        });

                        // Right column - Claim info
                        grid.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
                        {
                            column.Item().Text("INVOICE DETAILS").Bold();
                            column.Item().Text($"Date: {claim.ClaimDate:dd MMMM yyyy}");
                            column.Item().Text($"Hours: {claim.HoursWorked}");
                            column.Item().Text($"Rate: R {claim.HourlyRate:F2}/hour");
                        });
                    });

                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Description
                            columns.RelativeColumn(); // Hours
                            columns.RelativeColumn(); // Rate
                            columns.RelativeColumn(); // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").Bold();
                            header.Cell().Text("Hours").Bold();
                            header.Cell().Text("Rate").Bold();
                            header.Cell().Text("Amount").Bold();
                        });

                        table.Cell().Text(claim.Description);
                        table.Cell().Text(claim.HoursWorked.ToString("F1"));
                        table.Cell().Text($"R {claim.HourlyRate:F2}");
                        table.Cell().Text($"R {claim.TotalAmount:F2}").Bold();
                    });

                    page.Footer().AlignRight().Text($"Total Amount Due: R {claim.TotalAmount:F2}").Bold().FontSize(16);
                });
            });

            return document.GeneratePdf();
        }
    }
}