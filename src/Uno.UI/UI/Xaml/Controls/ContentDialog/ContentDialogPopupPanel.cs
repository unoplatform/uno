#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class ContentDialogPopupPanel : PopupPanel
	{
		public ContentDialogPopupPanel(ContentDialog dialog) : base(dialog._popup)
		{
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
				var rect = CalculateDialogPlacement(desiredSize);

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().LogDebug($"Arranging ContentDialogPopupPanel {desiredSize} to {rect}");
				}

				elem.Arrange(rect);
			}

			return finalSize;
		}

		private Rect CalculateDialogPlacement(Size desiredSize)
		{
			var visibleBounds = ApplicationView.GetForCurrentView().TrueVisibleBounds;

			// Make sure the desiredSize fits in visibleBounds
			if (desiredSize.Width > visibleBounds.Width)
			{
				desiredSize.Width = visibleBounds.Width;
			}
			if (desiredSize.Height > visibleBounds.Height)
			{
				desiredSize.Height = visibleBounds.Height;
			}

			var  finalPosition = new Point(
						x: (visibleBounds.Width - desiredSize.Width) / 2.0,
						y: (visibleBounds.Height - desiredSize.Height) / 2.0);

			var finalRect = new Rect(finalPosition, desiredSize);

			return finalRect;
		}

		// The ContentDialog backdrop (aka 'smoke layer') doesn't light-dismiss, but does block pointer interactions. On WinUI this is implemented by adding
		// a second Popup containing a stretched Rectangle. To keep things simple, we just block pointers from passing from here.
		internal override bool IsViewHit() => true;
	}
}
