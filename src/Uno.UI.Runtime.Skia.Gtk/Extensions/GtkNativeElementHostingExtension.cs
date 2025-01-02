#nullable enable
//#define TRACE_NATIVE_POINTER_EVENTS

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Gtk;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;

using GLib;
using Windows.UI.Xaml.Controls;
using GtkApplication = Gtk.Application;

namespace Uno.UI.Runtime.Skia.Gtk;

internal partial class GtkNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;

	public GtkNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	private static Dictionary<object, IDisposable> NativeRenderDisposables { get; } = new();

	private XamlRoot? XamlRoot => _presenter.XamlRoot;
	private Fixed? OverlayLayer => _presenter.XamlRoot is { } xamlRoot ? GtkManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer : null;

	public bool IsNativeElement(object content) => content is Widget;

	public void AttachNativeElement(object content)
	{
		if (content is Widget widget)
		{
			widget.ShowAll();
			// We don't attach the widget to the overlay here, but on the first arrange, to prevent the overlay from being
			// misplaced for a split-second before the arrange occurs
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to attach native element {content} to {XamlRoot}.");
			}
		}
	}

	public void DetachNativeElement(object content)
	{
		if (content is Widget widget && OverlayLayer is { } overlay)
		{
			if (NativeRenderDisposables.Remove(widget, out var disposable))
			{
				disposable.Dispose();
			}
			overlay.Remove(widget);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Unable to detach native element {content} from {XamlRoot}.");
			}
		}
	}

	private bool IsNativeElementAttached(object nativeElement) =>
		nativeElement is Widget widget && OverlayLayer is { } overlay && widget.Parent == overlay;

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		if (content is Widget widget)
		{
			widget.Visible = visible;
		}
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		if (content is Widget widget)
		{
			widget.Opacity = opacity;
		}
	}

	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect)
	{
		if (content is Widget widget && OverlayLayer is { } overlay)
		{
			if (!IsNativeElementAttached(content))
			{
				// We do this not on attaching, but on the first arrange, to prevent the overlay from being
				// misplaced for a split-second before the arrange occurs
				overlay.Put(widget, 0, 0);
			}

			if (!widget.Visible)
			{
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ArrangeNativeElement({XamlRoot}, {arrangeRect})");
			}

			var scaleAdjustment = XamlRoot!.FractionalScaleAdjustment;
			var rect = new Gdk.Rectangle(
				(int)(arrangeRect.X * scaleAdjustment),
				(int)(arrangeRect.Y * scaleAdjustment),
				(int)(arrangeRect.Width * scaleAdjustment),
				(int)(arrangeRect.Height * scaleAdjustment));

			if (NativeRenderDisposables.Remove(widget, out var disposable))
			{
				disposable.Dispose();
			}

			// This seems to be the only way to get clipping to work on the initial loading.
			// It appears that there's something else moving/changing the widget, which causes the clipping
			// to reset. Also, if the window loses focus and is then refocused, the clipping will be reset unless
			// a secondary callback to reapply the clipping is called. Initially, the implementation used AddTickCallback,
			// but it seems to be hogging up the Glib queue too much. This implementation still makes the rendering slow if
			// there are many gtk elements being rearranged.
			// TODO: find out what's going on
			var callback = (object? _, object? _) =>
			{
				TimeoutHandler timeoutHandler = () =>
				{
					widget.SizeAllocate(rect);
					if (widget.Visible) // gtk screams if you attempt to SetClip when the widget isn't visible.
					{
						widget.SetClip(new Gdk.Rectangle(
							rect.X + (int)(clipRect.X * scaleAdjustment),
							rect.Y + (int)(clipRect.Y * scaleAdjustment),
							(int)(clipRect.Width * scaleAdjustment),
							(int)(clipRect.Height * scaleAdjustment)));
					}
					if (widget.Parent is { }) // i.e. we haven't detached the widget yet
					{
						overlay.Move(widget, rect.X, rect.Y);
					}
					return false;
				};
				GLib.Timeout.Add(0, timeoutHandler, Priority.High); // this gets the positioning right, but not the clipping
				GLib.Timeout.Add(0, timeoutHandler, Priority.Low); // this fixes the clipping but has to be Low so that it's delayed enough to come after whatever is breaking the clipping
			};

			callback(null, null);

			var onActivated = new WindowActivatedEventHandler(callback);
			var onSizeChanged = new WindowSizeChangedEventHandler(callback);
			if (XamlRoot.HostWindow is { } window)
			{
				window.Activated += onActivated;
				window.SizeChanged += onSizeChanged;
				NativeRenderDisposables[widget] = new DisposableAction(() =>
				{
					window.Activated -= onActivated;
					window.SizeChanged -= onSizeChanged;
				});
			}
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to arrange native element {content} in {XamlRoot}.");
			}
		}
	}

	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is Widget widget)
		{
			widget.GetPreferredSize(out var minimum_Size, out var naturalSize);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"MeasureNativeElement({minimum_Size.Width}x{minimum_Size.Height}, {naturalSize.Width}x{naturalSize.Height})");
			}

			var scaleAdjustment = XamlRoot!.FractionalScaleAdjustment;
			return new(naturalSize.Width / scaleAdjustment, naturalSize.Height / scaleAdjustment);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to measure native element {content} in {XamlRoot}.");
			}
		}

		return new(0, 0);
	}
}
