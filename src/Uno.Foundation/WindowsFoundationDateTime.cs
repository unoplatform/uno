#nullable enable

// See https://github.com/microsoft/CsWinRT/blob/2b77d8412adb7b62bf8f1265a6d8b8c3169dde58/src/WinRT.Runtime/Projections/SystemTypes.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Foundation;

internal struct WindowsFoundationDateTime
{
	// NOTE: 'Windows.Foundation.DateTime.UniversalTime' is a FILETIME value (relative to 01/01/1601), however
	// 'System.DateTimeOffset.Ticks' is relative to 01/01/0001
	public long UniversalTime;

	// Numer of ticks counted between 0001-01-01, 00:00:00 and 1601-01-01, 00:00:00.
	// You can get this through:  (new DateTimeOffset(1601, 1, 1, 0, 0, 1, TimeSpan.Zero)).Ticks;
	private const long ManagedUtcTicksAtNativeZero = 504911232000000000;
	// DO NOT use ToFileTime/FromFileTime, which don't support negative UniversalTime.

	public static implicit operator DateTimeOffset(WindowsFoundationDateTime value)
	{
		var utcTime = new DateTimeOffset(value.UniversalTime + ManagedUtcTicksAtNativeZero, TimeSpan.Zero);
		var offset = TimeZoneInfo.Local.GetUtcOffset(utcTime);
		long localTicks = utcTime.Ticks + offset.Ticks;
		if (localTicks < DateTime.MinValue.Ticks || localTicks > DateTime.MaxValue.Ticks)
		{
			throw new ArgumentOutOfRangeException();
		}
		return utcTime.ToLocalTime();
	}

	public static implicit operator WindowsFoundationDateTime(DateTimeOffset value)
	{
		return new WindowsFoundationDateTime { UniversalTime = value.UtcTicks - ManagedUtcTicksAtNativeZero };
	}

	public static implicit operator WindowsFoundationDateTime(DateTime value)
	{
		return new DateTimeOffset(value);
	}

	public static bool operator ==(WindowsFoundationDateTime left, WindowsFoundationDateTime right) => left.Equals(right);
	public static bool operator !=(WindowsFoundationDateTime left, WindowsFoundationDateTime right) => !(left == right);

	internal string ToString(string? format, IFormatProvider? formatProvider) => ((DateTimeOffset)this).ToString(format, formatProvider);

	internal int CompareTo(DateTimeOffset other) => ((DateTimeOffset)this).CompareTo(other);

	public override bool Equals(object? obj) => obj is WindowsFoundationDateTime time && UniversalTime == time.UniversalTime;

	public override int GetHashCode() => UniversalTime.GetHashCode();

	internal int Year => ((DateTimeOffset)this).Year;
	internal int Month => ((DateTimeOffset)this).Month;
	internal int Day => ((DateTimeOffset)this).Day;
	internal int Hour => ((DateTimeOffset)this).Hour;
	internal int Minute => ((DateTimeOffset)this).Minute;
	internal int Second => ((DateTimeOffset)this).Second;
	internal int Millisecond => ((DateTimeOffset)this).Millisecond;
}
