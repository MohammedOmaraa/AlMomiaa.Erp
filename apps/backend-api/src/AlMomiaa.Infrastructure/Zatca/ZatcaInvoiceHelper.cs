using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AlMomiaa.Infrastructure.Zatca;

public static class ZatcaInvoiceHelper
{
    public const string SellerNameAr = "شركة المومياء للتجارة";
    public const string SellerVatNumber = "300000000000003";

    public static string InitialPreviousInvoiceHash =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("0")));

    public static decimal CalculateVat(decimal netAmount, decimal vatRatePercent = 15.00m) =>
        Math.Round(netAmount * (vatRatePercent / 100.0m), 2, MidpointRounding.AwayFromZero);

    public static string ComputeXmlHashBase64(
        string documentNumber,
        Guid zatcaUuid,
        long icv,
        string previousHash,
        DateTime issueDateUtc,
        decimal totalAmount,
        decimal vatAmount)
    {
        var canonicalPayload =
            $"<Invoice><ID>{documentNumber}</ID><UUID>{zatcaUuid:D}</UUID><ICV>{icv}</ICV><PIH>{previousHash}</PIH><IssueDate>{issueDateUtc:O}</IssueDate><Total>{totalAmount.ToString("F2", CultureInfo.InvariantCulture)}</Total><VAT>{vatAmount.ToString("F2", CultureInfo.InvariantCulture)}</VAT></Invoice>";
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload)));
    }

    public static string BuildTlvQrBase64(
        DateTime issueDateUtc,
        decimal totalWithVat,
        decimal vatTotal,
        string xmlHashBase64)
    {
        using var ms = new MemoryStream();
        WriteTlv(ms, 1, SellerNameAr);
        WriteTlv(ms, 2, SellerVatNumber);
        WriteTlv(ms, 3, issueDateUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        WriteTlv(ms, 4, totalWithVat.ToString("F2", CultureInfo.InvariantCulture));
        WriteTlv(ms, 5, vatTotal.ToString("F2", CultureInfo.InvariantCulture));
        WriteTlv(ms, 6, xmlHashBase64);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static void WriteTlv(Stream stream, byte tag, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        stream.WriteByte(tag);
        stream.WriteByte((byte)Math.Min(bytes.Length, byte.MaxValue));
        stream.Write(bytes, 0, Math.Min(bytes.Length, byte.MaxValue));
    }
}
