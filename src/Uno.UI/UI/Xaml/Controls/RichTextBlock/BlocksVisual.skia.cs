using System;
using Windows.UI.Xaml.Controls;
using Uno.UI.Composition;

#nullable enable

namespace Windows.UI.Composition
{
	internal class BlocksVisual : Visual
	{
		private readonly WeakReference<RichTextBlock> _owner;

		public BlocksVisual(Compositor compositor, RichTextBlock owner) : base(compositor)
		{
			_owner = new WeakReference<RichTextBlock>(owner);
		}

		internal override void Draw(in DrawingSession session)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.Blocks.Draw(in session);
			}
		}
	}
}
