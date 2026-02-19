using CarRentalService.Constants;
using CarRentalService.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarRentalService.Services.Pdf
{
    public class AccidentReportPdfGenerator
    {
        public byte[] Generate(AccidentReport report)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header()
                        .Text("ACCIDENT REPORT")
                        .FontSize(20)
                        .Bold()
                        .AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Full name: {report.FullName}");
                        col.Item().Text($"OIB: {report.Oib}");
                        col.Item().Text($"Driver License: {report.DriverLicenseNumber}");
                        col.Item().Text($"Phone: {report.Phone}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Accident date: {report.AccidentDate.ToString(Formats.DateWithTime)}");
                        col.Item().Text($"Location: {report.Location}");
                        col.Item().Text($"Weather: {report.Weather}");
                        col.Item().Text($"Road condition: {report.RoadCondition}");
                        col.Item().Text($"Speed: {report.Speed} km/h");
                        col.Item().Text($"Police notified: {(report.PoliceNotified ? "Yes" : "No")}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text("Damage description:");
                        col.Item().Text(report.DamageDescription);

                        if (report.OtherPartyInvolved)
                        {
                            col.Item().LineHorizontal(1);
                            col.Item().Text("Other party involved:");
                            col.Item().Text($"Name: {report.OtherPartyName}");
                            col.Item().Text($"Vehicle plate: {report.OtherPartyPlate}");
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated on ");
                            x.Span(DateTime.Now.ToString(Formats.DateWithTime));
                        });
                });
            }).GeneratePdf();
        }
    }
}
