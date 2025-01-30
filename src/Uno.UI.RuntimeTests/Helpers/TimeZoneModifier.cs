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

	private static object GetCachedData()
	{
		var cachedDataField = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.Static | BindingFlags.NonPublic);
		if (cachedDataField is null)
		{
			// At the time of writing, the field is found here:
			// https://github.com/dotnet/runtime/blob/91ee24a97d907e213e181a855b8469524cd376b5/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs#L61
			throw new Exception("s_cachedData field wasn't found");
		}

		return cachedDataField.GetValue(null);
	}

	private static TimeZoneInfo GetLocalTimeZone()
	{
		var cachedData = GetCachedData();
		var localProperty = cachedData.GetType().GetProperty("Local");
		if (localProperty is null)
		{
			// At the time of writing, the property is found here:
			// https://github.com/dotnet/runtime/blob/91ee24a97d907e213e181a855b8469524cd376b5/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs#L101
			throw new Exception("Local property wasn't found");
		}

		return (TimeZoneInfo)localProperty.GetValue(cachedData);
	}

	private static void SetLocalTimeZone(TimeZoneInfo timeZoneInfo)
	{
		var cachedData = GetCachedData();
		var localTimeZoneField = cachedData.GetType().GetField("_localTimeZone", BindingFlags.Instance | BindingFlags.NonPublic);
		if (localTimeZoneField is null)
		{
			// At the time of writing, the field is found here:
			// https://github.com/dotnet/runtime/blob/91ee24a97d907e213e181a855b8469524cd376b5/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs#L71
			throw new Exception("_localTimeZone field wasn't found");
		}

		localTimeZoneField.SetValue(cachedData, timeZoneInfo);
	}

	public void Dispose()
	{
		SetLocalTimeZone(_originalTimeZoneInfo);
	}
}
