#nullable enable

using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Uno.Extensions;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfApplication = System.Windows.Application;
using WpfWindow = System.Windows.Window;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.UI.Input;
using MouseDevice = System.Windows.Input.MouseDevice;
using System.Reflection;
using Windows.System;
using Uno.UI.Skia.Platform.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Constants;
using Uno.UI.Runtime.Skia.Wpf.Input;
using WpfCanvas = System.Windows.Controls.Canvas;
using Windows.UI.Xaml;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;

namespace Uno.UI.Skia.Platform
{
	internal partial class WpfCoreWindowExtension : ICoreWindowExtension
	{
		private readonly ICoreWindowEvents _ownerEvents;
		private readonly WpfHost? _host;

		public CoreCursor PointerCursor
		{
			get => Mouse.OverrideCursor.ToCoreCursor();
			set => Mouse.OverrideCursor = value.ToCursor();
		}

		public WpfCoreWindowExtension(object owner)
		{
			_ownerEvents = (ICoreWindowEvents)owner;

			_host = WpfHost.Current;
			if (_host is null)
			{
				return;
			}

			// Hook for native events
			_host.Loaded += HookNative;

			void HookNative(object sender, System.Windows.RoutedEventArgs e)
			{
				_host.Loaded -= HookNative;

				var win = System.Windows.Window.GetWindow(_host);

				win.AddHandler(System.Windows.UIElement.KeyUpEvent, (System.Windows.Input.KeyEventHandler)HostOnKeyUp, true);
				win.AddHandler(System.Windows.UIElement.KeyDownEvent, (System.Windows.Input.KeyEventHandler)HostOnKeyDown, true);
			}
		}

		public void SetPointerCapture(PointerIdentifier pointer)
			=> WpfHost.Current?.CaptureMouse();

		public void ReleasePointerCapture(PointerIdentifier pointer)
			=> WpfHost.Current?.ReleaseMouseCapture();

		internal static WpfCanvas? GetOverlayLayer(XamlRoot xamlRoot) =>
			XamlRootMap.GetHostForRoot(xamlRoot)?.NativeOverlayLayer;

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
		}

		public Windows.Foundation.Size MeasureNativeElement(object owner, object content, Windows.Foundation.Size size)
		{
			return size;
		}
	}
}
