namespace Hamlet.Sim.Core;

/// <summary>
/// The sim-owned clock (§7.1). 1 tick = 1 sim minute by default; the sim never
/// samples engine frame time — wall-clock time only ever decides how many
/// ticks to run. All rates are data-driven candidates, never hardcoded policy.
/// </summary>
public sealed class Clock
{
    public const int MinutesPerHour = 60;
    public const int HoursPerDay = 24;
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int MinutesPerDay = MinutesPerHour * HoursPerDay; // 1440
    public const int TicksPerDay = MinutesPerDay;                  // 1 tick = 1 sim minute

    public long Tick { get; private set; }

    /// <summary>Whole sim days since the world started; the work-shift bookkeeping key.</summary>
    public long Day => Tick / TicksPerDay;

    public int MinuteOfDay => (int)(Tick % MinutesPerDay);
    public int Minute => MinuteOfDay % MinutesPerHour;
    public int Hour => MinuteOfDay / MinutesPerHour;
    public int DayOfMonth => (int)(Tick / TicksPerDay) % DaysPerMonth + 1;
    public int Month => (int)(Tick / ((long)TicksPerDay * DaysPerMonth)) % MonthsPerYear + 1;
    public long Year => Tick / ((long)TicksPerDay * DaysPerMonth * MonthsPerYear) + 1;

    public void Advance() => Tick++;
}
