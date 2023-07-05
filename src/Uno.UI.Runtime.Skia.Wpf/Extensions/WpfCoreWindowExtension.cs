using System.Windows.Input;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Skia.Platform.Extensions;
using Windows.UI.Core;
using Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfUIElement = System.Windows.UIElement;

namespace Uno.UI.Skia.Platform
{
	internal partial class WpfCoreWindowExtension : ICoreWindowExtension
	{
		private readonly WpfHost? _host;
		private readonly CoreWindow _owner;

		public CoreCursor PointerCursor
		{
			get => Mouse.OverrideCursor.ToCoreCursor();
			set => Mouse.OverrideCursor = value.ToCursor();
		}

		public WpfCoreWindowExtension(object owner)
		{
			_owner = (CoreWindow)owner;
			_host = WpfHost.Current;

			WpfManager.XamlRootMap.Registered += XamlRootMap_Registered;
			WpfManager.XamlRootMap.Unregistered += XamlRootMap_Unregistered;
		}

		private void XamlRootMap_Registered(object? sender, XamlRoot xamlRoot)
		{
			var host = WpfManager.XamlRootMap.GetHostForRoot(xamlRoot);
			if (host is WpfUIElement uiElement)
			{
				uiElement.AddHandler(WpfUIElement.KeyUpEvent, (System.Windows.Input.KeyEventHandler)HostOnKeyUp, true);
				uiElement.AddHandler(WpfUIElement.KeyDownEvent, (System.Windows.Input.KeyEventHandler)HostOnKeyDown, true);
			}
		}

		private void XamlRootMap_Unregistered(object? sender, XamlRoot xamlRoot)
		{
			var host = WpfManager.XamlRootMap.GetHostForRoot(xamlRoot);
			if (host is WpfUIElement uiElement)
			{
				uiElement.RemoveHandler(WpfUIElement.KeyUpEvent, (System.Windows.Input.KeyEventHandler)HostOnKeyUp);
				uiElement.RemoveHandler(WpfUIElement.KeyDownEvent, (System.Windows.Input.KeyEventHandler)HostOnKeyDown);
			}
		}

		internal static WpfCanvas? GetOverlayLayer(XamlRoot xamlRoot) =>
			WpfManager.XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

		public bool IsNativeElement(object content)
			=> content is System.Windows.UIElement;

		public void AttachNativeElement(object owner, object content)
		{
			if (owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } layer
				&& content is System.Windows.FrameworkElement contentAsFE
				&& contentAsFE.Parent != layer)
			{
				layer.Children.Add(contentAsFE);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Unable to attach native element {content} in {owner}.");
				}
			}
		}

		public void DetachNativeElement(object owner, object content)
		{
			if (owner is XamlRoot xamlRoot
				&& GetOverlayLayer(xamlRoot) is { } layer
				&& content is System.Windows.FrameworkElement contentAsFE
				&& contentAsFE.Parent != layer)
			{
				layer.Children.Add(contentAsFE);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Unable to detach native element {content} in {owner}.");
				}
			}
		}

		public void ArrangeNativeElement(object owner, object content, Windows.Foundation.Rect arrangeRect)
		{
			if (content is System.Windows.UIElement contentAsUIElement)
			{
				WpfCanvas.SetLeft(contentAsUIElement, arrangeRect.X);
				WpfCanvas.SetTop(contentAsUIElement, arrangeRect.Y);

				contentAsUIElement.Arrange(
					new(0, 0, arrangeRect.Width, arrangeRect.Height)
				);
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
			return size;
		}
	}
}
