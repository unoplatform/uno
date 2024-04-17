#nullable enable
//#define TRACE_NATIVE_POINTER_EVENTS

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Gtk;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;

using GLib;
using Microsoft.UI.Xaml.Controls;
using GtkApplication = Gtk.Application;
using Button = Gtk.Button;

namespace Uno.UI.Runtime.Skia.Gtk;

internal class GtkNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private static Dictionary<object, IDisposable> NativeRenderDisposables { get; } = new();

	internal static Fixed? GetOverlayLayer(XamlRoot xamlRoot) =>
		GtkManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

	public bool IsNativeElement(object content)
		=> content is Widget;

	public void AttachNativeElement(XamlRoot owner, object content)
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
				this.Log().Debug($"Unable to attach native element {content} to {owner}.");
			}
		}
	}

	public void DetachNativeElement(XamlRoot owner, object content)
	{
		if (content is Widget widget
			&& GetOverlayLayer(owner) is { } overlay)
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
				this.Log().LogDebug($"Unable to detach native element {content} from {owner}.");
			}
		}
	}

	public object CreateSampleComponent(XamlRoot owner, string text)
	{
		var vbox = new VBox(false, 5);

		var label = new Label(text);
		vbox.PackStart(label, false, false, 0);

		var hbox = new HBox(true, 3);

		var button1 = new Button("Button 1");
		var button2 = new Button("Button 2");
		hbox.Add(button1);
		hbox.Add(button2);

		vbox.PackStart(hbox, false, false, 0);

		return vbox;
	}

	public bool IsNativeElementAttached(XamlRoot owner, object nativeElement) =>
		nativeElement is Widget widget
			&& GetOverlayLayer(owner) is { } overlay
			&& widget.Parent == overlay;

	public void ChangeNativeElementVisibility(XamlRoot owner, object content, bool visible)
	{
		if (content is Widget widget)
		{
			widget.Visible = visible;
		}
	}

	public void ChangeNativeElementOpacity(XamlRoot owner, object content, double opacity)
	{
		if (content is Widget widget)
		{
			widget.Opacity = opacity;
		}
	}

	public void ArrangeNativeElement(XamlRoot owner, object content, Rect arrangeRect, Rect clipRect)
		{
			if (content is Widget widget
				&& GetOverlayLayer(owner) is { } overlay)
			{
				if (!IsNativeElementAttached(owner, content))
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
					this.Log().Trace($"ArrangeNativeElement({owner}, {arrangeRect})");
				}

				var scaleAdjustment = owner.FractionalScaleAdjustment;
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
				if (owner.HostWindow is { } window)
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
					this.Log().Debug($"Unable to arrange native element {content} in {owner}.");
				}
			}
		}

	public Size MeasureNativeElement(XamlRoot owner, object content, Size childMeasuredSize, Size availableSize)
	{
		if (content is Widget widget)
		{
			widget.GetPreferredSize(out var minimum_Size, out var naturalSize);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"MeasureNativeElement({minimum_Size.Width}x{minimum_Size.Height}, {naturalSize.Width}x{naturalSize.Height})");
			}

			var scaleAdjustment = owner.FractionalScaleAdjustment;
			return new(naturalSize.Width / scaleAdjustment, naturalSize.Height / scaleAdjustment);
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to measure native element {content} in {owner}.");
			}
		}

		return new(0, 0);
	}
}
