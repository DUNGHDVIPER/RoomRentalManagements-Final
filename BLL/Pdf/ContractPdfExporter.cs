using DAL.Entities.Contracts;
using DAL.Entities.Motel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BLL.Pdf;

public static class ContractPdfExporter
{
    public static byte[] Export(Contract c)
    {
        // QuestPDF requires license setting
        QuestPDF.Settings.License = LicenseType.Community;

        var code = string.IsNullOrWhiteSpace(c.ContractCode)
            ? $"CT-{c.ContractId}"
            : c.ContractCode.Trim();

        var roomLine = c.Room != null
            ? $"{(string.IsNullOrWhiteSpace(c.Room.RoomNo) ? "" : c.Room.RoomNo + " - ")}{c.Room.RoomNo} (Id={c.RoomId})"
            : $"RoomId = {c.RoomId}";

        var tenantLine = c.Tenant != null
            ? $"{c.Tenant.FullName} (Id={c.TenantId})"
            : $"TenantId = {c.TenantId}";

        var rentText = $"{c.BaseRent:n0}";
        var depositText = $"{c.DepositAmount:n0}";
        var statusText = c.Status ?? "";

        var noteText = string.IsNullOrWhiteSpace(c.Note) ? "(none)" : c.Note!.Trim();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                // ===== HEADER =====
                page.Header().Column(h =>
                {
                    h.Spacing(4);

                    h.Item().Text("RENTAL CONTRACT").FontSize(18).SemiBold();
                    h.Item().Text($"Contract Code: {code}")
                        .FontColor(Colors.Grey.Darken2);

                    // ✅ FIX: do NOT chain LineHorizontal().LineColor() (LineHorizontal returns void in some builds)
                    h.Item()
                        .PaddingTop(8)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Lighten2);
                });

                // ===== CONTENT =====
                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(12);

                    // Parties
                    col.Item().Text("Parties").FontSize(13).SemiBold();

                    col.Item().Row(r =>
                    {
                        r.RelativeItem()
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(10)
                            .Column(x =>
                            {
                                x.Spacing(4);
                                x.Item().Text("Room").SemiBold();
                                x.Item().Text(roomLine);
                            });

                        r.ConstantItem(10);

                        r.RelativeItem()
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(10)
                            .Column(x =>
                            {
                                x.Spacing(4);
                                x.Item().Text("Tenant").SemiBold();
                                x.Item().Text(tenantLine);
                            });
                    });

                    // Terms
                    col.Item().Text("Contract Terms").FontSize(13).SemiBold();

                    col.Item()
                        .Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(10)
                        .Column(x =>
                        {
                            x.Spacing(8);

                            x.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Start Date: ").SemiBold();
                                    t.Span($"{c.StartDate:yyyy-MM-dd}");
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("End Date: ").SemiBold();
                                    t.Span($"{c.EndDate:yyyy-MM-dd}");
                                });
                            });

                            x.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Rent: ").SemiBold();
                                    t.Span(rentText);
                                });

                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Deposit: ").SemiBold();
                                    t.Span(depositText);
                                });
                            });

                            x.Item().Text(t =>
                            {
                                t.Span("Status: ").SemiBold();
                                t.Span(statusText);
                            });

                            if (!string.IsNullOrWhiteSpace(c.PaymentCycle))
                            {
                                x.Item().Text(t =>
                                {
                                    t.Span("Payment Cycle: ").SemiBold();
                                    t.Span(c.PaymentCycle);
                                });
                            }
                        });

                    // Note
                    col.Item().Text("Note").FontSize(13).SemiBold();
                    col.Item()
                        .Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(10)
                        .Text(noteText);

                    // Signatures
                    col.Item().Text("Signatures").FontSize(13).SemiBold();

                    col.Item().Row(row =>
                    {
                        row.Spacing(16);

                        row.RelativeItem()
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(12)
                            .Height(150)
                            .Column(x =>
                            {
                                x.Spacing(10);
                                x.Item().Text("LANDLORD / HOST").SemiBold();
                                x.Item().Text("Signature: ______________________________");
                                x.Item().Text("Full name: ______________________________");
                                x.Item().Text("Date: ___________________________________");
                            });

                        row.RelativeItem()
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(12)
                            .Height(150)
                            .Column(x =>
                            {
                                x.Spacing(10);
                                x.Item().Text("TENANT").SemiBold();
                                x.Item().Text("Signature: ______________________________");
                                x.Item().Text("Full name: ______________________________");
                                x.Item().Text("Date: ___________________________________");
                            });
                    });
                });

                // ===== FOOTER =====
                page.Footer()
                    .AlignCenter()
                    .Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                    
            });
        });

        return doc.GeneratePdf();
    }
}