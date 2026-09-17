using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

public partial class UIElement
{
	/// <summary>
	/// Called before the BringIntoViewRequested event occurs.
	/// </summary>
	/// <param name="e">Event args.</param>
	protected virtual void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs e)
	{
	}

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

		if (Visibility == Visibility.Collapsed || this is FrameworkElement { IsLoaded: false })
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


		RaiseBringIntoViewRequested(
			targetRect,
			forceIntoView: false,
			options.AnimationDesired,
			interruptDuringManipulation: true,
			options.HorizontalAlignmentRatio,
			options.VerticalAlignmentRatio,
			options.HorizontalOffset,
			options.VerticalOffset);
	}

	internal void BringIntoView(
		Rect targetRect,
		bool forceIntoView,
		bool useAnimation,
		bool interruptDuringManipulation,
		double horizontalAlignmentRatio,
		double verticalAlignmentRatio,
		double offsetX,
		double offsetY) =>
		RaiseBringIntoViewRequested(
			targetRect,
			forceIntoView,
			useAnimation,
			interruptDuringManipulation,
			horizontalAlignmentRatio,
			verticalAlignmentRatio,
			offsetX,
			offsetY);

	private void RaiseBringIntoViewRequested(
		Rect targetRect,
		bool forceIntoView,
		bool useAnimation,
		bool interruptDuringManipulation,
		double horizontalAlignmentRatio,
		double verticalAlignmentRatio,
		double offsetX,
		double offsetY)
	{
		var args = new BringIntoViewRequestedEventArgs
		{
			AnimationDesired = useAnimation,
			ForceIntoView = forceIntoView,
			InterruptDuringManipulation = interruptDuringManipulation,
			HorizontalOffset = offsetX,
			VerticalOffset = offsetY,
			HorizontalAlignmentRatio = horizontalAlignmentRatio,
			VerticalAlignmentRatio = verticalAlignmentRatio,
			TargetRect = targetRect,
			OriginalSource = this,
			TargetElement = this,
		};

		RaiseEvent(BringIntoViewRequestedEvent, args);
	}
}
