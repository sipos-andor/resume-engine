using System.Globalization;
using System.Text;
using QRCoder;

namespace Sipos.Resume.Documents.Pdf.Marks;

/// <summary>A QR code as one SVG path, without its quiet zone; the layout leaves the white space around it.</summary>
/// <remarks>
/// Decision: the code is drawn as vector paths from QRCoder's module matrix.
/// Why: a raster code blurs when printed or zoomed; one path per row run keeps the PDF small and sharp.
/// </remarks>
internal static class QrCode
{
    /// <summary>Encodes an address with medium error correction.</summary>
    /// <param name="url">The address.</param>
    /// <param name="color">The colour of the dark modules, six hex digits.</param>
    public static string Svg(Uri url, string color)
    {
        ArgumentNullException.ThrowIfNull(url);
        using var data = QRCodeGenerator.GenerateQrCode(url.AbsoluteUri, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;

        // The matrix carries a quiet zone; the finder patterns sit in three corners, so the dark modules span the symbol.
        var rows = Enumerable.Range(0, matrix.Count).Where(row => Dark(matrix[row]).Any()).ToList();
        var columns = rows.SelectMany(row => Dark(matrix[row])).ToList();
        var (top, left) = (rows.Min(), columns.Min());
        var size = rows.Max() - top + 1;

        var path = new StringBuilder();
        foreach (var row in rows)
        {
            var line = matrix[row];
            for (var column = 0; column < line.Length; column++)
            {
                if (!line[column])
                {
                    continue;
                }

                var start = column;
                while (column + 1 < line.Length && line[column + 1])
                {
                    column++;
                }

                path.Append(CultureInfo.InvariantCulture, $"M{start - left} {row - top}h{column - start + 1}v1h-{column - start + 1}z");
            }
        }

        return $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {size} {size}" shape-rendering="crispEdges"><path fill="#{color}" d="{path}"/></svg>""";
    }

    private static IEnumerable<int> Dark(System.Collections.BitArray row) => Enumerable.Range(0, row.Length).Where(index => row[index]);
}
