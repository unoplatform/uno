//#define TRACE_NATIVE_POINTER_EVENTS

using System.Linq;
using Gtk;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.GTK.Hosting;
using Uno.UI.Runtime.Skia.GTK;

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
	}
}
