using System;

namespace Windows.ApplicationModel.Background;

public partial class TimeTrigger : IBackgroundTrigger
{
	/// <summary>
	/// The shortest interval WinRT accepts when a time-triggered task is registered.
	/// </summary>
	internal const uint MinimumFreshnessTime = 15;

	public uint FreshnessTime { get; }

	public bool OneShot { get; }

	public TimeTrigger(uint freshnessTime, bool oneShot)
	{
		FreshnessTime = freshnessTime;
		OneShot = oneShot;
	}
}
