using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MathBridgeSystem.Domain.Entities;
using System.IO;

namespace MathBridgeSystem.Application.Services
{
    public static class ContractPdfGenerator
    {
        public static byte[] GenerateContractPdf(
            Contract contract,
            Child child,
            User parent,
            PaymentPackage package,
            User? mainTutor,
            Center? center)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header()
                        .AlignCenter()
                        .Text("TUTORING CONTRACT - MATHBRIDGE")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Student Information:").Bold();
                                    col.Item().Text($"Name: {child.FullName}");
                                    col.Item().Text($"Grade: {child.Grade}");
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Parent Information:").Bold();
                                    col.Item().Text($"Name: {parent.FullName}");
                                    col.Item().Text($"Email: {parent.Email}");
                                    col.Item().Text($"Phone: {parent.PhoneNumber}");
                                });
                            });

                            column.Item().PaddingTop(15).Text("Package Details:").Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Text("Package Name").Bold();
                                table.Cell().Text(package.PackageName);

                                table.Cell().Text("Sessions").Bold();
                                table.Cell().Text($"{package.SessionCount} sessions");

                                table.Cell().Text("Price").Bold();
                                table.Cell().Text($"{package.Price:0,0} VND");

                                table.Cell().Text("Duration").Bold();
                                table.Cell().Text($"{contract.StartDate:dd/MM/yyyy} to {contract.EndDate:dd/MM/yyyy}");
                            });

                            if (mainTutor != null)
                            {
                                column.Item().PaddingTop(15).Text("Main Tutor:").Bold();
                                column.Item().Text(mainTutor.FullName);
                            }

                            if (center != null)
                            {
                                column.Item().PaddingTop(15).Text("Center:").Bold();
                                column.Item().Text(center.Name);
                            }

                            column.Item().PaddingTop(20).Text("Schedule:").Bold();
                            column.Item().Text(FormatDaysOfWeek(contract.DaysOfWeeks));
                            column.Item().Text($"Time: {contract.StartTime} - {contract.EndTime}");

                            column.Item().PaddingTop(30)
                                .AlignCenter()
                                .Text("Thank you for choosing MathBridge!")
                                .FontSize(14)
                                .Italic()
                                .FontColor(Colors.Grey.Darken2);

                            column.Item().PaddingTop(20)
                                .AlignCenter()
                                .Text($"Contract ID: {contract.ContractId}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm} | MathBridge System")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                });
            }).GeneratePdf();
        }

        private static string FormatDaysOfWeek(int? daysOfWeek)
        {
            if (!daysOfWeek.HasValue || daysOfWeek == 0) return "No schedule";

            var days = new List<string>();
            var value = daysOfWeek.Value;
            if ((value & 1) != 0) days.Add("Sun");
            if ((value & 2) != 0) days.Add("Mon");
            if ((value & 4) != 0) days.Add("Tue");
            if ((value & 8) != 0) days.Add("Wed");
            if ((value & 16) != 0) days.Add("Thu");
            if ((value & 32) != 0) days.Add("Fri");
            if ((value & 64) != 0) days.Add("Sat");
            return string.Join(", ", days);
        }
    }
}