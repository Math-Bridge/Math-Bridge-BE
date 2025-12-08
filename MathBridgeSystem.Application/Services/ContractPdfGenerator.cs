using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

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
                        .FontSize(22)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            // Thông tin học sinh & phụ huynh
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Student Information:").Bold().FontSize(14);
                                    col.Item().Text($"Name: {child.FullName}");
                                    col.Item().Text($"Grade: {child.Grade}");
                                });
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Parent Information:").Bold().FontSize(14);
                                    col.Item().Text($"Name: {parent.FullName}");
                                    col.Item().Text($"Email: {parent.Email}");
                                    col.Item().Text($"Phone: {parent.PhoneNumber}");
                                });
                            });

                            // Gói học
                            column.Item().PaddingTop(15).Text("Package Details:").Bold().FontSize(14);
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
                                table.Cell().Text($"{package.Price:N0} VND");

                                table.Cell().Text("Duration").Bold();
                                table.Cell().Text($"{contract.StartDate:dd/MM/yyyy} to {contract.EndDate:dd/MM/yyyy}");
                            });

                            // Gia sư chính
                            if (mainTutor != null)
                            {
                                column.Item().PaddingTop(12).Text("Main Tutor:").Bold().FontSize(14);
                                column.Item().Text(mainTutor.FullName);
                            }

                            // Trung tâm (nếu học offline)
                            if (center != null)
                            {
                                column.Item().PaddingTop(12).Text("Center:").Bold().FontSize(14);
                                column.Item().Text(center.Name);
                            }

                            // LỊCH HỌC LINH HOẠT – ĐẸP NHƯ HỢP ĐỒNG CAO CẤP
                            column.Item().PaddingTop(20).Text("Weekly Schedule:").Bold().FontSize(14);
                            column.Item().PaddingTop(8).Element(ComposeScheduleTable);

                            void ComposeScheduleTable(IContainer container)
                            {
                                container.Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(100); // Day
                                        columns.ConstantColumn(120); // Time
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(5)
                                              .Text("Day of Week").Bold();
                                        header.Cell().Border(1).Background(Colors.Grey.Lighten2).Padding(5)
                                              .Text("Time").Bold();
                                    });

                                    foreach (var schedule in contract.ContractSchedules.OrderBy(s => s.DayOfWeek))
                                    {
                                        table.Cell().Border(1).Padding(5)
                                             .Text(GetDayName(schedule.DayOfWeek));

                                        table.Cell().Border(1).Padding(5)
                                             .Text($"{schedule.StartTime:HH:mm} - {schedule.EndTime:HH:mm}");
                                    }

                                    if (!contract.ContractSchedules.Any())
                                    {
                                        table.Cell().ColumnSpan(2).Padding(10)
                                             .Text("No schedule defined").Italic().FontColor(Colors.Grey.Medium);
                                    }
                                });
                            }

                            // Footer cảm ơn
                            column.Item().PaddingTop(40)
                                  .AlignCenter()
                                  .Text("Thank you for choosing MathBridge!")
                                  .FontSize(16)
                                  .Italic()
                                  .FontColor(Colors.Grey.Darken2);

                            column.Item().PaddingTop(15)
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

        // Helper: Chuyển DayOfWeek → tên ngày tiếng Anh đẹp
        private static string GetDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Monday",
                DayOfWeek.Tuesday => "Tuesday",
                DayOfWeek.Wednesday => "Wednesday",
                DayOfWeek.Thursday => "Thursday",
                DayOfWeek.Friday => "Friday",
                DayOfWeek.Saturday => "Saturday",
                DayOfWeek.Sunday => "Sunday",
                _ => day.ToString()
            };
        }
    }
}