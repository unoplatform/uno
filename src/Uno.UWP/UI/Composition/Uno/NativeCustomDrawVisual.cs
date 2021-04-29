#nullable enable

using System;
using System.Linq;
using Windows.UI;
using Windows.UI.Composition;
using Android.Graphics;

namespace Uno.UI.Composition
{
	/// <summary>
	/// A <see cref="Visual"/> which allow user to create provide its own draw method to draw content directly on a Android <see cref="Canvas"/>.
	/// </summary>
	internal sealed class NativeCustomDrawVisual : Visual
	{
		private readonly DrawCallback _draw;

		/// <summary>
		/// A callback used to draw the content (WARNING cf. Remarks).
		/// </summary>
		/// <remarks>
		/// Be aware that this callback will usually be invoked on a background thread.
		/// You must not access any dependency property, and implement a concurrency pattern.
		/// </remarks>
		/// <param name="canvas">The canvas on which draw the content.</param>
		public delegate void DrawCallback(Canvas canvas);

		public NativeCustomDrawVisual(DrawCallback draw, UIContext context)
			: base(context.Compositor)
		{
			_draw = draw;
		}

		/// <inheritdoc />
		private protected override void RenderDependent(Canvas canvas)
		{
			base.RenderDependent(canvas);

			_draw(canvas);
		}

		/// <summary>
		/// Invalidates the last draw.
		/// </summary>
		public void Invalidate()
			=> base.Invalidate(CompositionPropertyType.Dependent);
	}
}
