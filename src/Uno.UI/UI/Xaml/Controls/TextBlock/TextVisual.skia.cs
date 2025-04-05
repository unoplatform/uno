using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Composition;

#nullable enable

namespace Microsoft.UI.Composition
{
	internal class TextVisual : Visual
	{
		private readonly WeakReference<TextBlock> _owner;
		private readonly bool _isInsideTextBox;

		internal TextVisual(Compositor compositor, TextBlock owner, bool isInsideTextBox = false) : base(compositor)
		{
			_owner = new WeakReference<TextBlock>(owner);
			_isInsideTextBox = isInsideTextBox;
		}

		internal override void Paint(in PaintingSession session)
		{
			if (_owner.TryGetTarget(out var owner))
			{
				owner.Inlines.Draw(in session);
			}
		}

		internal override bool CanPaint() => true;

		internal override bool RequiresRepaintOnEveryFrame => _isInsideTextBox;
	}
}
