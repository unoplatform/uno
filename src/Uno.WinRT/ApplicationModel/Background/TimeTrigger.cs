using System;

namespace Windows.ApplicationModel.Background;

public partial class TimeTrigger : IBackgroundTrigger
{
	public uint FreshnessTime { get; }

	public bool OneShot { get; }

	public TimeTrigger(uint freshnessTime, bool oneShot)
	{
		FreshnessTime = freshnessTime;
		OneShot = oneShot;
	}
}
