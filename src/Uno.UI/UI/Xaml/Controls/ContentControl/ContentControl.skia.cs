using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentControl
	{
		partial void RegisterContentTemplateRoot()
		{
			AddChild(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild(ContentTemplateRoot);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			// This is the same as Control.MeasureOverride, except that we use Content if FindFirstChild is null.
			// The fact that FindFirstChild is null is actually a BUG!
			// The way this works in WinUI is:
			// 1. FrameworkElement.MeasureCore calls 'InvokeApplyTemplate'
			// 2. ContentControl overrides ApplyTemplate to do a bunch of stuff, including a call to CreateDefaultVisuals.
			// 3. CreateDefaultVisuals adds the Content as Child if it's a UIElement.
			// Tracking issue: https://github.com/unoplatform/uno/issues/190
			var child = this.FindFirstChild() ?? (Content as UIElement);
			return child != null ? MeasureElement(child, availableSize) : new Size(0, 0);
		}
	}
}
