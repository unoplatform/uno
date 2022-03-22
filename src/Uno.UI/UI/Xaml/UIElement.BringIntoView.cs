using System;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml;

public partial class UIElement
{
	/// <summary>
	/// Initiates a request to the XAML framework to bring the element into view within any scrollable regions it is contained within.
	/// </summary>
	public void StartBringIntoView()
	{
		var bringIntoViewOptions = new BringIntoViewOptions()
		{
			AnimationDesired = true,
		};

		StartBringIntoView(bringIntoViewOptions);
	}

	/// <summary>
	/// Initiates a request to the XAML framework to bring the element into view using the specified options.
	/// </summary>
	/// <param name="options">Options.</param>
	public void StartBringIntoView(BringIntoViewOptions options)
	{
		if (options is null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		var isHorizontalRatioValid =
			double.IsNaN(options.HorizontalAlignmentRatio) ||
			(options.HorizontalAlignmentRatio >= 0 && options.HorizontalAlignmentRatio <= 1);

		var isVerRatioValid =
			double.IsNaN(options.HorizontalAlignmentRatio) ||
			(options.HorizontalAlignmentRatio >= 0 && options.HorizontalAlignmentRatio <= 1);

		if (!IsLoaded && Visibility == Visibility.Visible)
		{
			// Element must be loaded and visible for bring into view.
			return;
		}

		Rect targetRect;
		if (options.TargetRect != null)
		{
			targetRect = options.TargetRect.Value;
		}
		else
		{
			targetRect = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
		}

#if __IOS__ || __ANDROID__
		Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			// This currently doesn't support nested scrolling.
			// This currently doesn't support BringIntoViewOptions.AnimationDesired.
			var scrollContentPresenter = this.FindFirstParent<ScrollContentPresenter>();
			scrollContentPresenter?.MakeVisible(this, options.TargetRect ?? Rect.Empty);
		});
#else
		Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
		{
			var scrollContentPresenter = this.FindFirstParent<ScrollContentPresenter>();
			scrollContentPresenter?.MakeVisible(this, options.TargetRect ?? Rect.Empty);
		});
#endif
	}
}
