using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _owner;

	public AndroidSkiaNativeElementHostingExtension(ContentPresenter owner)
	{
		_owner = owner;
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		if (content is View view)
		{
			var physicalRect = arrangeRect.LogicalToPhysicalPixels();
			view.Layout(
				(int)physicalRect.Left,
				(int)physicalRect.Top,
				(int)physicalRect.Right,
				(int)physicalRect.Bottom);

			var physicalClipRect = clipRect.LogicalToPhysicalPixels();
			view.ClipBounds = new global::Android.Graphics.Rect(
				(int)physicalClipRect.Left,
				(int)physicalClipRect.Top,
				(int)physicalClipRect.Right,
				(int)physicalClipRect.Bottom);
		}
	}

	public void AttachNativeElement(object content)
	{
		if (content is View view)
		{
			view.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.MatchParent);

			ApplicationActivity.Instance.RelativeLayout.AddView(view);
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is View view)
		{
			view.Alpha = (float)opacity;
		}
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		if (content is View view)
		{
			view.Visibility = visible ? ViewStates.Visible : ViewStates.Invisible;
		}
	}

	public object CreateSampleComponent(string text)
	{
		return new EditText(ApplicationActivity.Instance.RelativeLayout.Context)
		{
			Text = text
		};
	}

	public void DetachNativeElement(object content)
	{
		if (content is View view)
		{
			ApplicationActivity.Instance.AddContentView(view, null);
		}
	}

	public bool IsNativeElement(object content) => content is View;

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is View view)
		{
			var availablePhysical = availableSize.LogicalToPhysicalPixels();
			view.Measure((int)availablePhysical.Width, (int)availablePhysical.Height);
			return new Size(view.MeasuredWidth, view.MeasuredHeight).PhysicalToLogicalPixels();
		}

		return default;
	}
}
