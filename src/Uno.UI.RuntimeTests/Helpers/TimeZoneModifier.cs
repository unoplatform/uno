using System;
using System.Reflection;

namespace Uno.UI.RuntimeTests.Helpers;

public class TimeZoneModifier : IDisposable
{
	private readonly TimeZoneInfo _originalTimeZoneInfo;

	public TimeZoneModifier(TimeZoneInfo timeZoneInfo)
	{
		_originalTimeZoneInfo = GetLocalTimeZone();
		SetLocalTimeZone(timeZoneInfo);
	}

	private static TimeZoneInfo GetLocalTimeZone()
	{
		var cachedData = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		return (TimeZoneInfo)cachedData.GetType().GetProperty("Local").GetValue(cachedData);
	}

	private static void SetLocalTimeZone(TimeZoneInfo timeZoneInfo)
	{
		var cachedData = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		cachedData.GetType().GetField("_localTimeZone", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cachedData, timeZoneInfo);
	}

	public void Dispose()
	{
		SetLocalTimeZone(_originalTimeZoneInfo);
	}
}
