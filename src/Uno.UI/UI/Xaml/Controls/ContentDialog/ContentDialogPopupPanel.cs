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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Windows.UI.Xaml.Controls
{
	internal partial class ContentDialogPopupPanel : PopupPanel
	{
		public ContentDialogPopupPanel(ContentDialog dialog) : base(dialog._popup)
		{
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var actualAvailableSize = CalculateDialogAvailableSize(availableSize);
			return base.MeasureOverride(actualAvailableSize);
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

		private Size CalculateDialogAvailableSize(Size availableSize)
		{
			// Skip calculation if in the context of Uno Islands.
			if (!CoreApplication.IsFullFledgedApp)
			{
				return availableSize;
			}

			var visibleBounds = XamlRoot?.VisualTree.TrueVisibleBounds ?? default;

			if (availableSize.Width > visibleBounds.Width)
			{
				availableSize.Width = visibleBounds.Width;
			}
			if (availableSize.Height > visibleBounds.Height)
			{
				availableSize.Height = visibleBounds.Height;
			}

			return availableSize;
		}

		private Rect CalculateDialogPlacement(Size desiredSize, Size finalSize)
		{
			Rect visibleBounds = XamlRoot?.VisualTree.TrueVisibleBounds ?? default;

			var maximumWidth = Math.Min(visibleBounds.Width, finalSize.Width);
			var maximumHeight = Math.Min(visibleBounds.Height, finalSize.Height);

			// Make sure the desiredSize fits in visibleBounds
			var actualWidth = Math.Min(desiredSize.Width, maximumWidth);
			var actualHeight = Math.Min(desiredSize.Height, maximumHeight);

			var xOffset = Math.Max((maximumWidth - actualWidth) / 2, visibleBounds.X);
			var yOffset = Math.Max((maximumHeight - actualHeight) / 2, visibleBounds.Y);

			var finalRect = new Rect(
				xOffset,
				yOffset,
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
