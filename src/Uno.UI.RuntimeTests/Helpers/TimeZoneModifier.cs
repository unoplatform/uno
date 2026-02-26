using System;
using System.Diagnostics.CodeAnalysis;
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
		var localProperty = GetLocalProperty(cachedData);
		if (localProperty is null)
		{
			throw new Exception("Local property wasn't found");
		}

		return (TimeZoneInfo)localProperty.GetValue(cachedData);

		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "`t.GetProperty()` path should be impossible, and trimmer behavior ensures `typeof(TimeZoneInfo).GetProperty(string constant)` works.")]
		static PropertyInfo GetLocalProperty(object data)
		{
			if (data.GetType() is var t && t != typeof(TimeZoneInfo))
			{
				return t.GetProperty("Local");
			}
			else
			{
				return typeof(TimeZoneInfo).GetProperty("Local");
			}
		}
	}

	private static void SetLocalTimeZone(TimeZoneInfo timeZoneInfo)
	{
		var cachedData = GetCachedData();
		var localTimeZoneField = GetLocalField(cachedData);
		if (localTimeZoneField is null)
		{
			// At the time of writing, the field is found here:
			// https://github.com/dotnet/runtime/blob/91ee24a97d907e213e181a855b8469524cd376b5/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs#L71
			throw new Exception("_localTimeZone field wasn't found");
		}

		localTimeZoneField.SetValue(cachedData, timeZoneInfo);

		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "`t.GetField()` path should be impossible, and trimmer behavior ensures `typeof(TimeZoneInfo).GetField(string constant)` works.")]
		static FieldInfo GetLocalField(object data)
		{
			if (data.GetType() is var t && t != typeof(TimeZoneInfo))
			{
				return t.GetField("_localTimeZone", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			else
			{
				return typeof(TimeZoneInfo).GetField("_localTimeZone", BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}
	}

	public void Dispose()
	{
		SetLocalTimeZone(_originalTimeZoneInfo);
	}
}
