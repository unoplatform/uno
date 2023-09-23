#nullable enable

using System.Numerics;

namespace Uno.UI.Composition
{
	internal interface ISizedBrush
	{
		internal bool IsSized { get; }
		internal Vector2? Size { get; }
	}
}
