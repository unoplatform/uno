#nullable enable

using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionBrush
	{
		/// <summary>
		/// Paints this brush onto a backend-neutral <see cref="IDrawingSession"/>. Returns true when the brush
		/// handled the paint (including "nothing to paint"); false only when the brush cannot paint at all.
		/// </summary>
		internal virtual bool TryPaint(IDrawingSession session, float opacity, Rect bounds) => false;

		internal virtual bool CanPaint() => false;

		internal virtual bool RequiresRepaintOnEveryFrame => false;
	}
}
