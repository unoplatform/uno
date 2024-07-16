using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Rect = Windows.Foundation.Rect;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _owner;
	private Rect? _physicalRect;

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
			_physicalRect = physicalRect;
		}
	}

	public void AttachNativeElement(object content)
	{
		if (content is View view)
		{
			ApplicationActivity.Instance.NativeLayerHost.AddView(view);

			if (_physicalRect is { } physicalRect)
			{
				view.Layout(
					(int)physicalRect.Left,
					(int)physicalRect.Top,
					(int)physicalRect.Right,
					(int)physicalRect.Bottom);
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is View view)
		{
			ApplicationActivity.Instance.NativeLayerHost.RemoveView(view);
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
		return new TextView(ApplicationActivity.Instance.NativeLayerHost.Context)
		{
			Text = text
		};
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
