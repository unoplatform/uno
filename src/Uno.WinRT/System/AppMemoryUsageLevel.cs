#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System;

public enum AppMemoryUsageLevel
{
	Low = 0,
	Medium = 1,
	High = 2,
	OverLimit = 3,
}
