#nullable enable

using System.Numerics;
using SkiaSharp;

namespace Uno.UI.Composition
{
	internal interface ISizedBrush
	{
		internal bool IsSized { get; }
		internal Vector2? Size { get; }
	}
}
