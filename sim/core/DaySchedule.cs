namespace Hamlet.Sim.Core;

/// <summary>
/// The clock-keyed behaviour windows (§7.1): when citizens wake, sleep, and
/// work. Every hour threshold lives here, never inside the agent atoms, so the
/// day stays data-driven and one balance pass can move the whole schedule.
/// </summary>
public sealed class DaySchedule
{
    public static DaySchedule Default { get; } = new();

    public int WakeHour { get; init; } = 7;
    public int NightStartHour { get; init; } = 21;
    public int ShiftStartHour { get; init; } = 9;
    public int ShiftEndHour { get; init; } = 17;
    public int ShopOpenHour { get; init; } = 8;
    public int ShopCloseHour { get; init; } = 20;

    /// <summary>Night runs from the night hour through to (but excluding) the wake hour.</summary>
    public bool IsNight(int hour) => hour >= NightStartHour || hour < WakeHour;

    public bool IsShift(int hour) => hour >= ShiftStartHour && hour < ShiftEndHour;

    public bool IsShopOpen(int hour) => hour >= ShopOpenHour && hour < ShopCloseHour;
}
