using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Android.App;
using System;
using System.Numerics;
using Uno.Foundation.Logging;
using Rect = Windows.Foundation.Rect;
using Size = Windows.Foundation.Size;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _owner;

	public AndroidSkiaNativeElementHostingExtension(ContentPresenter owner)
	{
		_owner = owner;
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect)
	{
		if (content is View view)
		{
			var logicalRect = TransformToBounds(
				UIElement.GetTransform(from: _owner, to: null),
				arrangeRect);
			var physicalRect = logicalRect.LogicalToPhysicalPixels();
			var lp = new RelativeLayout.LayoutParams((int)physicalRect.Width, (int)physicalRect.Height)
			{
				LeftMargin = (int)physicalRect.Left,
				TopMargin = (int)physicalRect.Top,
				AlignWithParent = true
			};
			view.LayoutParameters = lp;
		}
	}

	private static Rect TransformToBounds(Matrix3x2 transform, Rect rect)
	{
		var topLeft = Vector2.Transform(new Vector2((float)rect.Left, (float)rect.Top), transform);
		var topRight = Vector2.Transform(new Vector2((float)rect.Right, (float)rect.Top), transform);
		var bottomLeft = Vector2.Transform(new Vector2((float)rect.Left, (float)rect.Bottom), transform);
		var bottomRight = Vector2.Transform(new Vector2((float)rect.Right, (float)rect.Bottom), transform);
		var left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
		var top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
		var right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
		var bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
		return new Rect(left, top, right - left, bottom - top);
	}

	public void AttachNativeElement(object content)
	{
		if (content is View view)
		{
			if (ApplicationActivity.NativeLayerHost is { } host)
			{
				host.AddView(view);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Cannot attach native element because {nameof(ApplicationActivity.Instance.NativeLayerHost)} is null.");
				}
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is View view)
		{
			if (ApplicationActivity.NativeLayerHost is { } host)
			{
				host.RemoveView(view);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Cannot detach native element because {nameof(ApplicationActivity.Instance.NativeLayerHost)} is null.");
				}
			}
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is View view)
		{
			view.Alpha = (float)opacity;
		}
	}

	public object? CreateSampleComponent(string text)
	{
		if (ApplicationActivity.NativeLayerHost is not { } host)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Cannot create a sample native element because {nameof(ApplicationActivity.NativeLayerHost)} is null.");
			}

			return null;
		}

		var btn = new global::Android.Widget.Button(host.Context)
		{
			Text = text
		};

		btn.Click += (_, _) =>
		{
			var builder = new AlertDialog.Builder(host.Context);
			var dialog = builder.SetTitle("Button clicked")!.SetMessage($"Button {text} clicked!")!.Create();
			dialog!.Show();
		};

		return btn;
	}

	public bool IsNativeElement(object content) => content is View;

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is View view)
		{
			var availablePhysical = availableSize.LogicalToPhysicalPixels();

			// Note: View.Measure(widthMeasureSpec, heightMeasureSpec) doesn't take "raw" sizes,
			// it instead takes a "MeasureSpec" which is 2 bits of "mode" and 30 bits of "size".
			// As e.g. availablePhysical.Width could be int.MaxValue -- when availableSize.Width is Infinite --
			// then availablePhysical.Width could *exceed* 30 bits.
			// Using MakeMeasureSpec() ensures that the size we specify doesn't overflow into "mode".
			int widthMeasureSpec = View.MeasureSpec.MakeMeasureSpec((int)availablePhysical.Width, MeasureSpecMode.Unspecified);
			int heightMeasureSpec = View.MeasureSpec.MakeMeasureSpec((int)availablePhysical.Height, MeasureSpecMode.Unspecified);
			view.Measure(widthMeasureSpec, heightMeasureSpec);
			return new Size(view.MeasuredWidth, view.MeasuredHeight).PhysicalToLogicalPixels();
		}

		return default;
	}
}
