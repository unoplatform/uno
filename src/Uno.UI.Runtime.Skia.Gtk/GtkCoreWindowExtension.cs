#nullable enable
//#define TRACE_NATIVE_POINTER_EVENTS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Gdk;
using Gtk;
using Uno.Extensions;
using Uno.UI.Runtime.Skia.GTK.Extensions;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.Foundation.Logging;
using static Windows.UI.Input.PointerUpdateKind;
using Device = Gtk.Device;
using Exception = System.Exception;
using Windows.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Uno.UI.XamlHost.Skia.Gtk.Hosting;
using Atk;

namespace Uno.UI.Runtime.Skia
{
	internal partial class GtkCoreWindowExtension : ICoreWindowExtension
	{
		private readonly CoreWindow _owner;

		public GtkCoreWindowExtension(object owner)
		{
			_owner = (CoreWindow)owner;

			InitializeKeyboard();
		}

		partial void InitializeKeyboard();

		internal static Fixed? FindNativeOverlayLayer(Gtk.Window window)
		{
			var overlay = (Overlay)((EventBox)window.Child).Child;
			return overlay.Children.OfType<Fixed>().FirstOrDefault();
		}

		internal static Fixed? GetOverlayLayer(XamlRoot xamlRoot) =>
			XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

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

		public void ArrangeNativeElement(object owner, object content, Windows.Foundation.Rect arrangeRect)
		{
			if (content is Widget widget
				&& owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } overlay)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"ArrangeNativeElement({owner}, {arrangeRect})");
				}

				widget.SizeAllocate(new((int)arrangeRect.X, (int)arrangeRect.Y, (int)arrangeRect.Width, (int)arrangeRect.Height));
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Unable to arrange native element {content} in {owner}.");
				}
			}
		}

		public Windows.Foundation.Size MeasureNativeElement(object owner, object content, Windows.Foundation.Size size)
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

				return new(naturalSize.Width, naturalSize.Height);
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

		public static VirtualKeyModifiers GetKeyModifiers(Gdk.ModifierType state)
		{
			var modifiers = VirtualKeyModifiers.None;
			if (state.HasFlag(Gdk.ModifierType.ShiftMask))
			{
				modifiers |= VirtualKeyModifiers.Shift;
			}
			if (state.HasFlag(Gdk.ModifierType.ControlMask))
			{
				modifiers |= VirtualKeyModifiers.Control;
			}
			if (state.HasFlag(Gdk.ModifierType.Mod1Mask))
			{
				modifiers |= VirtualKeyModifiers.Menu;
			}
			return modifiers;
		}
	}
}
