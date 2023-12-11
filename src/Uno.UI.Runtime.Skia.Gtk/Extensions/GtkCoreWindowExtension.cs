#nullable enable
//#define TRACE_NATIVE_POINTER_EVENTS

using System.Linq;
using Windows.Foundation;
using Gtk;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Windows.System;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Hosting;
using Uno.UI.Runtime.Skia.Gtk;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Controls;
using GtkApplication = Gtk.Application;
using Button = Gtk.Button;

namespace Uno.UI.Runtime.Skia.Gtk
{
	internal partial class GtkCoreWindowExtension : ICoreWindowExtension
	{
		private readonly CoreWindow _owner;
		private readonly DisplayInformation _displayInformation;

		public GtkCoreWindowExtension(object owner)
		{
			_owner = (CoreWindow)owner;
			_displayInformation = DisplayInformation.GetForCurrentView();
		}

		internal static Fixed? GetOverlayLayer(XamlRoot xamlRoot) =>
			GtkManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

		public bool IsNativeElement(object content)
			=> content is Widget;

		public void AttachNativeElement(object owner, object content)
		{
			if (content is Widget widget
				&& owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } overlay)
			{
				widget.ShowAll();
				overlay.Put(widget, 0, 0);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Unable to attach native element {content} to {owner}.");
				}
			}
		}

		public void DetachNativeElement(object owner, object content)
		{
			if (content is Widget widget
				&& owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } overlay)
			{
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

		public object CreateSampleComponent(string text)
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

		public bool IsNativeElementAttached(object owner, object nativeElement) =>
			nativeElement is Widget widget
				&& owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } overlay
				&& widget.Parent == overlay;

		public void ChangeNativeElementVisiblity(object owner, object content, bool visible)
		{
			if (content is Widget widget)
			{
				widget.Visible = visible;
			}
		}

		public void ChangeNativeElementOpacity(object owner, object content, double opacity)
		{
			if (content is Widget widget)
			{
				widget.Opacity = opacity;
			}
		}

		public void ArrangeNativeElement(object owner, object content, Windows.Foundation.Rect arrangeRect, Rect? clipRect)
		{
			if (content is Widget widget
				&& owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } overlay)
			{
				if (!widget.Visible)
				{
					return;
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"ArrangeNativeElement({owner}, {arrangeRect})");
				}

				var scaleAdjustment = _displayInformation.FractionalScaleAdjustment;
				var rect = new Gdk.Rectangle(
					(int)(arrangeRect.X * scaleAdjustment),
					(int)(arrangeRect.Y * scaleAdjustment),
					(int)(arrangeRect.Width * scaleAdjustment),
					(int)(arrangeRect.Height * scaleAdjustment));

				if (clipRect is { } c)
				{
					if (ContentPresenter.NativeRenderDisposables.TryGetValue(widget, out var disposable))
					{
						disposable.Dispose();
					}
					var id = widget.AddTickCallback((_, _) =>
					{
						// This seems to be the only way to get clipping to work on the initial loading.
						// It appears that there's something else moving/changing the widget, which causes the clipping
						// to reset. Also, if the window loses focus and is then refocused, the clipping will be reset unless
						// the callback is continuously called inside TickCallback
						// TODO: find out what's going on

						widget.SizeAllocate(rect);
						if (widget.Visible) // gtk screams if you attempt to SetClip when the widget isn't visible.
						{
							widget.SetClip(new Gdk.Rectangle(
								rect.X + (int)(c.X * scaleAdjustment),
								rect.Y + (int)(c.Y * scaleAdjustment),
								(int)(c.Width * scaleAdjustment),
								(int)(c.Height * scaleAdjustment)));
						}
						overlay.Move(widget, rect.X, rect.Y);
						return true;
					});
					ContentPresenter.NativeRenderDisposables[widget] = new DisposableAction(() => widget.RemoveTickCallback(id));
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

		public Windows.Foundation.Size MeasureNativeElement(object owner, object content, Size childMeasuredSize, Size availableSize)
		{
			if (content is Widget widget
				&& owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } overlay)
			{
				widget.GetPreferredSize(out var minimum_Size, out var naturalSize);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"MeasureNativeElement({minimum_Size.Width}x{minimum_Size.Height}, {naturalSize.Width}x{naturalSize.Height})");
				}

				var scaleAdjustment = _displayInformation.FractionalScaleAdjustment;
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
}
