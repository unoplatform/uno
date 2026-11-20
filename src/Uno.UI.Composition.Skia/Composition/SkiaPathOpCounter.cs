#nullable enable

using System.Threading;

namespace Microsoft.UI.Composition;

/// <summary>
/// Counts the path booleans (SKPath.Op) the Skia drawing backend runs. Runtime tests take deltas of <see cref="Count"/>
/// to budget the path work a frame does: an Op per clipped visual per frame is what made 6.7 scrolling slow.
/// </summary>
internal static class SkiaPathOpCounter
{
	private static int _count;

	internal static int Count => Volatile.Read(ref _count);

	internal static void Increment() => Interlocked.Increment(ref _count);
}
