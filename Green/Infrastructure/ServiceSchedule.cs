using System.Globalization;

namespace Green.Infrastructure;

public static class ServiceSchedule
{
    public static readonly TimeSpan OpeningTime = new(10, 0, 0);
    public static readonly TimeSpan ClosingTime = new(18, 0, 0);
    public const int SlotMinutes = 30;

    public static bool IsWorkingDay(DateTime date) => date.DayOfWeek != DayOfWeek.Sunday;

    public static bool IsValidAppointment(DateTime value)
    {
        var time = value.TimeOfDay;
        return IsWorkingDay(value)
            && time >= OpeningTime
            && time < ClosingTime
            && time.Minutes % SlotMinutes == 0
            && time.Seconds == 0;
    }

    public static IEnumerable<DateTime> GetSlots(DateTime date)
    {
        if (!IsWorkingDay(date))
        {
            return Enumerable.Empty<DateTime>();
        }

        var start = date.Date.Add(OpeningTime);
        var end = date.Date.Add(ClosingTime);
        return Enumerable.Range(0, (int)((end - start).TotalMinutes / SlotMinutes))
            .Select(i => start.AddMinutes(i));
    }

    public static string ToInputValue(DateTime value) => value.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
}
