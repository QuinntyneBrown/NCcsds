namespace NCcsds.Encoding.Time;

/// <summary>
/// Constants and utilities for CCSDS time formats.
/// </summary>
public static class CcsdsTime
{
    /// <summary>
    /// CCSDS epoch: January 1, 1958, 00:00:00 UTC.
    /// </summary>
    public static readonly DateTime CcsdsEpoch = new(1958, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// TAI epoch: January 1, 1958, 00:00:00 TAI (same as CCSDS epoch).
    /// </summary>
    public static readonly DateTime TaiEpoch = CcsdsEpoch;

    /// <summary>
    /// Unix epoch: January 1, 1970, 00:00:00 UTC.
    /// </summary>
    public static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// GPS epoch: January 6, 1980, 00:00:00 UTC.
    /// </summary>
    public static readonly DateTime GpsEpoch = new(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Converts a DateTime to seconds since the CCSDS epoch.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>Seconds since CCSDS epoch.</returns>
    public static double ToSecondsSinceCcsdsEpoch(DateTime dateTime)
    {
        return (dateTime.ToUniversalTime() - CcsdsEpoch).TotalSeconds;
    }

    /// <summary>
    /// Converts seconds since CCSDS epoch to DateTime.
    /// </summary>
    /// <param name="seconds">Seconds since CCSDS epoch.</param>
    /// <returns>The DateTime (UTC).</returns>
    public static DateTime FromSecondsSinceCcsdsEpoch(double seconds)
    {
        return CcsdsEpoch.AddSeconds(seconds);
    }

    /// <summary>
    /// Converts a DateTime to days and milliseconds of day.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <param name="epoch">The epoch to use.</param>
    /// <param name="days">The day count from epoch.</param>
    /// <param name="millisOfDay">Milliseconds within the day.</param>
    public static void ToDayAndMillis(DateTime dateTime, DateTime epoch, out int days, out uint millisOfDay)
    {
        var utc = dateTime.ToUniversalTime();
        var elapsed = utc - epoch;
        days = (int)elapsed.TotalDays;
        millisOfDay = (uint)((elapsed.TotalDays - days) * 86400000);
    }

    /// <summary>
    /// Converts days and milliseconds of day to DateTime.
    /// </summary>
    /// <param name="epoch">The epoch to use.</param>
    /// <param name="days">The day count from epoch.</param>
    /// <param name="millisOfDay">Milliseconds within the day.</param>
    /// <returns>The DateTime (UTC).</returns>
    public static DateTime FromDayAndMillis(DateTime epoch, int days, uint millisOfDay)
    {
        return epoch.AddDays(days).AddMilliseconds(millisOfDay);
    }
}
