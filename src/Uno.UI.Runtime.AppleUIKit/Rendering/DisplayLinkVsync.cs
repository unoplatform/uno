#nullable enable

using System;
using System.Diagnostics;
using CoreAnimation;

namespace Uno.UI.Runtime.AppleUIKit;

internal static class DisplayLinkVsync
{
	/// <summary>The <see cref="Stopwatch.GetTimestamp"/> time of the vsync that started the link's current frame.</summary>
	/// <remarks>
	/// Converted through its age on the link's own clock, so nothing assumes how that clock relates to Stopwatch's.
	/// </remarks>
	public static long GetTimestamp(CADisplayLink link)
	{
		var ageInSeconds = Math.Max(0, CAAnimation.CurrentMediaTime() - link.Timestamp);
		return Stopwatch.GetTimestamp() - (long)(ageInSeconds * Stopwatch.Frequency);
	}
}
