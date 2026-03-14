using BLL.DTOs.Report;
using BLL.Services.Interfaces;
using ClosedXML.Excel;
using DAL.Data;
using DAL.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace BLL.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

   public async Task<RevenueReportDto> GetRevenueDashboardAsync(int year, CancellationToken ct = default)
{
    var totalRooms = await _db.Rooms
        .AsNoTracking()
        .CountAsync(r => r.Status != RoomStatus.Disabled, ct);

    var occupiedRooms = await _db.Contracts
        .AsNoTracking()
        .Where(c => c.Status == "Active")
        .Select(c => c.RoomId)
        .Distinct()
        .CountAsync(ct);

    // 1) ISSUED
    var issuedByMonth = await _db.Bills
        .AsNoTracking()
        .Where(b => (b.Period / 100) == year)
        .GroupBy(b => b.Period % 100)
        .Select(g => new { Month = g.Key, Sum = g.Sum(x => x.TotalAmount) })
        .ToListAsync(ct);

    // 2) COLLECTED
    var collectedByMonth = await _db.Payments
        .AsNoTracking()
        .Where(p => p.Status == PaymentStatus.Completed && (p.Bill.Period / 100) == year)
        .GroupBy(p => p.Bill.Period % 100)
        .Select(g => new { Month = g.Key, Sum = g.Sum(x => x.Amount) })
        .ToListAsync(ct);

    // ✅ DTO phải tạo TRƯỚC khi dùng
    var dto = new RevenueReportDto
    {
        Year = year,
        TotalRooms = totalRooms,
        OccupiedRooms = occupiedRooms
    };

    // ✅ init đủ 12 tháng + init breakdown
    for (int m = 1; m <= 12; m++)
    {
        dto.RevenueByMonth[m] = 0;
        dto.IssuedByMonth[m] = 0;
        dto.IssuedBreakdownByMonth[m] = new FeeBreakdownDto();
    }

    foreach (var x in issuedByMonth)
        if (x.Month is >= 1 and <= 12)
            dto.IssuedByMonth[x.Month] = x.Sum;

    foreach (var x in collectedByMonth)
        if (x.Month is >= 1 and <= 12)
            dto.RevenueByMonth[x.Month] = x.Sum;

    // ✅ 3) BREAKDOWN theo BillItems (sau khi dto đã tồn tại)
    var breakdownRaw = await _db.BillItems
        .AsNoTracking()
        .Where(i => (i.Bill.Period / 100) == year)
        .Select(i => new
        {
            Month = i.Bill.Period % 100,
            Cat =
                i.ExtraFeeId.HasValue ? "Other" :
                i.Name.ToLower() == "rent" ? "Rent" :
                i.Name.ToLower() == "electric" ? "Electric" :
                i.Name.ToLower() == "water" ? "Water" :
                "Other",
            Amount = i.Amount
        })
        .GroupBy(x => new { x.Month, x.Cat })
        .Select(g => new { g.Key.Month, g.Key.Cat, Sum = g.Sum(v => v.Amount) })
        .ToListAsync(ct);

    foreach (var x in breakdownRaw)
    {
        if (x.Month is < 1 or > 12) continue;

        var b = dto.IssuedBreakdownByMonth[x.Month];

        switch (x.Cat)
        {
            case "Rent": b.Rent += x.Sum; break;
            case "Electric": b.Electric += x.Sum; break;
            case "Water": b.Water += x.Sum; break;
            default: b.Other += x.Sum; break;
        }
    }

    dto.TotalIssued = dto.IssuedByMonth.Values.Sum();
    dto.TotalRevenue = dto.RevenueByMonth.Values.Sum();
    dto.TotalDebt = dto.TotalIssued - dto.TotalRevenue;

    dto.CollectionRate = dto.TotalIssued <= 0
        ? 0
        : (double)dto.TotalRevenue / (double)dto.TotalIssued;

    return dto;
}

    public Task<ProfitReportDto> GetProfitDashboardAsync(int year, CancellationToken ct = default)
    {
        // Chưa có dữ liệu chi phí => profit để 0 (stub)
        var dto = new ProfitReportDto { Year = year, TotalProfit = 0 };
        for (int m = 1; m <= 12; m++) dto.ProfitByMonth[m] = 0;
        return Task.FromResult(dto);
    }

    // =========================
    // EXCEL EXPORT (PRETTY)
    // =========================
    public async Task<byte[]> ExportExcelStubAsync(string reportName, CancellationToken ct = default)
    {
        var (year, mode, month) = ParseReportOptions(reportName);
        var data = await GetRevenueDashboardAsync(year, ct);

        decimal Issued(int m) => data.IssuedByMonth.TryGetValue(m, out var v) ? v : 0;
        decimal Collected(int m) => data.RevenueByMonth.TryGetValue(m, out var v) ? v : 0;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Revenue");

        var titleStyle = wb.Style;
        titleStyle.Font.Bold = true;
        titleStyle.Font.FontSize = 16;

        var labelStyle = wb.Style;
        labelStyle.Font.Bold = true;

        var moneyFormat = "#,##0";
        var headerFill = XLColor.FromHtml("#F1F5F9");

        ws.Column(1).Width = 18;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 18;

        // Title
        ws.Range("A1:D1").Merge();
        ws.Cell("A1").Value = mode == "month" && month.HasValue
            ? $"Revenue & Debt - {year} (Month {month:00})"
            : $"Revenue & Debt - {year} ({mode.ToUpperInvariant()})";
        ws.Cell("A1").Style = titleStyle;
        ws.Row(1).Height = 26;

        // Summary (always show full-year KPI; if month selected, show month KPI too)
        ws.Cell("A3").Value = "Year";
        ws.Cell("B3").Value = year;

        ws.Cell("A4").Value = "Total issued";
        ws.Cell("B4").Value = data.TotalIssued;
        ws.Cell("B4").Style.NumberFormat.Format = moneyFormat;

        ws.Cell("A5").Value = "Total collected";
        ws.Cell("B5").Value = data.TotalRevenue;
        ws.Cell("B5").Style.NumberFormat.Format = moneyFormat;

        ws.Cell("A6").Value = "Outstanding debt";
        ws.Cell("B6").Value = data.TotalDebt;
        ws.Cell("B6").Style.NumberFormat.Format = moneyFormat;

        ws.Cell("A7").Value = "Collection rate";
        ws.Cell("B7").Value = data.CollectionRate;
        ws.Cell("B7").Style.NumberFormat.Format = "0%";

        ws.Range("A3:A7").Style = labelStyle;
        ws.Range("A3:B7").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range("A3:B7").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        int headerRow = 9;

        // Header
        ws.Cell(headerRow, 1).Value = mode == "quarter" ? "Quarter" : (mode == "year" ? "Year" : "Month");
        ws.Cell(headerRow, 2).Value = "Issued";
        ws.Cell(headerRow, 3).Value = "Collected";
        ws.Cell(headerRow, 4).Value = "Debt";

        ws.Range(headerRow, 1, headerRow, 4).Style.Font.Bold = true;
        ws.Range(headerRow, 1, headerRow, 4).Style.Fill.BackgroundColor = headerFill;
        ws.Range(headerRow, 1, headerRow, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(headerRow, 1, headerRow, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        int startRow = headerRow + 1;
        int r = startRow;

        void WriteRow(string label, decimal issued, decimal collected)
        {
            var debt = issued - collected;
            ws.Cell(r, 1).Value = label;
            ws.Cell(r, 2).Value = issued;
            ws.Cell(r, 3).Value = collected;
            ws.Cell(r, 4).Value = debt;

            ws.Cell(r, 2).Style.NumberFormat.Format = moneyFormat;
            ws.Cell(r, 3).Style.NumberFormat.Format = moneyFormat;
            ws.Cell(r, 4).Style.NumberFormat.Format = moneyFormat;

            ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            r++;
        }

        if (mode == "year")
        {
            WriteRow(year.ToString(), data.TotalIssued, data.TotalRevenue);
        }
        else if (mode == "quarter")
        {
            for (int q = 1; q <= 4; q++)
            {
                int s = (q - 1) * 3 + 1;
                var issued = Issued(s) + Issued(s + 1) + Issued(s + 2);
                var collected = Collected(s) + Collected(s + 1) + Collected(s + 2);
                WriteRow($"Q{q}", issued, collected);
            }
        }
        else // month
        {
            if (month is >= 1 and <= 12)
            {
                var issued = Issued(month.Value);
                var collected = Collected(month.Value);
                WriteRow(month.Value.ToString(), issued, collected);
            }
            else
            {
                for (int m = 1; m <= 12; m++)
                    WriteRow(m.ToString(), Issued(m), Collected(m));
            }
        }

        // Total row
        ws.Cell(r, 1).Value = "TOTAL";
        ws.Cell(r, 2).Value = (mode == "month" && month.HasValue) ? Issued(month.Value) : data.TotalIssued;
        ws.Cell(r, 3).Value = (mode == "month" && month.HasValue) ? Collected(month.Value) : data.TotalRevenue;
        ws.Cell(r, 4).Value = (mode == "month" && month.HasValue)
            ? (Issued(month.Value) - Collected(month.Value))
            : data.TotalDebt;

        ws.Range(r, 1, r, 4).Style.Font.Bold = true;
        ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

        ws.Cell(r, 2).Style.NumberFormat.Format = moneyFormat;
        ws.Cell(r, 3).Style.NumberFormat.Format = moneyFormat;
        ws.Cell(r, 4).Style.NumberFormat.Format = moneyFormat;

        ws.Range($"A{headerRow}:D{r}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range($"A{headerRow}:D{r}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.SheetView.FreezeRows(headerRow);

        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 1);

        ws.Columns("A:D").AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // =========================
    // PDF EXPORT (PRETTY TEXT)
    // =========================
    public async Task<byte[]> ExportPdfStubAsync(string reportName, CancellationToken ct = default)
    {
        var (year, mode, month) = ParseReportOptions(reportName);
        var data = await GetRevenueDashboardAsync(year, ct);

        decimal Issued(int m) => data.IssuedByMonth.TryGetValue(m, out var v) ? v : 0;
        decimal Collected(int m) => data.RevenueByMonth.TryGetValue(m, out var v) ? v : 0;

        var title = mode == "month" && month.HasValue
            ? $"REVENUE & DEBT - {year} (MONTH {month:00})"
            : $"REVENUE & DEBT - {year} ({mode.ToUpperInvariant()})";

        var lines = new List<string>
    {
        title,
        $"Total issued    : {data.TotalIssued:n0}",
        $"Total collected : {data.TotalRevenue:n0}",
        $"Outstanding debt: {data.TotalDebt:n0}",
        $"Collection rate : {(data.CollectionRate*100):0.#}%",
        "",
        "------------------------------------------------------------",
        (mode == "quarter"
            ? " Quarter |          Issued |       Collected |            Debt"
            : (mode == "year"
                ? "    Year |          Issued |       Collected |            Debt"
                : "   Month |          Issued |       Collected |            Debt")),
        "------------------------------------------------------------"
    };

        void AddLine(string label, decimal issued, decimal collected)
        {
            var debt = issued - collected;
            lines.Add($" {label,7} | {issued,15:n0} | {collected,15:n0} | {debt,15:n0}");
        }

        if (mode == "year")
        {
            AddLine(year.ToString(), data.TotalIssued, data.TotalRevenue);
        }
        else if (mode == "quarter")
        {
            for (int q = 1; q <= 4; q++)
            {
                int s = (q - 1) * 3 + 1;
                var issued = Issued(s) + Issued(s + 1) + Issued(s + 2);
                var collected = Collected(s) + Collected(s + 1) + Collected(s + 2);
                AddLine($"Q{q}", issued, collected);
            }
        }
        else // month
        {
            if (month is >= 1 and <= 12)
            {
                var issued = Issued(month.Value);
                var collected = Collected(month.Value);
                AddLine(month.Value.ToString(), issued, collected);

                lines.Add("------------------------------------------------------------");
                lines.Add($"   TOTAL | {issued,15:n0} | {collected,15:n0} | {(issued - collected),15:n0}");
                return SimplePdf.FromLines(lines);
            }

            for (int m = 1; m <= 12; m++)
                AddLine(m.ToString(), Issued(m), Collected(m));
        }

        lines.Add("------------------------------------------------------------");
        lines.Add($"   TOTAL | {data.TotalIssued,15:n0} | {data.TotalRevenue,15:n0} | {data.TotalDebt,15:n0}");

        return SimplePdf.FromLines(lines);
    }

    public Task GetRevenueAsync() => Task.CompletedTask;

    private static int? TryParseYear(string reportName)
    {
        var m = Regex.Match(reportName ?? "", @"\b(20\d{2})\b");
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    /// <summary>
    /// Minimal 1-page PDF writer with Helvetica.
    /// </summary>
    private static class SimplePdf
    {
        public static byte[] FromLines(IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("%PDF-1.4");

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            };

            var content = BuildTextContent(lines);
            objects.Add($"<< /Length {content.Length} >>\nstream\n{content}\nendstream");

            var xref = new List<int> { 0 };
            for (int i = 0; i < objects.Count; i++)
            {
                xref.Add(sb.Length);
                sb.AppendLine($"{i + 1} 0 obj");
                sb.AppendLine(objects[i]);
                sb.AppendLine("endobj");
            }

            var xrefStart = sb.Length;
            sb.AppendLine("xref");
            sb.AppendLine($"0 {objects.Count + 1}");
            sb.AppendLine("0000000000 65535 f ");
            for (int i = 1; i < xref.Count; i++)
                sb.AppendLine($"{xref[i]:D10} 00000 n ");

            sb.AppendLine("trailer");
            sb.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            sb.AppendLine("startxref");
            sb.AppendLine($"{xrefStart}");
            sb.AppendLine("%%EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static string BuildTextContent(IReadOnlyList<string> lines)
        {
            // Layout: start at top-left margin, line height 14
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 12 Tf");
            sb.AppendLine("72 790 Td"); // left margin=72, top=790

            foreach (var raw in lines)
            {
                var line = Escape(raw);
                sb.AppendLine($"({line}) Tj");
                sb.AppendLine("0 -14 Td");
            }

            sb.AppendLine("ET");
            return sb.ToString();
        }

        private static string Escape(string s)
            => (s ?? "").Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
    private static (int Year, string Mode, int? Month) ParseReportOptions(string reportName)
    {
        // Expected: Revenue-2026-month-2  | Revenue-2026-quarter-0 | Revenue-2026-year-0
        var m = Regex.Match(reportName ?? "", @"\b(20\d{2})\b(?:-(month|quarter|year))?(?:-(\d{1,2}))?",
            RegexOptions.IgnoreCase);

        var year = m.Success ? int.Parse(m.Groups[1].Value) : DateTime.Now.Year;

        var mode = "month";
        if (m.Success && m.Groups[2].Success)
        {
            var md = m.Groups[2].Value.ToLowerInvariant();
            if (md is "month" or "quarter" or "year") mode = md;
        }

        int? month = null;
        if (mode == "month" && m.Success && m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var mm) && mm is >= 1 and <= 12)
            month = mm;

        return (year, mode, month);
    }
}