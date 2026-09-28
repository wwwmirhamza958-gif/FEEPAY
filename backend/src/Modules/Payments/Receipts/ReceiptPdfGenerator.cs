using System.Text.Json;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FeePlatform.Modules.Payments.Receipts;

public sealed class ReceiptPdfModel
{
    public required string InstitutionName { get; init; }
    public required long ReceiptNumber { get; init; }
    public required string AmountWire { get; init; }
    public required string Channel { get; init; }
    public required string? GatewayTxnId { get; init; }
    public required string StudentInitials { get; init; }
    public required DateTimeOffset PaidAtUtc { get; init; }
    public required string VerifyUrl { get; init; }
    public required IReadOnlyList<ReceiptLineModel> Lines { get; init; }
}

public sealed record ReceiptLineModel(string Description, string AmountWire);

/// <summary>QuestPDF receipt — no tax line (CONTRACT §8 / plan deferred tax).</summary>
public static class ReceiptPdfGenerator
{
    private static int _licenseConfigured;

    public static void EnsureCommunityLicense()
    {
        if (Interlocked.Exchange(ref _licenseConfigured, 1) == 1)
            return;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(ReceiptPdfModel model)
    {
        EnsureCommunityLicense();

        var qrPng = BuildQrPng(model.VerifyUrl);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Text(model.InstitutionName).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text("Payment receipt").FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Receipt no. {model.ReceiptNumber}").SemiBold();
                    col.Item().Text($"Amount: Rs {FormatWholeRupees(model.AmountWire)}");
                    col.Item().Text($"Channel: {model.Channel}");
                    if (!string.IsNullOrWhiteSpace(model.GatewayTxnId))
                        col.Item().Text($"Gateway ref: {model.GatewayTxnId}");
                    col.Item().Text($"Date: {model.PaidAtUtc:yyyy-MM-dd} (UTC)");
                    col.Item().Text($"Payer: {model.StudentInitials}");

                    col.Item().PaddingTop(12).Text("Lines").SemiBold();
                    foreach (var line in model.Lines)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(line.Description);
                            row.ConstantItem(100).AlignRight().Text($"Rs {FormatWholeRupees(line.AmountWire)}");
                        });
                    }

                    col.Item().PaddingTop(24).Width(120).Image(qrPng);
                    col.Item().Text("Scan to verify receipt").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text("Fee Platform — Option A direct settlement. No tax line.")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    public static string InitialsFromDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "—";

        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }

    public static string SerializeLines(IEnumerable<ReceiptLineModel> lines) =>
        JsonSerializer.Serialize(lines);

    public static IReadOnlyList<ReceiptLineModel> DeserializeLines(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ReceiptLineModel>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static byte[] BuildQrPng(string verifyUrl)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(verifyUrl, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(4);
    }

    private static string FormatWholeRupees(string wire)
    {
        if (!PaymentsWire.TryParseWire(wire, out var amount))
            return wire;
        var whole = decimal.Truncate(amount);
        return whole.ToString("N0");
    }
}
