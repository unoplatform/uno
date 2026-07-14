#nullable enable

using System.Numerics;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition
{
	internal interface ISkiaSurface
	{
		internal void Paint(IDrawingSession session, float opacity);
		internal Vector2 Size { get; }
	}
}
