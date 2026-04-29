using System.Globalization;
using System.Text;

namespace ComiCal.Application.Common;

/// <summary>
/// 巻の keyset pagination 用カーソル <c>(ReleaseDate, VolumeId)</c> を Base64Url エンコード文字列で表現するヘルパー。
/// </summary>
public static class VolumeCursor
{
    /// <summary>カーソル文字列を生成する。</summary>
    public static string Encode(DateOnly releaseDate, Guid volumeId)
    {
        var raw = $"{releaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}|{volumeId:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>カーソル文字列を <c>(ReleaseDate, VolumeId)</c> に復元する。</summary>
    public static bool TryDecode(string? cursor, out DateOnly releaseDate, out Guid volumeId)
    {
        releaseDate = default;
        volumeId = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - (padded.Length % 4)) % 4);
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var parts = raw.Split('|', 2);
            if (parts.Length != 2)
            {
                return false;
            }
            if (!DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate))
            {
                return false;
            }
            return Guid.TryParse(parts[1], out volumeId);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
