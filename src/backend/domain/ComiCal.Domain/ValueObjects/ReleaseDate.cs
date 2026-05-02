namespace ComiCal.Domain.ValueObjects;

public sealed record ReleaseDate
{
    public DateOnly? Date { get; }
    public bool IsMonthOnly { get; }

    private ReleaseDate(DateOnly? date, bool isMonthOnly)
    {
        Date = date;
        IsMonthOnly = isMonthOnly;
    }

    public static ReleaseDate Tbd() => new(null, false);

    public static ReleaseDate FromDate(DateOnly date) => new(date, false);

    public static ReleaseDate FromYearMonth(int year, int month)
    {
        var lastDay = DateTime.DaysInMonth(year, month);
        return new ReleaseDate(new DateOnly(year, month, lastDay), true);
    }

    public string Display()
    {
        if (Date is null) return "未定";
        return IsMonthOnly
            ? Date.Value.ToString("yyyy年M月", System.Globalization.CultureInfo.CurrentCulture)
            : Date.Value.ToString("yyyy年M月d日", System.Globalization.CultureInfo.CurrentCulture);
    }

    public override string ToString() => Display();
}
