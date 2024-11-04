#nullable enable

using System.Threading;

namespace Uno.UI.Runtime.Skia.Win32.Input
{
	internal static class FrameIdProvider
	{
		private static int _currentFrameId;

		internal static uint GetNextFrameId() =>
			(uint)Interlocked.Increment(ref _currentFrameId);
	}
}
