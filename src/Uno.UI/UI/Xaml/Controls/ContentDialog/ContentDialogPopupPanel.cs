#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class ContentDialogPopupPanel : PopupPanel
	{
		public ContentDialogPopupPanel(ContentDialog dialog) : base(dialog._popup)
		{
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().LogDebug($"ArrangeOverride ContentDialogPopupPanel {finalSize}");
			}

			foreach (var child in Children)
			{
				if (!(child is UIElement elem))
				{
					continue;
				}

				var desiredSize = elem.DesiredSize;
				var rect = CalculateDialogPlacement(desiredSize, finalSize);

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().LogDebug($"Arranging ContentDialogPopupPanel {desiredSize} to {rect}");
				}

				elem.Arrange(rect);
			}

			return finalSize;
		}

		private Rect CalculateDialogPlacement(Size desiredSize, Size finalSize)
		{
			var actualWidth = Math.Min(desiredSize.Width, finalSize.Width);
			var actualHeight = Math.Min(desiredSize.Height, finalSize.Height);

			var finalRect = new Rect(
				(finalSize.Width - actualWidth) / 2,
				(finalSize.Height - actualHeight) / 2,
				actualWidth,
				actualHeight
			);

			return finalRect;
		}

		// The ContentDialog backdrop (aka 'smoke layer') doesn't light-dismiss, but does block pointer interactions. On WinUI this is implemented by adding
		// a second Popup containing a stretched Rectangle. To keep things simple, we just block pointers from passing from here.
		internal override bool IsViewHit() => true;
	}
}
