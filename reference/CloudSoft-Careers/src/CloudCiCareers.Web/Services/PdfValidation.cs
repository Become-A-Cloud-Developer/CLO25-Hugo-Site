namespace CloudCiCareers.Web.Services;

public static class PdfValidation
{
    private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 };

    public static bool IsPdf(Stream s)
    {
        if (s is null || !s.CanRead)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[4];
        var read = s.Read(header);

        if (s.CanSeek)
        {
            s.Position = 0;
        }

        if (read < 4)
        {
            return false;
        }

        return header.SequenceEqual(PdfSignature);
    }
}
