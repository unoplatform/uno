#nullable enable

using System;
using System.Linq;
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
			_text = text;

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
