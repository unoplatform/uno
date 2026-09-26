namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Carries the fractional remainder of a wheel/trackpad delta across events.
/// </summary>
/// <remarks>
/// Hosts like Win32/X11 receive wheel deltas already accumulated into whole units by the OS. The
/// browser instead reports the raw fractional CSS-pixel delta on every event, so truncating each
/// event independently drops sub-unit input entirely (e.g. a stream of 0.9px trackpad deltas would
/// never scroll). Keeping the leftover fraction and adding it to the next delta preserves it instead.
/// </remarks>
internal struct WheelDeltaAccumulator
{
	private double _remainder;

	public int Accumulate(double delta)
	{
		_remainder += delta;
		var whole = (int)double.Truncate(_remainder);
		_remainder -= whole;
		return whole;
	}
}
