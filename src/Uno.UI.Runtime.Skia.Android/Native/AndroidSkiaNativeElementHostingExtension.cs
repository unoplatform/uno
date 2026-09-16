using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Android.App;
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
			var physicalRect = arrangeRect.LogicalToPhysicalPixels();
			var lp = new RelativeLayout.LayoutParams((int)physicalRect.Width, (int)physicalRect.Height)
			{
				LeftMargin = (int)physicalRect.Left,
				TopMargin = (int)physicalRect.Top,
				AlignWithParent = true
			};
			view.LayoutParameters = lp;
		}
	}

	// Attached views and their presenter, so an activity taking over a window can adopt them.
	private static readonly Dictionary<View, ContentPresenter> _attachedViews = new(ReferenceEqualityComparer.Instance);

	private ApplicationActivity.ClippedRelativeLayout? NativeLayerHost
		=> AndroidSkiaXamlRootHost.GetActivity(_owner.XamlRoot)?.NativeLayerHost;

	public void AttachNativeElement(object content)
	{
		if (content is View view)
		{
			if (NativeLayerHost is { } host)
			{
				host.AddView(view);
				_attachedViews[view] = _owner;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Cannot attach native element because {nameof(ApplicationActivity.NativeLayerHost)} is null.");
				}
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is View view)
		{
			_attachedViews.Remove(view);

			// The view's parent, not the current activity's layer: after a re-creation they can differ.
			if (view.Parent is ViewGroup parent)
			{
				parent.RemoveView(view);
			}
		}
	}

	/// <summary>
	/// Moves the native views of <paramref name="xamlRoot"/> into <paramref name="host"/>, keeping their
	/// z-order. A re-created activity takes over a window whose tree is already loaded, so no
	/// presenter attaches again and the views would otherwise stay in the previous activity's layer.
	/// </summary>
	internal static void AdoptNativeElements(XamlRoot xamlRoot, ViewGroup host)
	{
		var views = _attachedViews
			.Where(entry => entry.Value.XamlRoot == xamlRoot && entry.Key.Parent is ViewGroup parent && parent != host)
			.Select(entry => entry.Key)
			.OrderBy(view => ((ViewGroup)view.Parent!).IndexOfChild(view))
			.ToList();

		foreach (var view in views)
		{
			((ViewGroup)view.Parent!).RemoveView(view);
			host.AddView(view);
		}
	}

	/// <summary>
	/// Drops the native views of <paramref name="xamlRoot"/>. Closing a window finishes the task
	/// hosting it rather than unloading its tree, so no presenter detaches them and their entries
	/// would pin the views — and through them the finished activity — for the life of the process.
	/// </summary>
	internal static void ReleaseNativeElements(XamlRoot xamlRoot)
	{
		var views = _attachedViews
			.Where(entry => entry.Value.XamlRoot == xamlRoot)
			.Select(entry => entry.Key)
			.ToList();

		foreach (var view in views)
		{
			_attachedViews.Remove(view);
			(view.Parent as ViewGroup)?.RemoveView(view);
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
		if (NativeLayerHost is not { } host)
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
