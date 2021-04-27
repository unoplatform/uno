#nullable enable

using System;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Android.Graphics;
using Android.Text;

namespace Uno.UI.Composition
{
	internal class NativeTextLayoutVisual : Visual
	{
		private Layout? _text;

		public NativeTextLayoutVisual(Layout? text, UIContext context)
			: base(context.Compositor)
		{
			Text = text;
			Kind = VisualKind.NativeDependent;
		}

		public Layout? Text
		{
			get => _text;
			set
			{
				if (_text == value)
				{
					return;
				}

				_text = value;
				Size = value is null
					? Vector2.Zero
					: new Vector2(value.Width, value.Height);

				Invalidate(VisualDirtyState.Dependent);
			}
		}

		/// <inheritdoc />
		private protected override void RenderDependent(Canvas canvas)
		{
			base.RenderDependent(canvas);

			_text?.Draw(canvas);
		}
	}
}
