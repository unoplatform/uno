using System.Threading;

namespace Uno.UI.Runtime.Skia.Wpf.Input
{
	internal static class FrameIdProvider
	{
		private static int _currentFrameId;

		internal static uint GetNextFrameId() =>
			(uint)Interlocked.Increment(ref _currentFrameId);
	}
}
